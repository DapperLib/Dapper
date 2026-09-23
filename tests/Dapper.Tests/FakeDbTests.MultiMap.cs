#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbMultiMapTests
    {
        // Two-type split: columns before splitOn go to TFirst, from splitOn onward to TSecond.

        private class Owner { public int Id { get; set; } public string? Name { get; set; } }
        private class Pet   { public int PetId { get; set; } public string? Breed { get; set; } }

        [Fact]
        public void Query_TwoTypeSplit_MapsCorrectly()
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

            var result = conn.Query<Owner, Pet, (Owner, Pet)>(
                "SELECT o.Id, o.Name, p.PetId, p.Breed FROM Owners o JOIN Pets p ON ...",
                (owner, pet) => (owner, pet),
                splitOn: "PetId").ToList();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Item1.Name);
            Assert.Equal("Labrador", result[0].Item2.Breed);
        }

        [Fact]
        public void Query_TwoTypeSplit_MultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" }, { "PetId", 10 }, { "Breed", "Lab" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" },   { "PetId", 20 }, { "Breed", "Pug" } },
            });
            conn.Open();

            var result = conn.Query<Owner, Pet, (Owner, Pet)>(
                "SELECT ...",
                (o, p) => (o, p),
                splitOn: "PetId").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Alice", result[0].Item1.Name);
            Assert.Equal("Bob",   result[1].Item1.Name);
        }

        private class Tag { public int TagId { get; set; } public string? Label { get; set; } }

        [Fact]
        public void Query_ThreeTypeSplit_MapsCorrectly()
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

            var result = conn.Query<Owner, Pet, Tag, (Owner, Pet, Tag)>(
                "SELECT ...",
                (o, p, t) => (o, p, t),
                splitOn: "PetId,TagId").ToList();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Item1.Name);
            Assert.Equal("Lab", result[0].Item2.Breed);
            Assert.Equal("Vaccinated", result[0].Item3.Label);
        }

        [Fact]
        public void Query_TwoTypeSplit_ExplicitSplitOn_SingleRow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 5 }, { "Name", "Charlie" },
                    { "PetId", 50 }, { "Breed", "Beagle" }
                }
            });
            conn.Open();

            var result = conn.Query<Owner, Pet, string>(
                "SELECT ...",
                (o, p) => $"{o.Name}:{p.Breed}",
                splitOn: "PetId").ToList();

            Assert.Single(result);
            Assert.Equal("Charlie:Beagle", result[0]);
        }

        // ── async multi-map ───────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_TwoTypeSplit_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 3 }, { "Name", "Dave" },
                    { "PetId", 30 }, { "Breed", "Corgi" }
                }
            });
            conn.Open();

            var result = (await conn.QueryAsync<Owner, Pet, (Owner, Pet)>(
                "SELECT ...",
                (o, p) => (o, p),
                splitOn: "PetId")).ToList();

            Assert.Single(result);
            Assert.Equal("Dave", result[0].Item1.Name);
            Assert.Equal("Corgi", result[0].Item2.Breed);
        }

        // ── Type[] + Func<object[], T> overload ───────────────────────

        [Fact]
        public void Query_TypeArrayOverload_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 7 }, { "Name", "Eve" },
                    { "PetId", 70 }, { "Breed", "Dachshund" }
                }
            });
            conn.Open();

            var result = conn.Query<(Owner, Pet)>(
                "SELECT ...",
                new[] { typeof(Owner), typeof(Pet) },
                objs => ((Owner)objs[0], (Pet)objs[1]),
                splitOn: "PetId").ToList();

            Assert.Single(result);
            Assert.Equal("Eve", result[0].Item1.Name);
        }
    }
}
#endif
