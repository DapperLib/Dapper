#if !NET481
using System.Collections.Generic;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Targets specific uncovered paths in DefaultTypeMap:
    /// - MatchFirstOrDefault lines 213 (exact after normalize), 229 (exact normalize both), 236 (case-insensitive normalize both)
    /// - FindConstructor line 85: EqualsCIU (underscore match in constructor params)
    /// - GetMember field underscore paths (lines 164-173)
    /// </summary>
    public class FakeDbDefaultTypeMapUnderscoreTests
    {
        // ── MatchFirstOrDefault line 213: normalized column matches property exactly ──
        // column "User_Id" → normalized "UserId", property "UserId" → exact ordinal match

        private class UserWithPascal { public int UserId { get; set; } }

        [Fact]
        public void MatchNamesWithUnderscores_Line213_ExactNormalizedMatch()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "User_Id", 42 } }
                });
                conn.Open();

                // column "User_Id" → normalized "UserId" → exact match with property "UserId"
                var result = conn.QueryFirst<UserWithPascal>("SELECT User_Id FROM T");
                Assert.Equal(42, result.UserId);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        // ── MatchFirstOrDefault line 229: normalized column matches normalized property ──
        // Property has underscore: "User_Id", column "UserId"
        // → normalized column "UserId", normalized property "UserId" → exact ordinal match

        private class UserWithUnderscoreProp { public int User_Id { get; set; } }

        [Fact]
        public void MatchNamesWithUnderscores_Line229_NormalizedBothExact()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "UserId", 7 } }
                });
                conn.Open();

                // column "UserId" (no underscore) → normalized "UserId"
                // property "User_Id" → normalized "UserId" → exact ordinal match at line 229
                var result = conn.QueryFirst<UserWithUnderscoreProp>("SELECT UserId FROM T");
                Assert.Equal(7, result.User_Id);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        // ── MatchFirstOrDefault line 236: case-insensitive normalized both ──
        // Property "user_id" (lowercase), column "UserId" (PascalCase)
        // → normalized column "UserId", normalized property "userid" → case-insensitive match at line 236

        private class UserLowercase { public int user_id { get; set; } }

        [Fact]
        public void MatchNamesWithUnderscores_Line236_CaseInsensitiveNormalizedBoth()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "UserId", 99 } }
                });
                conn.Open();

                // column "UserId" → normalized "UserId"
                // property "user_id" → normalized "userid"
                // "UserId" != "userid" (case-sensitive), but case-insensitive → MATCH at line 236
                var result = conn.QueryFirst<UserLowercase>("SELECT UserId FROM T");
                Assert.Equal(99, result.user_id);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        // ── FindConstructor line 85: EqualsCIU path ────────────────────
        // ctor param "userId", column name "user_id" → EqualsCI fails, EqualsCIU succeeds

        private class WithCtorUnderscore
        {
            public int UserId { get; }
            public WithCtorUnderscore(int userId) { UserId = userId; }
        }

        [Fact]
        public void FindConstructor_EqualsCIU_WithUnderscoreColumnName()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                var map = new DefaultTypeMap(typeof(WithCtorUnderscore));
                // "user_id" → EqualsCIU("userId", "user_id") → strips underscores → "userid" == "userid" → true
                var ctor = map.FindConstructor(new[] { "user_id" }, new[] { typeof(int) });
                Assert.NotNull(ctor);
                Assert.Single(ctor!.GetParameters());
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        // ── GetMember field with underscore matching (lines 164-173) ───
        // column "user_id" → effective "userid" → matches field "userid" (no underscores)

        private class WithNoUnderscoreField
        {
            public int userid = 0;
        }

        [Fact]
        public void GetMember_Field_WithUnderscoreNormalization_DirectTest()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                // Directly test GetMember: column "user_id" → effective "userid" → matches field "userid"
                var map = new DefaultTypeMap(typeof(WithNoUnderscoreField));
                var member = map.GetMember("user_id");
                Assert.NotNull(member);
                Assert.Equal("userid", member!.Field?.Name);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        [Fact]
        public void GetMember_Field_WithUnderscoreNormalization_Query()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;
                SqlMapper.PurgeQueryCache();

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "user_id", 5 } }
                });
                conn.Open();

                // column "user_id" → effectiveColumnName "userid" → matches field "userid" (exact ordinal)
                var result = conn.QueryFirst<WithNoUnderscoreField>("SELECT user_id FROM T");
                Assert.Equal(5, result.userid);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
                SqlMapper.PurgeQueryCache();
            }
        }
    }
}
#endif
