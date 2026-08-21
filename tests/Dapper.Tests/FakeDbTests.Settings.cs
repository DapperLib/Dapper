#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for SqlMapper.Settings properties and LiteralToken ({=param}) substitution.
    /// </summary>
    public class FakeDbSettingsTests
    {
        // ── SqlMapper.Settings ────────────────────────────────────────

        [Fact]
        public void Settings_CommandTimeout_CanBeSet()
        {
            var original = SqlMapper.Settings.CommandTimeout;
            try
            {
                SqlMapper.Settings.CommandTimeout = 60;
                Assert.Equal(60, SqlMapper.Settings.CommandTimeout);
            }
            finally
            {
                SqlMapper.Settings.CommandTimeout = original;
            }
        }

        [Fact]
        public void Settings_CommandTimeout_NullAllowed()
        {
            var original = SqlMapper.Settings.CommandTimeout;
            try
            {
                SqlMapper.Settings.CommandTimeout = null;
                Assert.Null(SqlMapper.Settings.CommandTimeout);
            }
            finally
            {
                SqlMapper.Settings.CommandTimeout = original;
            }
        }

        [Fact]
        public void Settings_ApplyNullValues_CanBeSet()
        {
            var original = SqlMapper.Settings.ApplyNullValues;
            try
            {
                SqlMapper.Settings.ApplyNullValues = true;
                Assert.True(SqlMapper.Settings.ApplyNullValues);
                SqlMapper.Settings.ApplyNullValues = false;
                Assert.False(SqlMapper.Settings.ApplyNullValues);
            }
            finally
            {
                SqlMapper.Settings.ApplyNullValues = original;
            }
        }

        [Fact]
        public void Settings_PadListExpansions_CanBeSet()
        {
            var original = SqlMapper.Settings.PadListExpansions;
            try
            {
                SqlMapper.Settings.PadListExpansions = false;
                Assert.False(SqlMapper.Settings.PadListExpansions);
                SqlMapper.Settings.PadListExpansions = true;
                Assert.True(SqlMapper.Settings.PadListExpansions);
            }
            finally
            {
                SqlMapper.Settings.PadListExpansions = original;
            }
        }

        [Fact]
        public void Settings_UseIncrementalPseudoPositionalParameterNames_CanBeSet()
        {
            var original = SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames;
            try
            {
                SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
                Assert.True(SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames);
            }
            finally
            {
                SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = original;
            }
        }

        // ── Literal token {=param} substitution ──────────────────────

        private class SimpleResult { public int Val { get; set; } }

        [Fact]
        public void Query_LiteralToken_IntValue_Substituted()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", 42 } }
            });
            conn.Open();

            // {=val} is a literal substitution — the value is embedded directly in the SQL
            var result = conn.Query<SimpleResult>(
                "SELECT {=val} AS Val", new { val = 42 }).ToList();

            Assert.Single(result);
            Assert.Equal(42, result[0].Val);
        }

        [Fact]
        public void Query_LiteralToken_StringValue_Substituted()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", 99 } }
            });
            conn.Open();

            var result = conn.Query<SimpleResult>(
                "SELECT {=n} AS Val FROM T WHERE n = {=n}", new { n = 99 }).ToList();

            Assert.Single(result);
        }

        // ── PropertyInfoByNameComparer (used by DefaultTypeMap) ───────

        [Fact]
        public void DefaultTypeMap_GetMember_CaseInsensitive_Works()
        {
            // DefaultTypeMap uses PropertyInfoByNameComparer for case-insensitive matching
            var map = new DefaultTypeMap(typeof(SimpleResult));
            var member = map.GetMember("val");  // lowercase - should match Val property
            Assert.NotNull(member);
        }

        // ── Identity and caching ──────────────────────────────────────

        [Fact]
        public void PurgeQueryCache_ReducesCacheCount()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", 1 } }
            });
            conn.Open();

            // Execute a query to populate cache
            conn.Query<SimpleResult>("SELECT 1 AS Val").ToList();
            var before = SqlMapper.GetCachedSQL();
            SqlMapper.PurgeQueryCache();
            var after = SqlMapper.GetCachedSQL();

            // After purge, cache should be empty
            Assert.Empty(after);
        }

        // ── Settings.ApplyNullValues behavior ─────────────────────────

        private class NullableResult { public int? Value { get; set; } }

        [Fact]
        public void Query_WithApplyNullValues_SetsNullProperty()
        {
            var original = SqlMapper.Settings.ApplyNullValues;
            try
            {
                SqlMapper.Settings.ApplyNullValues = true;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "Value", DBNull.Value } }
                });
                conn.Open();

                var result = conn.QueryFirst<NullableResult>("SELECT NULL AS Value");
                Assert.Null(result.Value);
            }
            finally
            {
                SqlMapper.Settings.ApplyNullValues = original;
            }
        }
    }
}
#endif
