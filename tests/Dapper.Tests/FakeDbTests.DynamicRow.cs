#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for DapperRow (the object returned by Query&lt;dynamic&gt;) covering
    /// its IDictionary interface, iteration, and metadata.
    /// </summary>
    public class FakeDbDynamicRowTests
    {
        // ── IDictionary<string, object> interface ─────────────────────

        [Fact]
        public void DynamicRow_CastToIDictionary_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var dict = (IDictionary<string, object>)row;

            Assert.Equal(1, dict["Id"]);
            Assert.Equal("Alice", dict["Name"]);
        }

        [Fact]
        public void DynamicRow_IDictionary_ContainsKey()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Bob" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var dict = (IDictionary<string, object>)row;

            Assert.True(dict.ContainsKey("Id"));
            Assert.True(dict.ContainsKey("Name"));
            Assert.False(dict.ContainsKey("Missing"));
        }

        [Fact]
        public void DynamicRow_IDictionary_Keys()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 }, { "C", 3 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT A, B, C FROM T").Single();
            var keys = ((IDictionary<string, object>)row).Keys.ToList();

            Assert.Equal(3, keys.Count);
            Assert.Contains("A", keys);
            Assert.Contains("B", keys);
            Assert.Contains("C", keys);
        }

        [Fact]
        public void DynamicRow_IDictionary_Values()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "X", 10 }, { "Y", 20 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT X, Y FROM T").Single();
            var values = ((IDictionary<string, object>)row).Values.ToList();

            Assert.Equal(2, values.Count);
            Assert.Contains(10, values);
            Assert.Contains(20, values);
        }

        [Fact]
        public void DynamicRow_IDictionary_Count()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 }, { "C", 3 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT A, B, C FROM T").Single();
            Assert.Equal(3, ((IDictionary<string, object>)row).Count);
        }

        [Fact]
        public void DynamicRow_IDictionary_TryGetValue_Found()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 42 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            var dict = (IDictionary<string, object>)row;
            var found = dict.TryGetValue("Id", out var val);

            Assert.True(found);
            Assert.Equal(42, val);
        }

        [Fact]
        public void DynamicRow_IDictionary_TryGetValue_NotFound()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            var dict = (IDictionary<string, object>)row;
            var found = dict.TryGetValue("Missing", out var val);

            Assert.False(found);
            Assert.Null(val);
        }

        // ── Enumeration ───────────────────────────────────────────────

        [Fact]
        public void DynamicRow_Enumeration_YieldsKeyValuePairs()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var pairs = ((IEnumerable<KeyValuePair<string, object>>)row).ToList();

            Assert.Equal(2, pairs.Count);
            Assert.Contains(pairs, p => p.Key == "Id" && (int)p.Value == 1);
            Assert.Contains(pairs, p => p.Key == "Name" && (string)p.Value == "Alice");
        }

        [Fact]
        public void DynamicRow_MultipleRows_EachEnumerable()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            var rows = conn.Query<dynamic>("SELECT Id, Name FROM T").ToList();
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object>)row;
                Assert.True(dict.ContainsKey("Id"));
                Assert.True(dict.ContainsKey("Name"));
            }
        }

        // ── Dynamic member access (exercises DapperRowMetaObject) ─────

        [Fact]
        public void DynamicRow_DynamicAccess_MemberNotFound_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            dynamic row = conn.Query<dynamic>("SELECT Id FROM T").Single();

            // Accessing a member that doesn't exist in the result set should return null
            Assert.Null(row.DoesNotExist);
        }

        [Fact]
        public void DynamicRow_SetValue_ViaIDictionary()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var dict = (IDictionary<string, object>)row;

            // Updating an existing key
            dict["Name"] = "Updated";
            Assert.Equal("Updated", dict["Name"]);
        }

        [Fact]
        public void DynamicRow_IDictionary_IsReadOnly_IsFalse()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            Assert.False(((IDictionary<string, object>)row).IsReadOnly);
        }

        [Fact]
        public void DynamicRow_IDictionary_Remove_ThenNotContains()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var dict = (IDictionary<string, object>)row;

            dict.Remove("Name");
            Assert.False(dict.ContainsKey("Name"));
        }

        [Fact]
        public void DynamicRow_IDictionary_Add_NewKey()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            var dict = (IDictionary<string, object>)row;

            dict["NewKey"] = "NewValue";
            Assert.True(dict.ContainsKey("NewKey"));
            Assert.Equal("NewValue", dict["NewKey"]);
        }

        [Fact]
        public void DynamicRow_IDictionary_CopyTo_Array()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var coll = (ICollection<KeyValuePair<string, object>>)row;
            var array = new KeyValuePair<string, object>[2];
            coll.CopyTo(array, 0);

            Assert.Equal(2, array.Length);
        }

        // ── DapperTable sharing ───────────────────────────────────────

        [Fact]
        public void DynamicRow_MultipleRows_ShareSameTableSchema()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "C" } },
            });
            conn.Open();

            var rows = conn.Query<dynamic>("SELECT Id, Name FROM T").ToList();
            Assert.Equal(3, rows.Count);

            // All rows should expose same columns
            foreach (dynamic row in rows)
            {
                var dict = (IDictionary<string, object>)row;
                Assert.True(dict.ContainsKey("Id"));
                Assert.True(dict.ContainsKey("Name"));
            }
        }
    }
}
#endif
