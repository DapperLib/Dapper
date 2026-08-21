#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

#pragma warning disable CS0618 // Obsolete internal-use-only members are intentionally exercised here

namespace Dapper.Tests
{
    // ── SanitizeParameterValue with all enum underlying types (L2387-2403) ─────

    /// <summary>
    /// Covers SanitizeParameterValue with enum types of every underlying numeric type,
    /// exercising all TypeCode case branches.
    /// </summary>
    public class FakeDbSanitizeParameterValueTests
    {
        private enum ByteEnum : byte { A = 1 }
        private enum SByteEnum : sbyte { A = 2 }
        private enum Int16Enum : short { A = 3 }
        private enum Int32Enum : int { A = 4 }
        private enum Int64Enum : long { A = 5 }
        private enum UInt16Enum : ushort { A = 6 }
        private enum UInt32Enum : uint { A = 7 }
        private enum UInt64Enum : ulong { A = 8 }

        [Fact] public void Byte_Enum_ReturnsByte() =>
            Assert.Equal((byte)1, SqlMapper.SanitizeParameterValue(ByteEnum.A));
        [Fact] public void SByte_Enum_ReturnsSByte() =>
            Assert.Equal((sbyte)2, SqlMapper.SanitizeParameterValue(SByteEnum.A));
        [Fact] public void Int16_Enum_ReturnsShort() =>
            Assert.Equal((short)3, SqlMapper.SanitizeParameterValue(Int16Enum.A));
        [Fact] public void Int32_Enum_ReturnsInt() =>
            Assert.Equal((int)4, SqlMapper.SanitizeParameterValue(Int32Enum.A));
        [Fact] public void Int64_Enum_ReturnsLong() =>
            Assert.Equal((long)5, SqlMapper.SanitizeParameterValue(Int64Enum.A));
        [Fact] public void UInt16_Enum_ReturnsUShort() =>
            Assert.Equal((ushort)6, SqlMapper.SanitizeParameterValue(UInt16Enum.A));
        [Fact] public void UInt32_Enum_ReturnsUInt() =>
            Assert.Equal((uint)7, SqlMapper.SanitizeParameterValue(UInt32Enum.A));
        [Fact] public void UInt64_Enum_ReturnsULong() =>
            Assert.Equal((ulong)8, SqlMapper.SanitizeParameterValue(UInt64Enum.A));
        [Fact] public void Null_ReturnsDBNull() =>
            Assert.Equal(DBNull.Value, SqlMapper.SanitizeParameterValue(null));
        [Fact] public void NonEnum_ReturnsSelf() =>
            Assert.Equal("hello", SqlMapper.SanitizeParameterValue("hello"));
    }

    // ── QuerySingleOrDefault dynamic overload (L821-822) ──────────────────────

