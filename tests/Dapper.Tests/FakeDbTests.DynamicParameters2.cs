#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbDynamicParameterTests
    {
        // ── ParameterNames ────────────────────────────────────────────

        [Fact]
        public void DynamicParameters_ParameterNames_ReflectsAdded()
        {
            var dp = new DynamicParameters();
            dp.Add("Foo", 1);
            dp.Add("Bar", 2);

            var names = dp.ParameterNames.ToList();
            Assert.Contains("Foo", names);
            Assert.Contains("Bar", names);
        }

        [Fact]
        public void DynamicParameters_ParameterNames_Empty_WhenNoParams()
        {
            var dp = new DynamicParameters();
            Assert.Empty(dp.ParameterNames);
        }

        // ── Add overloads ─────────────────────────────────────────────

        [Fact]
        public void DynamicParameters_Add_WithDbType()
        {
            var dp = new DynamicParameters();
            dp.Add("@p", "hello", DbType.String);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", "hello" } }
            });
            conn.Open();

            var result = conn.QueryFirst<string>("SELECT @p AS Val", dp);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void DynamicParameters_Add_WithSize()
        {
            var dp = new DynamicParameters();
            dp.Add("name", "Alice", DbType.String, ParameterDirection.Input, 50);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("UPDATE T SET Name = @name", dp);
        }

        [Fact]
        public void DynamicParameters_Add_WithPrecisionAndScale()
        {
            var dp = new DynamicParameters();
            dp.Add("amount", 99.99m, DbType.Decimal, ParameterDirection.Input, null, 10, 2);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("INSERT INTO Orders (Amount) VALUES (@amount)", dp);
        }

        // ── Get<T> ────────────────────────────────────────────────────

        [Fact]
        public void DynamicParameters_Get_ReturnsValue_AfterExecute()
        {
            var dp = new DynamicParameters();
            dp.Add("val", 42);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("SELECT @val", dp);

            Assert.Equal(42, dp.Get<int>("val"));
        }

        [Fact]
        public void DynamicParameters_Get_WithAtPrefix_StillWorks()
        {
            var dp = new DynamicParameters();
            dp.Add("@id", 7);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("SELECT @id", dp);

            Assert.Equal(7, dp.Get<int>("@id")); // @ should be stripped internally
        }

        // ── Template constructor ──────────────────────────────────────

        [Fact]
        public void DynamicParameters_FromObject_Template()
        {
            var template = new { id = 10, name = "Alice" };
            var dp = new DynamicParameters(template);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 10 }, { "Name", "Alice" } }
            });
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users WHERE Id = @id AND Name = @name", dp)
                             .ToList();
            Assert.Single(result);
        }

        [Fact]
        public void DynamicParameters_FromDictionary_Template()
        {
            var dict = new Dictionary<string, object?> { { "id", 5 } };
            var dp = new DynamicParameters(dict);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 5 }, { "Name", "E" } } });
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users WHERE Id = @id", dp).ToList();
            Assert.Single(result);
        }

        [Fact]
        public void DynamicParameters_MergedWithAnother()
        {
            var dp1 = new DynamicParameters();
            dp1.Add("a", 1);

            var dp2 = new DynamicParameters(dp1);
            dp2.Add("b", 2);

            var names = dp2.ParameterNames.ToList();
            Assert.Contains("a", names);
            Assert.Contains("b", names);
        }

        // ── RemoveUnused ─────────────────────────────────────────────

        [Fact]
        public void DynamicParameters_RemoveUnused_Default_IsTrue()
        {
            var dp = new DynamicParameters();
            Assert.True(dp.RemoveUnused);
        }

        [Fact]
        public void DynamicParameters_Execute_WithUnusedParam_RemoveUnused_True()
        {
            // RemoveUnused=true: params not referenced in SQL are removed
            var dp = new DynamicParameters();
            dp.Add("used", 1);
            dp.Add("unused", 99);
            dp.RemoveUnused = true;

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            // Should not throw even with the unused param
            conn.Execute("SELECT @used", dp);
        }

        // ── IParameterLookup indexer ──────────────────────────────────

        [Fact]
        public void DynamicParameters_Indexer_ReturnsValue()
        {
            var dp = new DynamicParameters();
            dp.Add("key", "value");

            SqlMapper.IParameterLookup lookup = dp;
            Assert.Equal("value", lookup["key"]);
        }

        [Fact]
        public void DynamicParameters_Indexer_ReturnsNull_ForMissingKey()
        {
            var dp = new DynamicParameters();
            SqlMapper.IParameterLookup lookup = dp;
            Assert.Null(lookup["missing"]);
        }
    }
}
#endif
