#if !NET481
using System;
using System.Collections.Generic;
using System.Reflection;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DefaultTypeMap tests: base-class property setter, FindConstructor with matching params,
    /// GetMember underscore paths, and MatchNamesWithUnderscores deep matching.
    /// </summary>
    public class FakeDbDefaultTypeMapAdvancedTests
    {
        // ── GetPropertySetter — property from base class ───────────────

        private abstract class Base { public int BaseId { get; set; } }
        private class Derived : Base { public string? Name { get; set; } }

        [Fact]
        public void GetPropertySetter_BaseClassProperty_Works()
        {
            // GetPropertySetter takes different path when DeclaringType != type
            var prop = typeof(Derived).GetProperty("BaseId")!;
            // BaseId is declared on Base, not Derived
            Assert.Equal(typeof(Base), prop.DeclaringType);

            var setter = DefaultTypeMap.GetPropertySetter(prop, typeof(Derived));
            Assert.NotNull(setter);
        }

        [Fact]
        public void GetPropertySetterOrThrow_BaseClassProperty_Works()
        {
            var prop = typeof(Derived).GetProperty("BaseId")!;
            var setter = DefaultTypeMap.GetPropertySetterOrThrow(prop, typeof(Derived));
            Assert.NotNull(setter);
        }

        // ── FindConstructor — with parameter names/types ───────────────
        // Must use a class WITHOUT a parameterless ctor, because FindConstructor
        // returns the no-arg ctor first when it exists.

        private class NoDefaultCtor
        {
            public int Id { get; }
            public string? Name { get; }
            public NoDefaultCtor(int id) { Id = id; }
            public NoDefaultCtor(int id, string name) { Id = id; Name = name; }
        }

        [Fact]
        public void FindConstructor_WithParams_Matches()
        {
            var map = new DefaultTypeMap(typeof(NoDefaultCtor));
            var ctor = map.FindConstructor(new[] { "id", "name" }, new[] { typeof(int), typeof(string) });
            Assert.NotNull(ctor);
            Assert.Equal(2, ctor!.GetParameters().Length);
        }

        [Fact]
        public void FindConstructor_SingleParam_Matches()
        {
            var map = new DefaultTypeMap(typeof(NoDefaultCtor));
            var ctor = map.FindConstructor(new[] { "id" }, new[] { typeof(int) });
            Assert.NotNull(ctor);
            Assert.Single(ctor!.GetParameters());
        }

        [Fact]
        public void FindConstructor_NoMatch_ReturnsNull()
        {
            var map = new DefaultTypeMap(typeof(NoDefaultCtor));
            // No constructor matching (double) type
            var ctor = map.FindConstructor(new[] { "nonexistent" }, new[] { typeof(double) });
            Assert.Null(ctor);
        }

        // ── GetConstructorParameter — throw path ───────────────────────

        [Fact]
        public void GetConstructorParameter_MissingName_Throws()
        {
            var map = new DefaultTypeMap(typeof(NoDefaultCtor));
            var ctor = map.FindConstructor(new[] { "id" }, new[] { typeof(int) })!;
            Assert.Throws<ArgumentException>(() => map.GetConstructorParameter(ctor, "nonexistent"));
        }

        // ── GetMember — underscore field matching ──────────────────────

        private class WithUnderscoreField
        {
            public int user_id = 0;
            public string? first_name = null;
        }

        [Fact]
        public void GetMember_Underscore_Field_ExactMatch()
        {
            var map = new DefaultTypeMap(typeof(WithUnderscoreField));
            var member = map.GetMember("user_id");
            Assert.NotNull(member);
        }

        [Fact]
        public void GetMember_Underscore_Field_WithMatchNamesWithUnderscores()
        {
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                // UserId -> user_id (strip underscores from field name to match column)
                var map = new DefaultTypeMap(typeof(WithUnderscoreField));
                // Column "user_id" should match field "user_id" exactly (no need for underscore stripping)
                var member = map.GetMember("user_id");
                Assert.NotNull(member);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        // ── MatchNamesWithUnderscores — query path ─────────────────────

        private class SnakeUser
        {
            public int UserId { get; set; }
            public string? FirstName { get; set; }
        }

        [Fact]
        public void DefaultTypeMap_MatchNamesWithUnderscores_Query_CaseInsensitive()
        {
            // Tests underscore matching in MatchFirstOrDefault (lines 214-240)
            var original = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> {
                        { "USER_ID", 42 },
                        { "FIRST_NAME", "Grace" }
                    }
                });
                conn.Open();

                var result = conn.QueryFirst<SnakeUser>("SELECT user_id, first_name FROM T");
                Assert.Equal(42, result.UserId);
                Assert.Equal("Grace", result.FirstName);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = original;
            }
        }

        // ── FindExplicitConstructor — type with attribute ──────────────

        private class WithExplicitCtor
        {
            public int Id { get; }
            [ExplicitConstructor]
            public WithExplicitCtor(int id) { Id = id; }
        }

        [Fact]
        public void FindExplicitConstructor_WithAttribute_ReturnsIt()
        {
            var map = new DefaultTypeMap(typeof(WithExplicitCtor));
            var ctor = map.FindExplicitConstructor();
            Assert.NotNull(ctor);
            Assert.Single(ctor!.GetParameters());
        }

        // ── GetPropertySetter throw ────────────────────────────────────

        private class GetOnlyProp { public int Val { get; } }

        [Fact]
        public void GetPropertySetterOrThrow_NoSetter_Throws()
        {
            // GetPropertySetterOrThrow throws InvalidOperationException when no setter
            var prop = typeof(GetOnlyProp).GetProperty("Val")!;
            Assert.Throws<InvalidOperationException>(() =>
                DefaultTypeMap.GetPropertySetterOrThrow(prop, typeof(GetOnlyProp)));
        }
    }
}
#endif
