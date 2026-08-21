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
    /// Tests for Parse extension methods on IDataReader and GetRowParser.
    /// These cover the SqlMapper.IDataReader.cs partial class.
    /// </summary>
    public class FakeDbReaderParserTests
    {
        private class User { public int Id { get; set; } public string? Name { get; set; } }

        // ── Parse<T>(IDataReader) ─────────────────────────────────────

        [Fact]
        public void IDataReader_Parse_Generic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Id, Name FROM Users");
            var results = reader.Parse<User>().ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal("Alice", results[0].Name);
            Assert.Equal("Bob", results[1].Name);
        }

        [Fact]
        public void IDataReader_Parse_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Charlie" } }
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Id, Name FROM Users");
            var results = reader.Parse(typeof(User)).ToList();

            Assert.Single(results);
            Assert.Equal(5, ((User)results[0]).Id);
        }

        [Fact]
        public void IDataReader_Parse_Dynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Dave" } }
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Id, Name FROM Users");
            var results = reader.Parse().ToList();

            Assert.Single(results);
            Assert.Equal(7, (int)results[0].Id);
        }

        // ── GetRowParser<T>(IDataReader) ──────────────────────────────

        [Fact]
        public void IDataReader_GetRowParser_Generic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Eve" } }
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Id, Name FROM Users");
            var parser = reader.GetRowParser<User>();

            Assert.True(reader.Read());
            var user = parser(reader);
            Assert.Equal(3, user.Id);
            Assert.Equal("Eve", user.Name);
        }

        [Fact]
        public void IDataReader_GetRowParser_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 9 }, { "Name", "Frank" } }
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Id, Name FROM Users");
            var parser = reader.GetRowParser<User>(typeof(User));

            Assert.True(reader.Read());
            var user = parser(reader);
            Assert.Equal(9, user.Id);
        }

        // ── GetRowParser<T>(DbDataReader) ─────────────────────────────

        [Fact]
        public void DbDataReader_GetRowParser_Generic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 11 }, { "Name", "Grace" } }
            });
            conn.Open();

            using var dbReader = (System.Data.Common.DbDataReader)conn.ExecuteReader("SELECT Id, Name FROM Users");
            var parser = dbReader.GetRowParser<User>();

            Assert.True(dbReader.Read());
            var user = parser(dbReader);
            Assert.Equal(11, user.Id);
        }

        // ── Parse<T> with scalar types ────────────────────────────────

        [Fact]
        public void IDataReader_Parse_Scalar_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", 42 } },
                new Dictionary<string, object?> { { "Val", 43 } },
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Val FROM T");
            var results = reader.Parse<int>().ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal(42, results[0]);
            Assert.Equal(43, results[1]);
        }
    }
}
#endif