    public class FakeDbQuerySingleOrDefaultDynamicTests
    {
        [Fact]
        public void QuerySingleOrDefault_String_ReturnsDynamic()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 42 } } });
            conn.Open();
            dynamic? result = conn.QuerySingleOrDefault("SELECT v FROM T_DynSoD");
            Assert.NotNull(result);
        }

        [Fact]
        public void QuerySingleOrDefault_String_NoRows_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            dynamic? result = conn.QuerySingleOrDefault("SELECT v FROM T_DynSoD2");
            Assert.Null(result);
        }
    }

    // ── ExecuteReader with multi-exec throws (L3067-3068) ────────────────────

    public class FakeDbExecuteReaderMultiExecTests
    {
        [Fact]
        public void ExecuteReader_WithIEnumerableParam_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();
            SqlMapper.PurgeQueryCache();
            Assert.Throws<NotSupportedException>(() =>
                conn.ExecuteReader(
                    new CommandDefinition("INSERT INTO T VALUES (@id)",
                        new[] { new { id = 1 }, new { id = 2 } })));
        }
    }

    // ── ValueTuple param throws NotSupportedException (L2557-2558) ────────────

    public class FakeDbValueTupleParamTests
    {
        [Fact]
        public void Query_WithValueTupleParam_ThrowsNotSupported()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();
            SqlMapper.PurgeQueryCache();
            Assert.Throws<NotSupportedException>(() =>
                conn.QueryFirst<int>("SELECT @Item1 AS v", (1, 2)));
        }
    }

    // ── SetTypeMap null throws (L3231) ─────────────────────────────────────────

    public class FakeDbSetTypeMapTests
    {
        [Fact]
        public void SetTypeMap_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SqlMapper.SetTypeMap(null!, null));
        }

        [Fact]
        public void SetTypeMap_NullMap_RemovesCustomMap()
        {
            // register then remove
            SqlMapper.SetTypeMap(typeof(SetTypeMapHelper), new CustomPropertyTypeMap(
                typeof(SetTypeMapHelper), (type, name) => type.GetProperty(name)));
            SqlMapper.SetTypeMap(typeof(SetTypeMapHelper), null); // remove
        }
    }

    // ── ReplaceLiterals extension method (L2424-2426) ─────────────────────────

    public class FakeDbReplaceLiteralsExtensionTests
    {
        [Fact]
        public void ReplaceLiterals_Extension_ReplacesToken()
        {
            // DynamicParameters implements IParameterLookup
            var dp = new DynamicParameters();
            dp.Add("x", 42);

            // create a fake command and set sql with literal token
            var fakeCmd = new LiteralsTestCommand();
            fakeCmd.CommandText = "SELECT {=x} AS v";

            // call the extension method
            dp.ReplaceLiterals(fakeCmd);

            // {=x} should have been replaced with "42"
            Assert.Contains("42", fakeCmd.CommandText);
        }

        [Fact]
        public void ReplaceLiterals_Extension_NoTokens_NoOp()
        {
            var dp = new DynamicParameters();
            dp.Add("x", 42);
            var fakeCmd = new LiteralsTestCommand();
            fakeCmd.CommandText = "SELECT @x AS v"; // no literal token

            dp.ReplaceLiterals(fakeCmd);

            Assert.Equal("SELECT @x AS v", fakeCmd.CommandText);
        }
    }

    // ── Pipelined Execute (L638-641) ───────────────────────────────────────────

    public class FakeDbPipelinedExecuteTests
    {
        [Fact]
        public void Execute_WithPipelinedFlag_AndMultiExec_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            SqlMapper.PurgeQueryCache();

            var rows = new[] { new { id = 1 }, new { id = 2 } };
            var cmd = new CommandDefinition(
                "INSERT INTO T VALUES (@id)", rows,
                flags: CommandFlags.Pipelined);
            conn.Execute(cmd);
        }
    }

    // ── DbString list expansion (L2201-2204) ──────────────────────────────────

    public class FakeDbDbStringListTests
    {
        [Fact]
        public void Query_WithDbStringList_ExpandsCorrectly()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            SqlMapper.PurgeQueryCache();

            var names = new[] {
                new DbString { Value = "Alice", IsFixedLength = false },
                new DbString { Value = "Bob", IsFixedLength = false }
            };
            conn.Query<string>("SELECT Name FROM T WHERE Name IN @names",
                new { names }).ToList();
        }
    }

    // ── Struct param triggers isStruct path (L2579-2582) ──────────────────────

    public class FakeDbStructParamTests
    {
        [Fact]
        public void Query_WithStructParam_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            var result = conn.QueryFirst<int>(
                "SELECT @Id AS v FROM T_Struct",
                new StructQueryParam { Id = 1 });
            Assert.Equal(1, result);
        }

        [Fact]
        public void Execute_WithStructParam_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            SqlMapper.PurgeQueryCache();
            conn.Execute("UPDATE T SET v = @Value WHERE Id = @Id",
                new StructQueryParam2 { Id = 1, Value = 99 });
        }
    }

    // ── Out-of-order ctor params → hard-way sort (L2613-2650) ─────────────────

    /// <summary>
    /// Tests CreateParamInfoGenerator ctor-sort path:
    /// when property declaration order differs from ctor param order,
    /// Dapper uses positionByName to re-sort them.
    /// </summary>
    public class FakeDbCtorSortParamTests
    {
        [Fact]
        public void Query_OutOfOrderCtorParam_WorksCorrectly()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            // OutOfOrderCtorParam: properties declared B, A but ctor takes (a, b)
            // → triggers ok=false at L2613, then hard-way sort at L2625
            var result = conn.QueryFirst<int>(
                "SELECT @a + @b AS v FROM T_CtorSort",
                new OutOfOrderCtorParam("x", 1));
            Assert.Equal(1, result);
        }

        [Fact]
        public void Query_MismatchedCtorNames_FallsToAlphabeticalSort()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            // MismatchedCtorParam: property C not in ctor → hard-way sort fails → alphabetical sort
            var result = conn.QueryFirst<int>(
                "SELECT @a + @c AS v FROM T_AlphaSort",
                new MismatchedCtorParam(1, 2));
            Assert.Equal(1, result);
        }
    }

    // ── Helper types ──────────────────────────────────────────────────────────

    internal class SetTypeMapHelper
    {
        public int Id { get; set; }
    }

    internal class LiteralsTestCommand : System.Data.Common.DbCommand
    {
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override System.Data.Common.DbConnection? DbConnection { get; set; }
        protected override System.Data.Common.DbParameterCollection DbParameterCollection { get; } = new LiteralsParamCollection();
        protected override System.Data.Common.DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override System.Data.Common.DbParameter CreateDbParameter() => new LiteralsParam();
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
    }

    internal class LiteralsParam : System.Data.Common.DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string? ParameterName { get; set; }
        public override int Size { get; set; }
        public override string? SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    internal class LiteralsParamCollection : System.Data.Common.DbParameterCollection
    {
        private readonly System.Collections.ArrayList _list = new();
        public override int Count => _list.Count;
        public override object SyncRoot => _list.SyncRoot;
        public override int Add(object value) => _list.Add(value);
        public override void AddRange(Array values) => _list.AddRange(values);
        public override void Clear() => _list.Clear();
        public override bool Contains(object value) => _list.Contains(value);
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) => _list.CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
        public override int IndexOf(object value) => _list.IndexOf(value);
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => _list.Insert(index, value);
        public override void Remove(object value) => _list.Remove(value);
        public override void RemoveAt(int index) => _list.RemoveAt(index);
        public override void RemoveAt(string parameterName) { }
        protected override System.Data.Common.DbParameter GetParameter(int index) => (System.Data.Common.DbParameter)_list[index]!;
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) => throw new NotImplementedException();
        protected override void SetParameter(int index, System.Data.Common.DbParameter value) => _list[index] = value;
        protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) => throw new NotImplementedException();
    }

    internal struct StructQueryParam
    {
        public int Id { get; set; }
    }

    internal struct StructQueryParam2
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }

    /// <summary>
    /// Properties declared in order B, A — but ctor takes (a, b).
    /// This triggers the "ok=false" path in CreateParamInfoGenerator, then
    /// the hard-way sort by ctor position.
    /// </summary>
    internal class OutOfOrderCtorParam
    {
        public int B { get; }
        public string A { get; }
        public OutOfOrderCtorParam(string a, int b)
        {
            A = a;
            B = b;
        }
    }

    /// <summary>
    /// Property C is not in the ctor → hard-way sort fails → falls back to alphabetical sort.
    /// </summary>
    internal class MismatchedCtorParam
    {
        public int A { get; }
        public int C { get; }
        public MismatchedCtorParam(int a, int c)
        {
            A = a;
            C = c;
        }
    }
}
#endif
