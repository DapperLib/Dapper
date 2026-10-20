#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for async multimap 3/4/5/6/7-type variants (string SQL and CommandDefinition),
    /// and QueryAsync/QueryFirstAsync/QuerySingleAsync with Type+CommandDefinition overloads.
    /// </summary>
    public class FakeDbAsyncMultimapTests
    {
        private class A { public int Id { get; set; } public string? Name { get; set; } }
        private class B { public int BId { get; set; } public string? BName { get; set; } }
        private class C { public int CId { get; set; } }
        private class D { public int DId { get; set; } }
        private class E { public int EId { get; set; } }
        private class F { public int FId { get; set; } }
        private class G { public int GId { get; set; } }

        private static Dictionary<string, object?> MakeRow7() => new()
        {
            { "Id", 1 }, { "Name", "Alice" },
            { "BId", 2 }, { "BName", "Brow" },
            { "CId", 3 }, { "DId", 4 }, { "EId", 5 }, { "FId", 6 }, { "GId", 7 }
        };

        // ── QueryAsync(Type, CommandDefinition) ───────────────────────

        [Fact]
        public async Task QueryAsync_Type_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var results = (await conn.QueryAsync(typeof(A), cmd)).Cast<A>().ToList();

            Assert.Single(results);
            Assert.Equal(1, results[0].Id);
        }

        [Fact]
        public async Task QueryFirstAsync_Type_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = (A)await conn.QueryFirstAsync(typeof(A), cmd);

            Assert.Equal(2, row.Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_Type_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = await conn.QueryFirstOrDefaultAsync(typeof(A), cmd);

            Assert.NotNull(row);
            Assert.Equal(3, ((A)row!).Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_Type_CommandDefinition_Empty_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = await conn.QueryFirstOrDefaultAsync(typeof(A), cmd);

            Assert.Null(row);
        }

        [Fact]
        public async Task QuerySingleAsync_Type_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 4 }, { "Name", "Dave" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = (A)await conn.QuerySingleAsync(typeof(A), cmd);

            Assert.Equal(4, row.Id);
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_Type_CommandDefinition_Empty_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = await conn.QuerySingleOrDefaultAsync(typeof(A), cmd);

            Assert.Null(row);
        }

        // ── Async 3-type CommandDefinition ────────────────────────────

        [Fact]
        public async Task QueryAsync_3Types_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }
                }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var results = (await conn.QueryAsync<A, B, C, string>(
                cmd,
                (a, b, c) => $"{a.Id}-{b.BId}-{c.CId}",
                splitOn: "BId,CId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3", results[0]);
        }

        // ── Async 4-type string SQL ───────────────────────────────────

        [Fact]
        public async Task QueryAsync_4Types_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }, { "DId", 4 }
                }
            });
            conn.Open();

            var results = (await conn.QueryAsync<A, B, C, D, string>(
                "SELECT ...",
                (a, b, c, d) => $"{a.Id}-{b.BId}-{c.CId}-{d.DId}",
                splitOn: "BId,CId,DId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3-4", results[0]);
        }

        // ── Async 4-type CommandDefinition ────────────────────────────

        [Fact]
        public async Task QueryAsync_4Types_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }, { "DId", 4 }
                }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var results = (await conn.QueryAsync<A, B, C, D, string>(
                cmd,
                (a, b, c, d) => $"{a.Id}-{b.BId}-{c.CId}-{d.DId}",
                splitOn: "BId,CId,DId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3-4", results[0]);
        }

        // ── Async 5-type string SQL ───────────────────────────────────

        [Fact]
        public async Task QueryAsync_5Types_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }, { "DId", 4 }, { "EId", 5 }
                }
            });
            conn.Open();

            var results = (await conn.QueryAsync<A, B, C, D, E, string>(
                "SELECT ...",
                (a, b, c, d, e) => $"{a.Id}-{e.EId}",
                splitOn: "BId,CId,DId,EId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-5", results[0]);
        }

        // ── Async 5-type CommandDefinition ────────────────────────────

        [Fact]
        public async Task QueryAsync_5Types_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }, { "DId", 4 }, { "EId", 5 }
                }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var results = (await conn.QueryAsync<A, B, C, D, E, string>(
                cmd,
                (a, b, c, d, e) => $"{a.Id}-{e.EId}",
                splitOn: "BId,CId,DId,EId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-5", results[0]);
        }

        // ── Async 6-type string SQL ───────────────────────────────────

        [Fact]
        public async Task QueryAsync_6Types_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }, { "DId", 4 }, { "EId", 5 }, { "FId", 6 }
                }
            });
            conn.Open();

            var results = (await conn.QueryAsync<A, B, C, D, E, F, string>(
                "SELECT ...",
                (a, b, c, d, e, f) => $"{a.Id}-{f.FId}",
                splitOn: "BId,CId,DId,EId,FId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-6", results[0]);
        }

        // ── Async 6-type CommandDefinition ────────────────────────────

        [Fact]
        public async Task QueryAsync_6Types_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 }, { "DId", 4 }, { "EId", 5 }, { "FId", 6 }
                }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var results = (await conn.QueryAsync<A, B, C, D, E, F, string>(
                cmd,
                (a, b, c, d, e, f) => $"{a.Id}-{f.FId}",
                splitOn: "BId,CId,DId,EId,FId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-6", results[0]);
        }

        // ── Async 7-type string SQL ───────────────────────────────────

        [Fact]
        public async Task QueryAsync_7Types_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { MakeRow7() });
            conn.Open();

            var results = (await conn.QueryAsync<A, B, C, D, E, F, G, string>(
                "SELECT ...",
                (a, b, c, d, e, f, g) => $"{a.Id}-{g.GId}",
                splitOn: "BId,CId,DId,EId,FId,GId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-7", results[0]);
        }

        // ── Async 7-type CommandDefinition ────────────────────────────

        [Fact]
        public async Task QueryAsync_7Types_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { MakeRow7() });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var results = (await conn.QueryAsync<A, B, C, D, E, F, G, string>(
                cmd,
                (a, b, c, d, e, f, g) => $"{a.Id}-{g.GId}",
                splitOn: "BId,CId,DId,EId,FId,GId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-7", results[0]);
        }

        // ── Async 2-type CommandDefinition ────────────────────────────

        [Fact]
        public async Task QueryAsync_2Types_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "BId", 2 }, { "BName", "B" }
                }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var results = (await conn.QueryAsync<A, B, string>(
                cmd,
                (a, b) => $"{a.Id}-{b.BId}",
                splitOn: "BId"
            )).ToList();

            Assert.Single(results);
            Assert.Equal("1-2", results[0]);
        }
    }
}
#endif
