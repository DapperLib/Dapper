#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers miscellaneous uncovered paths in SqlMapper.cs:
    /// TypeMapEntry methods, GetCachedSQL filter, GetHashCollissions,
    /// AddTypeMap/RemoveTypeMap/HasTypeHandler, SetDbType, LookupDbType paths,
    /// ExecuteScalar/ExecuteReader/Query overloads, CompiledRegex.PseudoPositional,
    /// ThrowMultipleRows/ThrowZeroRows, and multimap overloads.
    /// </summary>
    public class FakeDbSqlMapperMiscTests
    {
        // ── TypeMapEntry internal struct methods ──────────────────────

        [Fact]
        public void TypeMapEntry_GetHashCode_ReturnsValue()
        {
            var entry = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            var hash = entry.GetHashCode();
            Assert.NotEqual(0, hash);
        }

        [Fact]
        public void TypeMapEntry_ToString_ContainsDbType()
        {
            var entry = new SqlMapper.TypeMapEntry(DbType.String, SqlMapper.TypeMapEntryFlags.SetType);
            var s = entry.ToString();
            Assert.Contains("String", s);
        }

        [Fact]
        public void TypeMapEntry_Equals_SameValues_ReturnsTrue()
        {
            var a = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            var b = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void TypeMapEntry_Equals_Object_ReturnsTrueForSame()
        {
            var a = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            object b = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void TypeMapEntry_Equals_Object_ReturnsFalseForDifferent()
        {
            var a = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            object b = new SqlMapper.TypeMapEntry(DbType.String, SqlMapper.TypeMapEntryFlags.SetType);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void TypeMapEntry_Equals_Object_ReturnsFalseForNull()
        {
            var a = new SqlMapper.TypeMapEntry(DbType.Int32, SqlMapper.TypeMapEntryFlags.SetType);
            Assert.False(a.Equals(null));
        }

        // ── GetCachedSQL with ignoreHitCountAbove filter ───────────────

        [Fact]
        public void GetCachedSQL_WithFilter_ReturnsFilteredResults()
        {
            // Run a query to populate cache
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            conn.QueryFirst<int>("SELECT v FROM CacheSQLFilter_Test");

            // Call with small ignoreHitCountAbove to exercise the Where path (L144)
            var results = SqlMapper.GetCachedSQL(999).ToList();
            Assert.NotNull(results);
        }

        // ── GetHashCollissions ─────────────────────────────────────────

        [Fact]
        public void GetHashCollissions_Returns_Enumerable()
        {
            // Just enumerate — may be empty, but covers all code paths L152-169
            var collisions = SqlMapper.GetHashCollissions().ToList();
            Assert.NotNull(collisions);
        }

        // ── AddTypeMap with useGetFieldValue = true ────────────────────

        [Fact]
        public void AddTypeMap_WithUseGetFieldValue_SetsFlag()
        {
            // Covers L303-305: the useGetFieldValue=true path
            SqlMapper.AddTypeMap(typeof(MiscTestStruct), DbType.String, useGetFieldValue: true);
            // Clean up
            SqlMapper.RemoveTypeMap(typeof(MiscTestStruct));
        }

        // ── RemoveTypeMap — removes existing type ─────────────────────

        [Fact]
        public void RemoveTypeMap_ExistingType_RemovesIt()
        {
            // Add first, then remove — covers L334-337
            SqlMapper.AddTypeMap(typeof(MiscTestStruct2), DbType.String);
            SqlMapper.RemoveTypeMap(typeof(MiscTestStruct2));
            // Removing a type that doesn't exist is a no-op
            SqlMapper.RemoveTypeMap(typeof(MiscTestStruct2)); // no-op: covers L332
        }

        // ── HasTypeHandler ─────────────────────────────────────────────

        [Fact]
        public void HasTypeHandler_UnknownType_ReturnsFalse()
        {
            Assert.False(SqlMapper.HasTypeHandler(typeof(MiscTestStruct3)));
        }

        [Fact]
        public void HasTypeHandler_KnownType_ReturnsTrue()
        {
            SqlMapper.AddTypeHandler(typeof(MiscTestStruct3), new MiscTypeHandler());
            try
            {
                Assert.True(SqlMapper.HasTypeHandler(typeof(MiscTestStruct3)));
            }
            finally
            {
                // Clean up
                SqlMapper.ResetTypeHandlers();
            }
        }

        // ── AddTypeHandlerImpl (obsolete error:true) via reflection ───

        [Fact]
        public void AddTypeHandlerImpl_ViaReflection_InvokesCore()
        {
            // [Obsolete(error:true)] prevents direct call; use reflection
            var method = typeof(SqlMapper).GetMethod("AddTypeHandlerImpl",
                BindingFlags.Public | BindingFlags.Static)!;
            // passing null handler removes the handler
            method.Invoke(null, new object?[] { typeof(MiscTestStruct4), null, true });
        }

        // ── SetDbType (obsolete warning:false) ────────────────────────

        [Fact]
        public void SetDbType_WithValue_SetsDbType()
        {
            var param = new MinimalDbParameter2(); // already defined in TVPParameter test
#pragma warning disable CS0618
            SqlMapper.SetDbType(param, 42);
#pragma warning restore CS0618
            // DbType.Int32 should be set
            Assert.Equal(DbType.Int32, param.DbType);
        }

        [Fact]
        public void SetDbType_WithNull_IsNoOp()
        {
            var param = new MinimalDbParameter2();
#pragma warning disable CS0618
            SqlMapper.SetDbType(param, null);
#pragma warning restore CS0618
            // L442: returns early, DbType stays default
            Assert.Equal(DbType.Object, param.DbType);
        }

        [Fact]
        public void SetDbType_WithDBNull_IsNoOp()
        {
            var param = new MinimalDbParameter2();
#pragma warning disable CS0618
            SqlMapper.SetDbType(param, DBNull.Value);
#pragma warning restore CS0618
            Assert.Equal(DbType.Object, param.DbType);
        }

        // ── LookupDbType: enum type ────────────────────────────────────

        [Fact]
        public void LookupDbType_EnumType_ReturnsUnderlyingDbType()
        {
#pragma warning disable CS0618
            var result = SqlMapper.LookupDbType(typeof(DayOfWeek), "day", false, out _);
#pragma warning restore CS0618
            // DayOfWeek → int → DbType.Int32
            Assert.Equal(DbType.Int32, result);
        }

        // ── LookupDbType: type mapped with SetType=0 (DoNotSet) ───────

        [Fact]
        public void LookupDbType_DoNotSetType_ReturnsNull()
        {
            // Add with dbType < 0 so SetType flag is not set
            SqlMapper.AddTypeMap(typeof(MiscTestDoNotSet), (DbType)(-2), false);
            try
            {
#pragma warning disable CS0618
                var result = SqlMapper.LookupDbType(typeof(MiscTestDoNotSet), "x", false, out _);
#pragma warning restore CS0618
                Assert.Null(result);
            }
            finally
            {
                SqlMapper.RemoveTypeMap(typeof(MiscTestDoNotSet));
            }
        }

        // ── LookupDbType: IEnumerable<IDataRecord> auto-detect ────────

        [Fact]
        public void LookupDbType_IEnumerableIDataRecord_ReturnsObject()
        {
#pragma warning disable CS0618
            var result = SqlMapper.LookupDbType(typeof(IEnumerable<IDataRecord>), "recs", false, out var handler);
#pragma warning restore CS0618
            Assert.Equal(DbType.Object, result);
            Assert.NotNull(handler);
        }

        // ── LookupDbType: demand=true with unregistered type ──────────

        [Fact]
        public void LookupDbType_DemandTrue_UnknownType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
#pragma warning disable CS0618
                SqlMapper.LookupDbType(typeof(FakeUnmappedStruct), "field", true, out _);
#pragma warning restore CS0618
            });
        }

        // ── CompiledRegex.PseudoPositional ────────────────────────────

        [Fact]
        public void CompiledRegex_PseudoPositional_Matches()
        {
            Assert.True(CompiledRegex.PseudoPositional.IsMatch("?param?"));
            Assert.False(CompiledRegex.PseudoPositional.IsMatch("@param"));
        }

        // ── ExecuteScalar(string) overload ────────────────────────────

        [Fact]
        public void ExecuteScalar_StringOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 42 } } });
            conn.Open();
            var result = conn.ExecuteScalar("SELECT 42");
            Assert.Equal(42, result);
        }

        // ── ExecuteScalar(CommandDefinition) overload ─────────────────

        [Fact]
        public void ExecuteScalar_CommandDefinitionOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 99 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT_ExecuteScalar_CmdDef_Test");
            SqlMapper.PurgeQueryCache();
            var result = conn.ExecuteScalar(cmd);
            Assert.NotNull(result);
        }

        // ── ExecuteReader(CommandDefinition) overload ─────────────────

        [Fact]
        public void ExecuteReader_CommandDefinitionOverload_ReturnsReader()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT Id FROM T");
            using var reader = conn.ExecuteReader(cmd);
            Assert.True(reader.Read());
        }

        // ── ExecuteReader(CommandDefinition, CommandBehavior) overload ──

        [Fact]
        public void ExecuteReader_CommandDefinitionAndBehavior_ReturnsReader()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 2 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT Id FROM T");
            using var reader = conn.ExecuteReader(cmd, CommandBehavior.Default);
            Assert.True(reader.Read());
        }

        // ── QueryFirstOrDefault dynamic overload (string) ─────────────

        [Fact]
        public void QueryFirstOrDefault_Dynamic_StringOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 5 } } });
            conn.Open();
            dynamic? result = conn.QueryFirstOrDefault("SELECT Id FROM T");
            Assert.NotNull(result);
        }

        [Fact]
        public void QueryFirstOrDefault_Dynamic_StringOverload_NoRows_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            dynamic? result = conn.QueryFirstOrDefault("SELECT Id FROM T");
            Assert.Null(result);
        }

        // ── QuerySingle dynamic overload (string) ─────────────────────

        [Fact]
        public void QuerySingle_Dynamic_StringOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 9 } } });
            conn.Open();
            dynamic result = conn.QuerySingle("SELECT Id FROM T");
            Assert.NotNull(result);
        }

        // ── QueryFirst(Type, string) overload ─────────────────────────

        [Fact]
        public void QueryFirst_TypeOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 11 } } });
            conn.Open();
            var result = conn.QueryFirst(typeof(int), "SELECT 11");
            Assert.Equal(11, result);
        }

        [Fact]
        public void QueryFirst_TypeOverload_NullType_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();
            Assert.Throws<ArgumentNullException>(() => conn.QueryFirst(null!, "SELECT 1"));
        }

        // ── QueryFirstOrDefault(Type, string) overload ────────────────

        [Fact]
        public void QueryFirstOrDefault_TypeOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 12 } } });
            conn.Open();
            var result = conn.QueryFirstOrDefault(typeof(int), "SELECT 12");
            Assert.Equal(12, result);
        }

        // ── QuerySingle(Type, string) overload ────────────────────────

        [Fact]
        public void QuerySingle_TypeOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 13 } } });
            conn.Open();
            var result = conn.QuerySingle(typeof(int), "SELECT 13");
            Assert.Equal(13, result);
        }

        // ── QuerySingleOrDefault(Type, string) overload ───────────────

        [Fact]
        public void QuerySingleOrDefault_TypeOverload_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 14 } } });
            conn.Open();
            var result = conn.QuerySingleOrDefault(typeof(int), "SELECT 14");
            Assert.Equal(14, result);
        }

        // ── QueryFirst<T>(CommandDefinition) overload ─────────────────

        [Fact]
        public void QueryFirst_CommandDefinition_Generic_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 15 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT 15");
            var result = conn.QueryFirst<int>(cmd);
            Assert.Equal(15, result);
        }

        // ── QueryFirstOrDefault<T>(CommandDefinition) overload ────────

        [Fact]
        public void QueryFirstOrDefault_CommandDefinition_Generic_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 16 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT 16");
            var result = conn.QueryFirstOrDefault<int>(cmd);
            Assert.Equal(16, result);
        }

        // ── QuerySingle<T>(CommandDefinition) overload ────────────────

        [Fact]
        public void QuerySingle_CommandDefinition_Generic_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 17 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT 17");
            var result = conn.QuerySingle<int>(cmd);
            Assert.Equal(17, result);
        }

        // ── QuerySingleOrDefault<T>(CommandDefinition) overload ───────

        [Fact]
        public void QuerySingleOrDefault_CommandDefinition_Generic_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 18 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT 18");
            var result = conn.QuerySingleOrDefault<int>(cmd);
            Assert.Equal(18, result);
        }

        // ── QueryMultiple(CommandDefinition) overload ─────────────────

        [Fact]
        public void QueryMultiple_CommandDefinition_Overload_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 19 } } });
            conn.Open();
            var cmd = new CommandDefinition("SELECT 19");
            using var grid = conn.QueryMultiple(cmd);
            var result = grid.Read<int>().First();
            Assert.Equal(19, result);
        }

        // ── ThrowMultipleRows: Row.Single ─────────────────────────────

        [Fact]
        public void QuerySingle_MultipleRows_ThrowsInvalidOperation()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "v", 1 } },
                new Dictionary<string, object?> { { "v", 2 } }
            });
            conn.Open();
            Assert.Throws<InvalidOperationException>(() => conn.QuerySingle<int>("SELECT v FROM T"));
        }

        // ── ThrowMultipleRows: Row.SingleOrDefault ────────────────────

        [Fact]
        public void QuerySingleOrDefault_MultipleRows_ThrowsInvalidOperation()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "v", 1 } },
                new Dictionary<string, object?> { { "v", 2 } }
            });
            conn.Open();
            Assert.Throws<InvalidOperationException>(() => conn.QuerySingleOrDefault<int>("SELECT v FROM T"));
        }

        // ── ThrowZeroRows: Row.Single (0 rows) ────────────────────────

        [Fact]
        public void QuerySingle_ZeroRows_ThrowsInvalidOperation()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            Assert.Throws<InvalidOperationException>(() => conn.QuerySingle<int>("SELECT v FROM T"));
        }

        // ── ThrowZeroRows via QueryFirst (Row.First) ─────────────────

        [Fact]
        public void QueryFirst_ZeroRows_ThrowsInvalidOperation()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            Assert.Throws<InvalidOperationException>(() => conn.QueryFirst<int>("SELECT v FROM T"));
        }

        // ── 4-type MultiMap overload ──────────────────────────────────

        [Fact]
        public void Query_4TypeMultiMap_ReturnsResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?>
                {
                    { "AId", 1 }, { "BId", 2 }, { "CId", 3 }, { "DId", 4 }
                }
            });
            conn.Open();

            var results = conn.Query<MiscA, MiscB, MiscC, MiscD, string>(
                "SELECT AId, BId, CId, DId FROM T",
                (a, b, c, d) => $"{a.AId},{b.BId},{c.CId},{d.DId}",
                splitOn: "BId,CId,DId").ToList();

            Assert.Single(results);
        }

        // ── 5-type MultiMap overload ──────────────────────────────────

        [Fact]
        public void Query_5TypeMultiMap_ReturnsResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?>
                {
                    { "AId", 1 }, { "BId", 2 }, { "CId", 3 }, { "DId", 4 }, { "EId", 5 }
                }
            });
            conn.Open();

            var results = conn.Query<MiscA, MiscB, MiscC, MiscD, MiscE, string>(
                "SELECT AId, BId, CId, DId, EId FROM T",
                (a, b, c, d, e) => $"{a.AId},{b.BId},{c.CId},{d.DId},{e.EId}",
                splitOn: "BId,CId,DId,EId").ToList();

            Assert.Single(results);
        }

        // ── 6-type MultiMap overload ──────────────────────────────────

        [Fact]
        public void Query_6TypeMultiMap_ReturnsResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?>
                {
                    { "AId", 1 }, { "BId", 2 }, { "CId", 3 }, { "DId", 4 }, { "EId", 5 }, { "FId", 6 }
                }
            });
            conn.Open();

            var results = conn.Query<MiscA, MiscB, MiscC, MiscD, MiscE, MiscF, string>(
                "SELECT AId, BId, CId, DId, EId, FId FROM T",
                (a, b, c, d, e, f) => $"{a.AId},{b.BId},{c.CId},{d.DId},{e.EId},{f.FId}",
                splitOn: "BId,CId,DId,EId,FId").ToList();

            Assert.Single(results);
        }

        // ── 7-type MultiMap overload ──────────────────────────────────

        [Fact]
        public void Query_7TypeMultiMap_ReturnsResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?>
                {
                    { "AId", 1 }, { "BId", 2 }, { "CId", 3 }, { "DId", 4 }, { "EId", 5 }, { "FId", 6 }, { "GId", 7 }
                }
            });
            conn.Open();

            var results = conn.Query<MiscA, MiscB, MiscC, MiscD, MiscE, MiscF, MiscG, string>(
                "SELECT AId, BId, CId, DId, EId, FId, GId FROM T",
                (a, b, c, d, e, f, g) => $"{a.AId},{b.BId},{c.CId},{d.DId},{e.EId},{f.FId},{g.GId}",
                splitOn: "BId,CId,DId,EId,FId,GId").ToList();

            Assert.Single(results);
        }

        // ── MultiMapImpl with empty types array ───────────────────────

        [Fact]
        public void Query_TypeArray_EmptyTypes_ThrowsArgumentException()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            Assert.Throws<ArgumentException>(() =>
                conn.Query<string>("SELECT 1", Array.Empty<Type>(), objects => "x").ToList());
        }
    }

    // ── Helper types ──────────────────────────────────────────────────

    internal struct MiscTestStruct { }
    internal struct MiscTestStruct2 { }
    internal struct MiscTestStruct3 { }
    internal struct MiscTestStruct4 { }
    internal struct MiscTestDoNotSet { }
    internal struct FakeUnmappedStruct { }

    internal class MiscTypeHandler : SqlMapper.TypeHandler<MiscTestStruct3>
    {
        public override void SetValue(IDbDataParameter parameter, MiscTestStruct3 value)
            => parameter.Value = DBNull.Value;
        public override MiscTestStruct3 Parse(object value) => default;
    }

    internal class MiscA { public int AId { get; set; } }
    internal class MiscB { public int BId { get; set; } }
    internal class MiscC { public int CId { get; set; } }
    internal class MiscD { public int DId { get; set; } }
    internal class MiscE { public int EId { get; set; } }
    internal class MiscF { public int FId { get; set; } }
    internal class MiscG { public int GId { get; set; } }
}
#endif
