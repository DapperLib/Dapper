#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbDbStringTests
    {
        // ── DbString construction ──────────────────────────────────────

        [Fact]
        public void DbString_DefaultsToUnicode()
        {
            var s = new DbString { Value = "hello" };
            Assert.False(s.IsAnsi);
        }

        [Fact]
        public void DbString_ToString_ContainsValue()
        {
            var s = new DbString { Value = "world", IsAnsi = true, Length = 50 };
            var str = s.ToString();
            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void DbString_FixedLength_WithoutLength_ThrowsOnAddParameter()
        {
            var s = new DbString { Value = "x", IsFixedLength = true }; // Length stays -1
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            // DbString.AddParameter throws InvalidOperationException for fixed-length + length=-1
            Assert.Throws<InvalidOperationException>(() =>
                conn.Execute("SELECT @p", new { p = s }));
        }

        [Fact]
        public void DbString_AnsiVarChar_PassedAsParameter()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", "result" } }
            });
            conn.Open();

            var s = new DbString { Value = "search", IsAnsi = true, Length = 100 };
            var result = conn.QueryFirst<string>("SELECT @p AS Val", new { p = s });
            Assert.Equal("result", result);
        }

        [Fact]
        public void DbString_UnicodeFixedLength_PassedAsParameter()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", "result" } }
            });
            conn.Open();

            var s = new DbString { Value = "test", IsAnsi = false, IsFixedLength = true, Length = 50 };
            var result = conn.QueryFirst<string>("SELECT @p AS Val", new { p = s });
            Assert.Equal("result", result);
        }

        [Fact]
        public void DbString_AnsiFixedLength_PassedAsParameter()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", "ok" } }
            });
            conn.Open();

            var s = new DbString { Value = "test", IsAnsi = true, IsFixedLength = true, Length = 10 };
            var result = conn.QueryFirst<string>("SELECT @p AS Val", new { p = s });
            Assert.Equal("ok", result);
        }

        [Fact]
        public void DbString_NullValue_PassedAsParameter()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var s = new DbString { Value = null, IsAnsi = false };
            conn.Execute("UPDATE T SET Col = @p", new { p = s });
        }

        [Fact]
        public void DbString_LongString_AutoSizesToMax()
        {
            // String longer than DefaultLength (4000) should not truncate
            var longVal = new string('x', 5000);
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            var s = new DbString { Value = longVal, IsAnsi = false };
            conn.Execute("UPDATE T SET Col = @p", new { p = s });
        }

        // ── DbString used inline in query ─────────────────────────────

        [Fact]
        public void Query_WithDbStringParameter_Executes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var nameParam = new DbString { Value = "Alice", IsAnsi = false, Length = 50 };
            var result = conn.Query<User>(
                "SELECT Id, Name FROM Users WHERE Name = @name",
                new { name = nameParam }).ToList();

            Assert.Single(result);
        }
    }
}
#endif
