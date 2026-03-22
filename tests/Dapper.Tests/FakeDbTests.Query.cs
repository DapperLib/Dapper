#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbQueryTests
    {
        private static fakeDbConnection OpenConnection()
        {
            var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();
            return conn;
        }

        [Fact]
        public void Query_MapsColumnsToProperties()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users").ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Query_ReturnsMultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } },
            });
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users").ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Carol", result[2].Name);
        }

        [Fact]
        public void Query_ReturnsEmpty_WhenNoRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users").ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void Query_Dynamic_ReturnsExpandoObjects()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 42 }, { "Name", "Dynamic" } }
            });
            conn.Open();

            var result = conn.Query("SELECT Id, Name FROM Users").ToList();

            Assert.Single(result);
            Assert.Equal(42, (int)result[0].Id);
            Assert.Equal("Dynamic", (string)result[0].Name);
        }

        [Fact]
        public void QueryFirst_ReturnsFirstRow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 10 }, { "Name", "First" } },
                new Dictionary<string, object?> { { "Id", 20 }, { "Name", "Second" } },
            });
            conn.Open();

            var result = conn.QueryFirst<User>("SELECT Id, Name FROM Users");

            Assert.Equal(10, result.Id);
            Assert.Equal("First", result.Name);
        }

        [Fact]
        public void QueryFirst_ThrowsOnEmptyResult()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            Assert.Throws<InvalidOperationException>(() =>
                conn.QueryFirst<User>("SELECT Id, Name FROM Users"));
        }

        [Fact]
        public void QueryFirstOrDefault_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var result = conn.QueryFirstOrDefault<User>("SELECT Id, Name FROM Users");

            Assert.Null(result);
        }

        [Fact]
        public void QueryFirstOrDefault_ReturnsFirstRow_WhenMultiple()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            var result = conn.QueryFirstOrDefault<User>("SELECT Id, Name FROM Users");

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public void QuerySingle_ReturnsRow_WhenExactlyOne()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Solo" } }
            });
            conn.Open();

            var result = conn.QuerySingle<User>("SELECT Id, Name FROM Users WHERE Id = 7");

            Assert.Equal(7, result.Id);
            Assert.Equal("Solo", result.Name);
        }

        [Fact]
        public void QuerySingle_ThrowsOnEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            Assert.Throws<InvalidOperationException>(() =>
                conn.QuerySingle<User>("SELECT Id, Name FROM Users WHERE Id = 99"));
        }

        [Fact]
        public void QuerySingle_ThrowsOnMultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            Assert.Throws<InvalidOperationException>(() =>
                conn.QuerySingle<User>("SELECT Id, Name FROM Users"));
        }

        [Fact]
        public void QuerySingleOrDefault_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var result = conn.QuerySingleOrDefault<User>("SELECT Id, Name FROM Users WHERE Id = 99");

            Assert.Null(result);
        }

        [Fact]
        public void Query_MapsNullableColumnToNullProperty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", DBNull.Value } }
            });
            conn.Open();

            var result = conn.QueryFirst<User>("SELECT Id, Name FROM Users WHERE Id = 5");

            Assert.Equal(5, result.Id);
            Assert.Null(result.Name);
        }

        [Fact]
        public void Query_IsCaseInsensitive_ForColumnNames()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "id", 3 }, { "name", "Lower" } }
            });
            conn.Open();

            var result = conn.QueryFirst<User>("SELECT id, name FROM Users");

            Assert.Equal(3, result.Id);
            Assert.Equal("Lower", result.Name);
        }
    }
}
#endif
