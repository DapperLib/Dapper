#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DbString coverage: value+length constructor, parameter reuse branch,
    /// fixed-length modes, and AddParameter with existing parameter.
    /// </summary>
    public class FakeDbDbStringAdvancedTests
    {
        // ── DbString(value, length) constructor ──────────────────────

        [Fact]
        public void DbString_ValueLength_Constructor_Works()
        {
            var s = new DbString("hello", 50);
            Assert.Equal("hello", s.Value);
            Assert.Equal(50, s.Length);
            // IsAnsi defaults to IsAnsiDefault
            Assert.Equal(DbString.IsAnsiDefault, s.IsAnsi);
        }

        [Fact]
        public void DbString_ValueLength_NullValue_Works()
        {
            var s = new DbString(null, 100);
            Assert.Null(s.Value);
            Assert.Equal(100, s.Length);
        }

        // ── DbString AddParameter with IsFixedLength + length specified ─

        [Fact]
        public void DbString_FixedLength_AddParameter_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            // IsFixedLength requires Length to be set
            var dp = new DynamicParameters();
            dp.Add("name", new DbString { Value = "hi", IsFixedLength = true, Length = 10, IsAnsi = false });
            conn.Execute("UPDATE T SET Name = @name", dp);
        }

        [Fact]
        public void DbString_AnsiFixedLength_AddParameter_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var dp = new DynamicParameters();
            dp.Add("name", new DbString { Value = "hi", IsFixedLength = true, Length = 10, IsAnsi = true });
            conn.Execute("UPDATE T SET Name = @name", dp);
        }

        [Fact]
        public void DbString_IsFixedLength_NoLength_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            // Length == -1 and IsFixedLength == true should throw
            var dp = new DynamicParameters();
            dp.Add("name", new DbString { Value = "hi", IsFixedLength = true, Length = -1 });
            Assert.Throws<InvalidOperationException>(() =>
                conn.Execute("UPDATE T SET Name = @name", dp));
        }

        [Fact]
        public void DbString_LongValue_SetsExplicitLength()
        {
            // When Length == -1 and Value.Length > DefaultLength, Size = Length (=-1, meaning max)
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var longValue = new string('x', DbString.DefaultLength + 1);
            var dp = new DynamicParameters();
            dp.Add("name", new DbString { Value = longValue, Length = -1, IsAnsi = false });
            conn.Execute("UPDATE T SET Name = @name", dp);
        }

        // ── DbString via Query ────────────────────────────────────────

        [Fact]
        public void Query_WithDbString_Param_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var result = conn.Query<int>("SELECT Id FROM T WHERE Name = @name",
                new { name = new DbString { Value = "Alice", IsAnsi = true } }).ToList();
            Assert.Single(result);
        }

        // ── IsAnsiDefault static property ────────────────────────────

        [Fact]
        public void DbString_IsAnsiDefault_CanBeChanged()
        {
            var original = DbString.IsAnsiDefault;
            try
            {
                DbString.IsAnsiDefault = !original;
                var s = new DbString("test");
                Assert.Equal(!original, s.IsAnsi);
            }
            finally
            {
                DbString.IsAnsiDefault = original;
            }
        }
    }
}
#endif
