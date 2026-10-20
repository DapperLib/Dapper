#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbParameterTests
    {
        [Fact]
        public void Execute_WithAnonymousParameters_Succeeds()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var rows = conn.Execute("UPDATE Users SET Name = @name WHERE Id = @id",
                new { id = 42, name = "Updated" });

            Assert.Equal(1, rows);
        }

        [Fact]
        public void Query_WithAnonymousParameters_ReturnsResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 42 }, { "Name", "Found" } }
            });
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users WHERE Id = @id",
                new { id = 42 }).ToList();

            Assert.Single(result);
            Assert.Equal(42, result[0].Id);
        }

        [Fact]
        public void Execute_WithDynamicParameters_Succeeds()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var dp = new DynamicParameters();
            dp.Add("id", 10);
            dp.Add("name", "DynUser");

            var rows = conn.Execute("UPDATE Users SET Name = @name WHERE Id = @id", dp);

            Assert.Equal(1, rows);
        }

        [Fact]
        public void Query_WithDynamicParameters_ReturnsResults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 10 }, { "Name", "DynUser" } }
            });
            conn.Open();

            var dp = new DynamicParameters();
            dp.Add("id", 10);

            var result = conn.Query<User>("SELECT Id, Name FROM Users WHERE Id = @id", dp).ToList();

            Assert.Single(result);
            Assert.Equal(10, result[0].Id);
        }

        [Fact]
        public void Execute_WithNullParameter_Succeeds()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            var rows = conn.Execute("UPDATE Users SET Name = @name WHERE Id = @id",
                new { id = 1, name = (string?)null });

            Assert.Equal(0, rows);
        }

        [Fact]
        public void ExecuteScalar_WithParameters_ReturnsValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(1);
            conn.Open();

            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Users WHERE Id = @id",
                new { id = 5 });

            Assert.Equal(1, exists);
        }
    }
}
#endif
