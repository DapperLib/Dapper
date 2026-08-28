#if !NET481
using System.Collections.Generic;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbExecuteTests
    {
        [Fact]
        public void Execute_ReturnsAffectedRowCount()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(3);
            conn.Open();

            var rows = conn.Execute("DELETE FROM Users WHERE Active = 0");

            Assert.Equal(3, rows);
        }

        [Fact]
        public void Execute_ReturnsZero_WhenNoRowsAffected()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            var rows = conn.Execute("UPDATE Users SET Name = 'x' WHERE Id = -1");

            Assert.Equal(0, rows);
        }

        [Fact]
        public void ExecuteScalar_ReturnsPreloadedValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(42L);
            conn.Open();

            var count = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");

            Assert.Equal(42L, count);
        }

        [Fact]
        public void ExecuteScalar_ReturnsString()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult("hello");
            conn.Open();

            var result = conn.ExecuteScalar<string>("SELECT Name FROM Users WHERE Id = 1");

            Assert.Equal("hello", result);
        }

        [Fact]
        public void ExecuteScalar_ReturnsDefault_WhenNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(null);
            conn.Open();

            var result = conn.ExecuteScalar<string?>("SELECT Name FROM Users WHERE Id = 99");

            Assert.Null(result);
        }

        [Fact]
        public void ExecuteReader_ReturnsReadableResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT Id, Name FROM Users");

            int count = 0;
            while (reader.Read()) count++;
            Assert.Equal(2, count);
        }
    }
}
#endif
