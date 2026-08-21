#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbTypeHandlerTests
    {
        private class GuidStringHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value)
                => parameter.Value = value.ToString("D");

            public override Guid Parse(object value)
                => Guid.Parse(value.ToString()!);
        }

        [Fact]
        public void TypeHandler_Parse_IsInvokedWhenReadingColumn()
        {
            SqlMapper.AddTypeHandler(new GuidStringHandler());
            try
            {
                var id = Guid.NewGuid();
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "GuidId", id.ToString("D") } }
                });
                conn.Open();

                var result = conn.QueryFirst<GuidRow>("SELECT GuidId FROM Items");

                Assert.Equal(id, result.GuidId);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private class GuidRow
        {
            public Guid GuidId { get; set; }
        }

        [Fact]
        public void TypeHandler_SetValue_IsInvokedWhenPassingParameter()
        {
            var handler = new TrackingHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("INSERT INTO Items (Val) VALUES (@val)",
                    new { val = new TrackedType { Value = "x" } });

                Assert.True(handler.SetValueCalled);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private class TrackedType { public string? Value { get; set; } }

        private class TrackingHandler : SqlMapper.TypeHandler<TrackedType>
        {
            public bool SetValueCalled { get; private set; }

            public override void SetValue(IDbDataParameter parameter, TrackedType? value)
            {
                SetValueCalled = true;
                parameter.Value = value?.Value;
            }

            public override TrackedType Parse(object value)
                => new TrackedType { Value = value?.ToString() };
        }

        [Fact]
        public void PackListParameters_UsesTypeHandler_ForExpandedItems()
        {
            var handler = new ListTrackingHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                using var cmd = new TestCommand();

#pragma warning disable CS0618
                SqlMapper.PackListParameters(cmd, "ids", new[]
                {
                    new TrackedType { Value = "a" },
                    new TrackedType { Value = "b" }
                });
#pragma warning restore CS0618

                Assert.Equal(2, handler.SetValueCallCount);
                var parameters = cmd.TestParameters.Cast<TestParameter>().ToArray();
                Assert.Equal(2, parameters.Length);
                Assert.Equal("ids1", parameters[0].ParameterName);
                Assert.Equal("handled:a", parameters[0].Value);
                Assert.Equal("ids2", parameters[1].ParameterName);
                Assert.Equal("handled:b", parameters[1].Value);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private sealed class ListTrackingHandler : SqlMapper.TypeHandler<TrackedType>
        {
            public int SetValueCallCount { get; private set; }

            public override void SetValue(IDbDataParameter parameter, TrackedType? value)
            {
                SetValueCallCount++;
                parameter.Value = value is null ? null : "handled:" + value.Value;
            }

            public override TrackedType Parse(object value)
                => new TrackedType { Value = value?.ToString() };
        }

        private sealed class TestCommand : DbCommand
        {
            private readonly TestParameterCollection _parameters = new();

            public TestParameterCollection TestParameters => _parameters;

            public override string? CommandText { get; set; } = "SELECT * FROM T WHERE Id IN @ids";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection => _parameters;
            protected override DbTransaction? DbTransaction { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => 0;
            public override object? ExecuteScalar() => null;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new TestParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        }

        private sealed class TestParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; }
            public override bool IsNullable { get; set; }
            public override string? ParameterName { get; set; } = "";
            public override int Size { get; set; }
            public override string? SourceColumn { get; set; } = "";
            public override bool SourceColumnNullMapping { get; set; }
            public override object? Value { get; set; }

            public override void ResetDbType() { }
        }

        private sealed class TestParameterCollection : DbParameterCollection
        {
            private readonly List<DbParameter> _items = new();

            public override int Count => _items.Count;
            public override object SyncRoot => ((System.Collections.ICollection)_items).SyncRoot;

            public override int Add(object value)
            {
                _items.Add((DbParameter)value);
                return _items.Count - 1;
            }

            public override void AddRange(Array values)
            {
                foreach (var value in values)
                {
                    Add(value!);
                }
            }

            public override void Clear() => _items.Clear();
            public override bool Contains(object value) => _items.Contains((DbParameter)value);
            public override bool Contains(string value) => _items.Any(p => p.ParameterName == value);
            public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_items).CopyTo(array, index);
            public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
            public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
            public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
            public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
            public override void Remove(object value) => _items.Remove((DbParameter)value);
            public override void RemoveAt(int index) => _items.RemoveAt(index);
            public override void RemoveAt(string parameterName)
            {
                var index = IndexOf(parameterName);
                if (index >= 0)
                {
                    _items.RemoveAt(index);
                }
            }

            protected override DbParameter GetParameter(int index) => _items[index];
            protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
            protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
            protected override void SetParameter(string parameterName, DbParameter value)
            {
                var index = IndexOf(parameterName);
                if (index >= 0)
                {
                    _items[index] = value;
                }
                else
                {
                    _items.Add(value);
                }
            }
        }
    }
}
#endif
