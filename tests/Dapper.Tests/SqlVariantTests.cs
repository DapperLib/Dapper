using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Dapper.Tests;

// A column reported as System.Object (sql_variant, e.g. SELECT SERVERPROPERTY('IsLocalDB'))
// says nothing about the runtime value: the value must be unboxed directly when the box
// is exactly the member type, and converted at runtime otherwise (#2234)
public class SqlVariantTests
{
    [Fact]
    public void VariantInt32MapsToBool()
    {
        Assert.False(VariantQuery<BoolRow>((object)0).IsLocalDB);
        Assert.True(VariantQuery<BoolRow>((object)1).IsLocalDB);
    }

    [Fact]
    public void VariantInt32MapsToNullableBool()
    {
        Assert.Equal((bool?)true, VariantQuery<NullableBoolRow>((object)1).IsLocalDB);
        Assert.Null(VariantQuery<NullableBoolRow>(DBNull.Value).IsLocalDB);
    }

    [Fact]
    public void VariantInt32MapsToBoolStruct()
    {
        Assert.True(VariantQuery<BoolStruct>((object)1).IsLocalDB);
    }

    [Fact]
    public void VariantExactBoxMapsDirectly()
    {
        Assert.Equal(42, VariantQuery<IntRow>((object)42, "Value").Value);
        var when = new DateTime(2026, 10, 24, 12, 0, 0);
        Assert.Equal(when, VariantQuery<DateTimeRow>(when, "Value").Value);
    }

    [Fact]
    public void VariantConvertibleBoxConverts()
    {
        Assert.Equal(42, VariantQuery<IntRow>((object)42L, "Value").Value);
        Assert.Equal(1.5m, VariantQuery<DecimalRow>((object)1.5, "Value").Value);
    }

    [Fact]
    public void VariantBoxTypeCanVaryPerRow()
    {
        var rows = VariantQueryAll<BoolRow>((object)0, (object)1L, (object)1);
        Assert.Equal(new[] { false, true, true }, rows.Select(r => r.IsLocalDB).ToArray());
    }

    [Fact]
    public void DeclaredInt32StillMapsToBool()
    {
        using var cnn = FakeConnection.WithColumn(typeof(int), "IsLocalDB", 1);
        Assert.True(cnn.QuerySingle<BoolRow>("mock").IsLocalDB);
    }

    private static T VariantQuery<T>(object? value, string column = "IsLocalDB")
    {
        using var cnn = FakeConnection.WithColumn(typeof(object), column, value);
        return cnn.QuerySingle<T>("mock");
    }

    private static List<T> VariantQueryAll<T>(params object?[] values)
    {
        using var cnn = FakeConnection.WithColumn(typeof(object), "IsLocalDB", values);
        return cnn.Query<T>("mock").AsList();
    }

    public class BoolRow { public bool IsLocalDB { get; set; } }
    public class NullableBoolRow { public bool? IsLocalDB { get; set; } }
    public class IntRow { public int Value { get; set; } }
    public class DecimalRow { public decimal Value { get; set; } }
    public class DateTimeRow { public DateTime Value { get; set; } }
    public struct BoolStruct { public bool IsLocalDB { get; set; } }

#pragma warning disable CS8765 // the base ADO.NET string properties annotate their setters [AllowNull]; these fakes never observe a null value
    // an in-memory connection whose command returns rows for a single column
    // with a fixed *declared* type; values are stored (and read) as objects
    private sealed class FakeConnection : DbConnection
    {
        private readonly DbDataReader _reader;
        private ConnectionState _state = ConnectionState.Closed;

        private FakeConnection(DbDataReader reader) => _reader = reader;

        public static FakeConnection WithColumn(Type columnType, string columnName, params object?[] values)
        {
            var table = new DataTable();
            table.Columns.Add(columnName, columnType);
            foreach (var value in values)
            {
                var row = table.NewRow();
                row[columnName] = value ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return new FakeConnection(table.CreateDataReader());
        }

        public override string ConnectionString { get; set; } = "";
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;
        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new FakeCommand(this, _reader);
    }

    private sealed class FakeCommand : DbCommand
    {
        private readonly DbDataReader _reader;

        public FakeCommand(DbConnection cnn, DbDataReader reader)
        {
            DbConnection = cnn;
            _reader = reader;
        }

        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object? ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _reader;
    }

    private sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = "";
        public override int Size { get; set; }
        public override string SourceColumn { get; set; } = "";
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<object> _items = new();
        public override int Count => _items.Count;
        public override object SyncRoot => this;
        public override int Add(object value) { _items.Add(value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var v in values) _items.Add(v!); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains(value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((System.Collections.IList)_items).CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
        protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
        protected override DbParameter GetParameter(string parameterName) => (DbParameter)_items[IndexOf(parameterName)];
        public override int IndexOf(object value) => _items.IndexOf(value);
        public override int IndexOf(string parameterName)
        {
            for (int i = 0; i < _items.Count; i++)
                if (((DbParameter)_items[i]).ParameterName == parameterName) return i;
            return -1;
        }
        public override void Insert(int index, object value) => _items.Insert(index, value);
        public override void Remove(object value) => _items.Remove(value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }
}
#pragma warning restore CS8765
