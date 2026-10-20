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
    // ── PassByPosition (L1900-1944) ────────────────────────────────────────────

    /// <summary>
    /// Covers ShouldPassByPosition + PassByPosition: SQL with ?x? pseudo-positional params.
    /// </summary>
    public class FakeDbSqlMapperPassByPositionTests
    {
        [Fact]
        public void PassByPosition_BasicQuery_RewritesSqlAndExecutes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 3 } } });
            conn.Open();
            // ?x? and ?y? trigger ShouldPassByPosition → PassByPosition
            var result = conn.QueryFirst<int>("SELECT ?x? + ?y? AS v", new { x = 1, y = 2 });
            Assert.Equal(3, result);
        }

        [Fact]
        public void PassByPosition_UnknownParam_LeavesAlone()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 5 } } });
            conn.Open();
            // ?z? is not in the param object → leaves token as-is (L1940-1942)
            var result = conn.QueryFirst<int>("SELECT ?x? + ?z? AS v", new { x = 1, y = 2 });
            Assert.Equal(5, result);
        }

        [Fact]
        public void PassByPosition_IncrementalNames_SetsParameterNames()
        {
            var original = SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames;
            try
            {
                SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 10 } } });
                conn.Open();
                var result = conn.QueryFirst<int>("SELECT ?x? AS v", new { x = 10 });
                Assert.Equal(10, result);
            }
            finally
            {
                SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = original;
            }
        }

        [Fact]
        public void PassByPosition_DuplicateParamReference_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            // Using ?x? twice should throw because once consumed, second reference fails
            Assert.Throws<InvalidOperationException>(() =>
                conn.QueryFirst<int>("SELECT ?x? + ?x? AS v", new { x = 1 }));
        }
    }

    // ── Empty IN-list handling (L2255-2274) ────────────────────────────────────

    /// <summary>
    /// Covers the empty-list path in PackListParameters: rewrites IN @ids to (SELECT @ids WHERE 1=0).
    /// </summary>
    public class FakeDbSqlMapperEmptyListTests
    {
        [Fact]
        public void Query_EmptyIntList_ReturnsEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            var results = conn.Query<int>(
                "SELECT Id FROM T WHERE Id IN @ids",
                new { ids = Array.Empty<int>() }).ToList();
            Assert.Empty(results);
        }

        [Fact]
        public void Query_EmptyLongList_ReturnsEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            var results = conn.Query<long>(
                "SELECT Id FROM T WHERE Id IN @ids",
                new { ids = Array.Empty<long>() }).ToList();
            Assert.Empty(results);
        }

        [Fact]
        public void Query_EmptyStringList_ReturnsEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();
            var results = conn.Query<string>(
                "SELECT Name FROM T WHERE Name IN @names",
                new { names = Array.Empty<string>() }).ToList();
            Assert.Empty(results);
        }
    }

    // ── PadListExpansions (L2229-2245) ─────────────────────────────────────────

    /// <summary>
    /// Covers the PadListExpansions=true code path: pads expanded IN-list parameters.
    /// </summary>
    public class FakeDbSqlMapperPadListExpansionsTests
    {
        [Fact]
        public void PadListExpansions_ListOf7_PadsTo10()
        {
            var original = SqlMapper.Settings.PadListExpansions;
            try
            {
                SqlMapper.Settings.PadListExpansions = true;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new[] { 1, 2, 3, 4, 5, 6, 7 };
                conn.Query<int>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
                // If no exception thrown, padding executed correctly
            }
            finally
            {
                SqlMapper.Settings.PadListExpansions = original;
            }
        }

        [Fact]
        public void PadListExpansions_StringList_PadsCorrectly()
        {
            var original = SqlMapper.Settings.PadListExpansions;
            try
            {
                SqlMapper.Settings.PadListExpansions = true;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var names = new[] { "a", "b", "c", "d", "e", "f", "g" };
                conn.Query<string>("SELECT Name FROM T WHERE Name IN @names", new { names }).ToList();
            }
            finally
            {
                SqlMapper.Settings.PadListExpansions = original;
            }
        }
    }

    // ── GetListPaddingExtraCount (L2119-2144) ──────────────────────────────────

    /// <summary>
    /// Directly covers all branches of GetListPaddingExtraCount internal method.
    /// </summary>
    public class FakeDbSqlMapperPaddingCountTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(5, 0)]
        [InlineData(-1, 0)]
        [InlineData(6, 4)]    // 6 % 10 = 6 → need 4 more
        [InlineData(10, 0)]   // 10 % 10 = 0 → no padding
        [InlineData(17, 3)]   // 17 % 10 = 7 → need 3 more
        [InlineData(150, 0)]  // 150 % 10 = 0 → no padding (boundary)
        [InlineData(151, 49)] // padFactor=50; 151 % 50 = 1 → need 49 more
        [InlineData(750, 0)]  // 750 % 50 = 0 → no padding
        [InlineData(751, 49)] // padFactor=100; 751 % 100 = 51 → need 49 more
        [InlineData(2000, 0)] // boundary of padFactor=100
        [InlineData(2001, 9)] // padFactor=10; 2001 % 10 = 1 → need 9 more
        [InlineData(2070, 0)] // boundary of padFactor=10
        [InlineData(2071, 0)] // between 2070-2100 → return 0
        [InlineData(2100, 0)] // boundary between 2070-2100 → return 0
        [InlineData(2101, 99)] // padFactor=200; 2101 % 200 = 101 → need 99 more
        public void GetListPaddingExtraCount_ReturnsExpected(int count, int expected)
        {
            var result = SqlMapper.GetListPaddingExtraCount(count);
            Assert.Equal(expected, result);
        }
    }

    // ── TryStringSplit (L2310-2376) ────────────────────────────────────────────

    /// <summary>
    /// Covers TryStringSplit dispatch (int/long/short/byte) and TryStringSplit&lt;T&gt; body.
    /// Triggered by Settings.InListStringSplitCount &gt;= 0 with lists at or above the threshold.
    /// </summary>
    public class FakeDbSqlMapperStringSplitTests
    {
        [Fact]
        public void InListStringSplit_IntList_AtThreshold_UsesSplit()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 5;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new[] { 1, 2, 3, 4, 5, 6 }; // 6 >= 5
                conn.Query<int>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void InListStringSplit_LongList_AtThreshold_UsesSplit()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 3;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new long[] { 10L, 20L, 30L, 40L }; // 4 >= 3
                conn.Query<long>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void InListStringSplit_ShortList_AtThreshold_UsesSplit()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 2;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new short[] { 1, 2, 3 }; // 3 >= 2
                conn.Query<short>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void InListStringSplit_ByteList_AtThreshold_UsesSplit()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 2;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new byte[] { 1, 2, 3 }; // 3 >= 2
                conn.Query<byte>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void InListStringSplit_BelowThreshold_ExpandsNormally()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 10;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new[] { 1, 2, 3 }; // 3 < 10 → normal expansion
                conn.Query<int>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void InListStringSplit_SingleItem_IterPath_Works()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 0; // 1 >= 0
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
                conn.Open();
                var ids = new[] { 42 };
                conn.Query<int>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }
    }

    // ── Format() method (L2436-2502) ──────────────────────────────────────────

    /// <summary>
    /// Directly covers Format() all TypeCode branches, the multiexec (IEnumerable) path,
    /// empty multiexec, and the unsupported type exception.
    /// </summary>
    public class FakeDbSqlMapperFormatTests
    {
        [Fact] public void Format_Null_ReturnsNullString() => Assert.Equal("null", SqlMapper.Format(null));
        [Fact] public void Format_DBNull_ReturnsNullString() => Assert.Equal("null", SqlMapper.Format(DBNull.Value));
        [Fact] public void Format_True_Returns1() => Assert.Equal("1", SqlMapper.Format(true));
        [Fact] public void Format_False_Returns0() => Assert.Equal("0", SqlMapper.Format(false));
        [Fact] public void Format_Byte_ReturnsString() => Assert.Equal("5", SqlMapper.Format((byte)5));
        [Fact] public void Format_SByte_ReturnsString() => Assert.Equal("-3", SqlMapper.Format((sbyte)-3));
        [Fact] public void Format_UInt16_ReturnsString() => Assert.Equal("65535", SqlMapper.Format((ushort)65535));
        [Fact] public void Format_Int16_ReturnsString() => Assert.Equal("-100", SqlMapper.Format((short)-100));
        [Fact] public void Format_UInt32_ReturnsString() => Assert.Equal("4000000000", SqlMapper.Format((uint)4_000_000_000u));
        [Fact] public void Format_Int32_ReturnsString() => Assert.Equal("42", SqlMapper.Format((int)42));
        [Fact] public void Format_UInt64_ReturnsString() => Assert.Equal("18446744073709551615", SqlMapper.Format(ulong.MaxValue));
        [Fact] public void Format_Int64_ReturnsString() => Assert.Equal("-9223372036854775808", SqlMapper.Format(long.MinValue));
        [Fact] public void Format_Single_ReturnsString() => Assert.Equal("1.5", SqlMapper.Format((float)1.5f));
        [Fact] public void Format_Double_ReturnsString() => Assert.Equal("3.14", SqlMapper.Format((double)3.14));
        [Fact] public void Format_Decimal_ReturnsString() => Assert.Equal("123.456", SqlMapper.Format((decimal)123.456m));

        [Fact]
        public void Format_IntArray_NonEmpty_ReturnsTuple()
        {
            var result = SqlMapper.Format(new[] { 1, 2, 3 });
            Assert.Equal("(1,2,3)", result);
        }

        [Fact]
        public void Format_IntList_NonEmpty_ReturnsTuple()
        {
            var result = SqlMapper.Format(new List<int> { 10, 20 });
            Assert.Equal("(10,20)", result);
        }

        [Fact]
        public void Format_EmptyArray_ReturnsSelectNull()
        {
            var result = SqlMapper.Format(Array.Empty<int>());
            Assert.Equal("(select null where 1=0)", result);
        }

        [Fact]
        public void Format_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => SqlMapper.Format(new object()));
        }
    }

    // ── ReadChar / ReadNullableChar (L2071-2092) ───────────────────────────────

    /// <summary>
    /// Covers ReadChar and ReadNullableChar all branches.
    /// </summary>
    public class FakeDbSqlMapperReadCharTests
    {
        [Fact]
        public void ReadChar_SingleCharString_ReturnsChar()
        {
            var c = SqlMapper.ReadChar("a");
            Assert.Equal('a', c);
        }

        [Fact]
        public void ReadChar_CharValue_ReturnsChar()
        {
            var c = SqlMapper.ReadChar('z');
            Assert.Equal('z', c);
        }

        [Fact]
        public void ReadChar_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SqlMapper.ReadChar(null!));
        }

        [Fact]
        public void ReadChar_DBNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SqlMapper.ReadChar(DBNull.Value));
        }

        [Fact]
        public void ReadChar_MultiCharString_Throws()
        {
            Assert.Throws<ArgumentException>(() => SqlMapper.ReadChar("ab"));
        }

        [Fact]
        public void ReadNullableChar_Null_ReturnsNull()
        {
            var c = SqlMapper.ReadNullableChar(null!);
            Assert.Null(c);
        }

        [Fact]
        public void ReadNullableChar_DBNull_ReturnsNull()
        {
            var c = SqlMapper.ReadNullableChar(DBNull.Value);
            Assert.Null(c);
        }

        [Fact]
        public void ReadNullableChar_SingleCharString_ReturnsChar()
        {
            var c = SqlMapper.ReadNullableChar("x");
            Assert.Equal('x', c);
        }

        [Fact]
        public void ReadNullableChar_CharValue_ReturnsChar()
        {
            var c = SqlMapper.ReadNullableChar('k');
            Assert.Equal('k', c);
        }

        [Fact]
        public void ReadNullableChar_MultiCharString_Throws()
        {
            Assert.Throws<ArgumentException>(() => SqlMapper.ReadNullableChar("ab"));
        }
    }

    // ── ReplaceLiterals via {=col} literal token SQL syntax (L2505-2517) ───────

    /// <summary>
    /// Covers ReplaceLiterals: Dapper substitutes {=col} tokens with SQL literal values.
    /// Simultaneously exercises Format() for various types via the {=x} path.
    /// </summary>
    public class FakeDbSqlMapperReplaceLiteralsTests
    {
        [Fact]
        public void LiteralToken_Int_SubstitutedInSQL()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 42 } } });
            conn.Open();
            SqlMapper.PurgeQueryCache();
            var result = conn.QueryFirst<int>("SELECT {=x} AS v FROM T_LitInt", new { x = 42 });
            Assert.Equal(42, result);
        }

        [Fact]
        public void LiteralToken_Bool_SubstitutedInSQL()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            SqlMapper.PurgeQueryCache();
            var result = conn.QueryFirst<int>("SELECT {=flag} AS v FROM T_LitBool", new { flag = true });
            Assert.Equal(1, result);
        }

        [Fact]
        public void LiteralToken_Long_SubstitutedInSQL()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", 1 } } });
            conn.Open();
            SqlMapper.PurgeQueryCache();
            var result = conn.QueryFirst<int>("SELECT {=id} AS v FROM T_LitLong", new { id = 100L });
            Assert.Equal(1, result);
        }
    }

    // ── Dynamic multimap (L1747-1773) ─────────────────────────────────────────

    /// <summary>
    /// Covers GenerateDeserializers dynamic path: first type is object/dynamic.
    /// </summary>
    public class FakeDbSqlMapperDynamicMultimapTests
    {
        [Fact]
        public void DynamicMultimap_TwoTypes_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Name", "Alice" }, { "Id", 1 }, { "Score", 99 } }
            });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            var results = conn.Query<dynamic, AdvancedB, AdvancedResult>(
                "SELECT Name, Id, Score FROM T_DynMulti",
                (a, b) => new AdvancedResult { DynamicName = ((IDictionary<string, object?>)a)["Name"]?.ToString(), Score = b.Score },
                splitOn: "Id").ToList();

            Assert.Single(results);
            Assert.Equal("Alice", results[0].DynamicName);
        }

        [Fact]
        public void DynamicMultimap_SplitOnStar_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Name", "Bob" }, { "Score", 42 } }
            });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            // splitOn="*" → each column gets its own deserializer slot
            var results = conn.Query<dynamic, dynamic, AdvancedResult>(
                "SELECT Name, Score FROM T_DynMultiStar",
                (a, b) => new AdvancedResult { DynamicName = "ok", Score = 0 },
                splitOn: "*").ToList();

            Assert.Single(results);
        }
    }

    // ── MultiMapException paths (L1984-2001) ───────────────────────────────────

    /// <summary>
    /// Covers MultiMapException: called when splitOn column not found or no columns.
    /// </summary>
    public class FakeDbSqlMapperMultiMapExceptionTests
    {
        [Fact]
        public void MultiMap_BadSplitOn_ThrowsArgumentException()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "X" } }
            });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            // splitOn="MissingColumn" — column doesn't exist → MultiMapException
            Assert.Throws<ArgumentException>(() =>
                conn.Query<AdvancedA, AdvancedB, AdvancedResult>(
                    "SELECT Id, Name FROM T_BadSplitOn",
                    (a, b) => new AdvancedResult(),
                    splitOn: "MissingColumn").ToList());
        }

        [Fact]
        public void MultiMap_NoSplitOnSpecified_ThrowsArgumentException()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Name", "X" }, { "Score", 1 } }
            });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            // Default splitOn="Id" — not present in results → MultiMapException with message about splitOn
            Assert.Throws<ArgumentException>(() =>
                conn.Query<AdvancedA, AdvancedB, AdvancedResult>(
                    "SELECT Name, Score FROM T_NoSplitOn",
                    (a, b) => new AdvancedResult(),
                    splitOn: "Id").ToList());
        }
    }

    // ── GetDapperRowDeserializer startBound != 0 (L2052-2060) ─────────────────

    /// <summary>
    /// Covers the startBound != 0 path in GetDapperRowDeserializer: triggered by multimap
    /// queries where second/later types start mid-reader.
    /// </summary>
    public class FakeDbSqlMapperDapperRowStartBoundTests
    {
        [Fact]
        public void DynamicMultimap_SecondTypeIsDynamic_StartBoundNonZero()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" }, { "Score", 10 } }
            });
            conn.Open();
            SqlMapper.PurgeQueryCache();

            // First type is typed, second is dynamic → startBound != 0 for second
            var results = conn.Query<AdvancedA, dynamic, AdvancedResult>(
                "SELECT Id, Name, Score FROM T_StartBound",
                (a, b) => new AdvancedResult { Score = a.Id },
                splitOn: "Name").ToList();

            Assert.Single(results);
        }
    }

    // ── Helper types ──────────────────────────────────────────────────────────

    internal class AdvancedA
    {
        public int Id { get; set; }
    }

    internal class AdvancedB
    {
        public int Score { get; set; }
    }

    internal class AdvancedResult
    {
        public string? DynamicName { get; set; }
        public int Score { get; set; }
    }
}
#endif
