#if !NET481
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for GridReader multi-type Read methods (multi-map within QueryMultiple result sets).
    /// These cover MultiReadInternal and ReadDeferred paths.
    /// </summary>
    public class FakeDbGridReaderMultiMapTests
    {
        private class Owner { public int Id { get; set; } public string? Name { get; set; } }
        private class Pet { public int PetId { get; set; } public string? Breed { get; set; } }
        private class Tag { public int TagId { get; set; } public string? Label { get; set; } }

        // ── GridReader multi-type Read ─────────────────────────────────

        [Fact]
        public void GridReader_Read_TwoTypes_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "PetId", 10 }, { "Breed", "Labrador" }
                }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT ...");
            var result = multi.Read<Owner, Pet, (Owner, Pet)>(
                (o, p) => (o, p), splitOn: "PetId").ToList();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Item1.Name);
            Assert.Equal("Labrador", result[0].Item2.Breed);
        }

        [Fact]
        public void GridReader_Read_ThreeTypes_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "PetId", 10 }, { "Breed", "Lab" },
                    { "TagId", 100 }, { "Label", "Vaccinated" }
                }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT ...");
            var result = multi.Read<Owner, Pet, Tag, (Owner, Pet, Tag)>(
                (o, p, t) => (o, p, t), splitOn: "PetId,TagId").ToList();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Item1.Name);
            Assert.Equal("Lab", result[0].Item2.Breed);
            Assert.Equal("Vaccinated", result[0].Item3.Label);
        }

        [Fact]
        public void GridReader_Read_TypeArrayOverload_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 5 }, { "Name", "Bob" },
                    { "PetId", 50 }, { "Breed", "Pug" }
                }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT ...");
            var result = multi.Read<(Owner, Pet)>(
                new[] { typeof(Owner), typeof(Pet) },
                objs => ((Owner)objs[0], (Pet)objs[1]),
                splitOn: "PetId").ToList();

            Assert.Single(result);
            Assert.Equal("Bob", result[0].Item1.Name);
        }

        // ── GridReader unbuffered Read<T> ─────────────────────────────

        [Fact]
        public void GridReader_Read_Unbuffered_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Users");
            // buffered: false goes through ReadDeferred<T>
            var results = multi.Read<Owner>(buffered: false).ToList();

            Assert.Equal(2, results.Count);
        }

        // ── GridReader async multi-type reads ─────────────────────────

        [Fact]
        public async Task GridReader_ReadAsync_Generic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT ...");
            var results = (await multi.ReadAsync<Owner>()).ToList();

            Assert.Single(results);
            Assert.Equal("Carol", results[0].Name);
        }

        [Fact]
        public async Task GridReader_ReadAsync_WithType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 4 }, { "Name", "Dave" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT ...");
            var results = (await multi.ReadAsync(typeof(Owner))).ToList();

            Assert.Single(results);
            Assert.Equal("Dave", ((Owner)results[0]).Name);
        }
    }
}
#endif
