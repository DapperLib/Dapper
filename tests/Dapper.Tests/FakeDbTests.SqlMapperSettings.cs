#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional tests for SqlMapper.Settings properties not yet covered,
    /// and DefaultTypeMap features.
    /// </summary>
    public class FakeDbSqlMapperSettingsTests
    {
        // ── SqlMapper.Settings — additional properties ─────────────────

        [Fact]
        public void Settings_UseSingleResultOptimization_CanBeSet()
        {
            var original = SqlMapper.Settings.UseSingleResultOptimization;
            try
            {
                SqlMapper.Settings.UseSingleResultOptimization = false;
                Assert.False(SqlMapper.Settings.UseSingleResultOptimization);
                SqlMapper.Settings.UseSingleResultOptimization = true;
                Assert.True(SqlMapper.Settings.UseSingleResultOptimization);
            }
            finally
            {
                SqlMapper.Settings.UseSingleResultOptimization = original;
            }
        }

        [Fact]
        public void Settings_UseSingleRowOptimization_CanBeSet()
        {
            var original = SqlMapper.Settings.UseSingleRowOptimization;
            try
            {
                SqlMapper.Settings.UseSingleRowOptimization = false;
                Assert.False(SqlMapper.Settings.UseSingleRowOptimization);
                SqlMapper.Settings.UseSingleRowOptimization = true;
                Assert.True(SqlMapper.Settings.UseSingleRowOptimization);
            }
            finally
            {
                SqlMapper.Settings.UseSingleRowOptimization = original;
            }
        }

        [Fact]
        public void Settings_InListStringSplitCount_CanBeSet()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 5;
                Assert.Equal(5, SqlMapper.Settings.InListStringSplitCount);
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void Settings_FetchSize_CanBeSet()
        {
            var original = SqlMapper.Settings.FetchSize;
            try
            {
                SqlMapper.Settings.FetchSize = 1024;
                Assert.Equal(1024, SqlMapper.Settings.FetchSize);
            }
            finally
            {
                SqlMapper.Settings.FetchSize = original;
            }
        }

        [Fact]
        public void Settings_SupportLegacyParameterTokens_CanBeSet()
        {
            var original = SqlMapper.Settings.SupportLegacyParameterTokens;
            try
            {
                SqlMapper.Settings.SupportLegacyParameterTokens = false;
                Assert.False(SqlMapper.Settings.SupportLegacyParameterTokens);
            }
            finally
            {
                SqlMapper.Settings.SupportLegacyParameterTokens = original;
            }
        }

        [Fact]
        public void Settings_SetDefaults_Works()
        {
            // SetDefaults resets all settings to defaults — just verify it doesn't throw
            SqlMapper.Settings.SetDefaults();
            // Verify defaults were restored
            Assert.Null(SqlMapper.Settings.CommandTimeout);
        }

        // ── DefaultTypeMap — additional operations ────────────────────

        private class Underscore
        {
            public int UserId { get; set; }
            public string? FirstName { get; set; }
        }

        [Fact]
        public void DefaultTypeMap_MatchNamesWithUnderscores_Works()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> {
                        { "user_id", 1 },
                        { "first_name", "Alice" }
                    }
                });
                conn.Open();

                var result = conn.QueryFirst<Underscore>("SELECT user_id, first_name FROM T");
                Assert.Equal(1, result.UserId);
                Assert.Equal("Alice", result.FirstName);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        [Fact]
        public void DefaultTypeMap_FindConstructor_DefaultCtor_Works()
        {
            var map = new DefaultTypeMap(typeof(Underscore));
            // Empty param list matches the default constructor
            var ctor = map.FindConstructor(System.Array.Empty<string>(), System.Array.Empty<System.Type>());
            Assert.NotNull(ctor);
        }

        [Fact]
        public void DefaultTypeMap_FindExplicitConstructor_NoAttribute_ReturnsNull()
        {
            var map = new DefaultTypeMap(typeof(Underscore));
            var ctor = map.FindExplicitConstructor();

            // No [ExplicitConstructor] attribute on Underscore
            Assert.Null(ctor);
        }

        [Fact]
        public void DefaultTypeMap_GetConstructorParameter_Works()
        {
            // A type with a single-arg constructor
            var map = new DefaultTypeMap(typeof(SingleCtorClass));
            var ctor = map.FindConstructor(new[] { "id" }, new[] { typeof(int) });
            if (ctor is not null)
            {
                var member = map.GetConstructorParameter(ctor, "id");
                Assert.NotNull(member);
            }
        }

        [Fact]
        public void DefaultTypeMap_GetMember_FieldMapping_Works()
        {
            // DefaultTypeMap also maps public fields
            var map = new DefaultTypeMap(typeof(FieldClass));
            var member = map.GetMember("Value");
            Assert.NotNull(member);
        }

        [Fact]
        public void DefaultTypeMap_Properties_IsNotEmpty()
        {
            var map = new DefaultTypeMap(typeof(Underscore));
            Assert.NotEmpty(map.Properties);
        }

        // ── Helper types ──────────────────────────────────────────────

        private class SingleCtorClass
        {
            public int Id { get; }
            public SingleCtorClass(int id) { Id = id; }
        }

        private class FieldClass
        {
            public int Value;
        }

        // ── AsList extension ──────────────────────────────────────────

        [Fact]
        public void AsList_FromList_ReturnsSameList()
        {
            var list = new System.Collections.Generic.List<int> { 1, 2, 3 };
            var result = list.AsList();
            Assert.Same(list, result);
        }

        [Fact]
        public void AsList_FromArray_ReturnsNewList()
        {
            int[] array = { 1, 2, 3 };
            var result = array.AsList();
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void AsList_FromNull_ReturnsNull()
        {
            System.Collections.Generic.IEnumerable<int>? nullSeq = null;
            var result = nullSeq.AsList();
            // AsList returns null! when source is null
            Assert.Null(result);
        }
    }
}
#endif
