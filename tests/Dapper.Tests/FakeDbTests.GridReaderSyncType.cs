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
    /// Tests for GridReader sync Type-based overloads and 4/5/6/7-type Read multimap variants.
    /// </summary>
    public class FakeDbGridReaderSyncTypeTests
    {
        private class A { public int Id { get; set; } public string? Name { get; set; } }
        private class B { public int BId { get; set; } public string? BName { get; set; } }
        private class C { public int CId { get; set; } }
        private class D { public int DId { get; set; } }
        private class E { public int EId { get; set; } }
        private class F { public int FId { get; set; } }
        private class G { public int GId { get; set; } }

        private static fakeDbConnection MakeConn(IReadOnlyList<Dictionary<string, object?>> rows)
        {
            var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(rows);
            conn.Open();
            return conn;
        }

        // ── Sync Read(Type type) ──────────────────────────────────────

        [Fact]
        public void GridReader_Read_ByType_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var results = multi.Read(typeof(A)).Cast<A>().ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal("Alice", results[0].Name);
        }

        [Fact]
        public void GridReader_Read_ByType_Null_Throws()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<ArgumentNullException>(() => multi.Read(null!).ToList());
        }

        [Fact]
        public void GridReader_Read_ByType_Unbuffered_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var results = multi.Read(typeof(A), buffered: false).Cast<A>().ToList();

            Assert.Single(results);
            Assert.Equal(3, results[0].Id);
        }

        // ── Sync ReadFirst(Type) ──────────────────────────────────────

        [Fact]
        public void GridReader_ReadFirst_ByType_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Dave" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = (A)multi.ReadFirst(typeof(A));

            Assert.Equal(5, row.Id);
        }

        [Fact]
        public void GridReader_ReadFirst_ByType_Null_Throws()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<ArgumentNullException>(() => multi.ReadFirst(null!));
        }

        // ── Sync ReadFirstOrDefault(Type) ─────────────────────────────

        [Fact]
        public void GridReader_ReadFirstOrDefault_ByType_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 6 }, { "Name", "Eve" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = (A?)multi.ReadFirstOrDefault(typeof(A));

            Assert.NotNull(row);
            Assert.Equal(6, row!.Id);
        }

        [Fact]
        public void GridReader_ReadFirstOrDefault_ByType_EmptySet_ReturnsNull()
        {
            using var conn = MakeConn(Array.Empty<Dictionary<string, object?>>());

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = multi.ReadFirstOrDefault(typeof(A));

            Assert.Null(row);
        }

        [Fact]
        public void GridReader_ReadFirstOrDefault_ByType_Null_Throws()
        {
            using var conn = MakeConn(Array.Empty<Dictionary<string, object?>>());

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<ArgumentNullException>(() => multi.ReadFirstOrDefault(null!));
        }

        // ── Sync ReadSingle(Type) ─────────────────────────────────────

        [Fact]
        public void GridReader_ReadSingle_ByType_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Frank" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = (A)multi.ReadSingle(typeof(A));

            Assert.Equal(7, row.Id);
        }

        [Fact]
        public void GridReader_ReadSingle_ByType_Null_Throws()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<ArgumentNullException>(() => multi.ReadSingle(null!));
        }

        // ── Sync ReadSingleOrDefault(Type) ────────────────────────────

        [Fact]
        public void GridReader_ReadSingleOrDefault_ByType_EmptySet_ReturnsNull()
        {
            using var conn = MakeConn(Array.Empty<Dictionary<string, object?>>());

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = multi.ReadSingleOrDefault(typeof(A));

            Assert.Null(row);
        }

        [Fact]
        public void GridReader_ReadSingleOrDefault_ByType_Null_Throws()
        {
            using var conn = MakeConn(Array.Empty<Dictionary<string, object?>>());

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<ArgumentNullException>(() => multi.ReadSingleOrDefault(null!));
        }

        // ── 4-type Read multimap ──────────────────────────────────────

        [Fact]
        public void GridReader_Read_4Types_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 },
                    { "DId", 4 }
                }
            });

            using var multi = conn.QueryMultiple("SELECT ...");
            var results = multi.Read<A, B, C, D, string>(
                (a, b, c, d) => $"{a.Id}-{b.BId}-{c.CId}-{d.DId}",
                splitOn: "BId,CId,DId"
            ).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3-4", results[0]);
        }

        // ── 5-type Read multimap ──────────────────────────────────────

        [Fact]
        public void GridReader_Read_5Types_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 },
                    { "DId", 4 },
                    { "EId", 5 }
                }
            });

            using var multi = conn.QueryMultiple("SELECT ...");
            var results = multi.Read<A, B, C, D, E, string>(
                (a, b, c, d, e) => $"{a.Id}-{b.BId}-{c.CId}-{d.DId}-{e.EId}",
                splitOn: "BId,CId,DId,EId"
            ).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3-4-5", results[0]);
        }

        // ── 6-type Read multimap ──────────────────────────────────────

        [Fact]
        public void GridReader_Read_6Types_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 },
                    { "DId", 4 },
                    { "EId", 5 },
                    { "FId", 6 }
                }
            });

            using var multi = conn.QueryMultiple("SELECT ...");
            var results = multi.Read<A, B, C, D, E, F, string>(
                (a, b, c, d, e, f) => $"{a.Id}-{b.BId}-{c.CId}-{d.DId}-{e.EId}-{f.FId}",
                splitOn: "BId,CId,DId,EId,FId"
            ).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3-4-5-6", results[0]);
        }

        // ── 7-type Read multimap ──────────────────────────────────────

        [Fact]
        public void GridReader_Read_7Types_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "X" },
                    { "BId", 2 }, { "BName", "B" },
                    { "CId", 3 },
                    { "DId", 4 },
                    { "EId", 5 },
                    { "FId", 6 },
                    { "GId", 7 }
                }
            });

            using var multi = conn.QueryMultiple("SELECT ...");
            var results = multi.Read<A, B, C, D, E, F, G, string>(
                (a, b, c, d, e, f, g) => $"{a.Id}-{b.BId}-{c.CId}-{d.DId}-{e.EId}-{f.FId}-{g.GId}",
                splitOn: "BId,CId,DId,EId,FId,GId"
            ).ToList();

            Assert.Single(results);
            Assert.Equal("1-2-3-4-5-6-7", results[0]);
        }

        // ── Read<T>(buffered: false) for sync ─────────────────────────

        [Fact]
        public void GridReader_Read_Unbuffered_Sync_Works()
        {
            using var conn = MakeConn(new[]
            {
                new Dictionary<string, object?> { { "Id", 10 }, { "Name", "Grace" } },
                new Dictionary<string, object?> { { "Id", 11 }, { "Name", "Heidi" } },
            });

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var results = multi.Read<A>(buffered: false).ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal(10, results[0].Id);
        }
    }
}
#endif
