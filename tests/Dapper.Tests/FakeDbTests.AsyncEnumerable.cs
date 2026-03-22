#if !NET481
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for QueryUnbufferedAsync (IAsyncEnumerable) and related async enumerable paths.
    /// </summary>
    public class FakeDbAsyncEnumerableTests
    {
        private class User { public int Id { get; set; } public string? Name { get; set; } }

        [Fact]
        public async Task QueryUnbufferedAsync_SingleRow_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var results = new List<User>();
            await foreach (var item in conn.QueryUnbufferedAsync<User>("SELECT Id, Name FROM Users"))
            {
                results.Add(item);
            }

            Assert.Single(results);
            Assert.Equal(1, results[0].Id);
            Assert.Equal("Alice", results[0].Name);
        }

        [Fact]
        public async Task QueryUnbufferedAsync_MultipleRows_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } },
            });
            conn.Open();

            var results = new List<User>();
            await foreach (var item in conn.QueryUnbufferedAsync<User>("SELECT Id, Name FROM Users"))
            {
                results.Add(item);
            }

            Assert.Equal(3, results.Count);
            Assert.Equal("Alice", results[0].Name);
            Assert.Equal("Bob", results[1].Name);
            Assert.Equal("Carol", results[2].Name);
        }

        [Fact]
        public async Task QueryUnbufferedAsync_EmptyResult_ReturnsNothing()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(System.Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var results = new List<User>();
            await foreach (var item in conn.QueryUnbufferedAsync<User>("SELECT Id, Name FROM Users"))
            {
                results.Add(item);
            }

            Assert.Empty(results);
        }

        [Fact]
        public async Task QueryUnbufferedAsync_WithParameters_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Dave" } }
            });
            conn.Open();

            var results = new List<User>();
            await foreach (var item in conn.QueryUnbufferedAsync<User>(
                "SELECT Id, Name FROM Users WHERE Id = @id", new { id = 5 }))
            {
                results.Add(item);
            }

            Assert.Single(results);
            Assert.Equal(5, results[0].Id);
        }

        [Fact]
        public async Task QueryUnbufferedAsync_CollectAll_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            var results = new List<User>();
            await foreach (var item in conn.QueryUnbufferedAsync<User>("SELECT Id, Name FROM Users"))
                results.Add(item);

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task QueryUnbufferedAsync_Dynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Eve" } }
            });
            conn.Open();

            var results = new List<dynamic>();
            await foreach (var item in conn.QueryUnbufferedAsync("SELECT Id, Name FROM T"))
            {
                results.Add(item);
            }

            Assert.Single(results);
            Assert.Equal(7, (int)results[0].Id);
        }
    }
}
#endif
