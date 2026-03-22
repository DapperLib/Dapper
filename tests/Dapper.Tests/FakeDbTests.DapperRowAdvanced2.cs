#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DapperRow coverage: IReadOnlyDictionary interface, DeadValue paths,
    /// ToString with null values, ICollection&lt;KVP&gt;.Add, IDictionary.Add (isAdd path).
    /// </summary>
    public class FakeDbDapperRowAdvanced2Tests
    {
        private static dynamic GetRow(Dictionary<string, object?> data)
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { data });
            conn.Open();
            return conn.QueryFirst("SELECT * FROM T");
        }

        // ── IReadOnlyDictionary<string, object> interface ─────────────

        [Fact]
        public void DapperRow_IReadOnlyDict_ContainsKey_True()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
            IReadOnlyDictionary<string, object?> rod = (IReadOnlyDictionary<string, object?>)(object)row;
            Assert.True(rod.ContainsKey("Id"));
        }

        [Fact]
        public void DapperRow_IReadOnlyDict_ContainsKey_False()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 } });
            IReadOnlyDictionary<string, object?> rod = (IReadOnlyDictionary<string, object?>)(object)row;
            Assert.False(rod.ContainsKey("Missing"));
        }

        [Fact]
        public void DapperRow_IReadOnlyDict_Indexer()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 42 } });
            IReadOnlyDictionary<string, object?> rod = (IReadOnlyDictionary<string, object?>)(object)row;
            Assert.Equal(42, rod["Id"]);
        }

        [Fact]
        public void DapperRow_IReadOnlyDict_Keys()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
            IReadOnlyDictionary<string, object?> rod = (IReadOnlyDictionary<string, object?>)(object)row;
            var keys = rod.Keys.ToList();
            Assert.Contains("Id", keys);
            Assert.Contains("Name", keys);
        }

        [Fact]
        public void DapperRow_IReadOnlyDict_Values()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 5 } });
            IReadOnlyDictionary<string, object?> rod = (IReadOnlyDictionary<string, object?>)(object)row;
            var vals = rod.Values.ToList();
            Assert.Single(vals);
            Assert.Equal(5, vals[0]);
        }

        [Fact]
        public void DapperRow_IReadOnlyCollection_Count()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } });
            IReadOnlyCollection<KeyValuePair<string, object?>> roc = (IReadOnlyCollection<KeyValuePair<string, object?>>)(object)row;
            Assert.Equal(2, roc.Count);
        }

        // ── DeadValue path — after Remove, TryGetValue returns false ──

        [Fact]
        public void DapperRow_AfterRemove_TryGetValue_ReturnsFalse()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
            IDictionary<string, object?> dict = row;
            dict.Remove("Name");

            // TryGetValue on a removed (DeadValue) key should return false (lines 55-57)
            Assert.False(dict.TryGetValue("Name", out _));
        }

        [Fact]
        public void DapperRow_AfterRemove_ContainsKey_False()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
            IDictionary<string, object?> dict = row;
            dict.Remove("Name");
            Assert.False(dict.ContainsKey("Name"));
        }

        // ── ToString with null value ──────────────────────────────────
        // fakeDb GetFieldType throws on true null; use Remove to create a null slot

        [Fact]
        public void DapperRow_ToString_WithNullValue_ShowsNull()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
            IDictionary<string, object?> dict = row;
            // Set Name to null then check ToString shows NULL
            dict["Name"] = null;
            string str = ((object)dict).ToString()!;
            Assert.Contains("NULL", str);
        }

        // ── ICollection<KVP>.Add ──────────────────────────────────────

        [Fact]
        public void DapperRow_ICollection_Add_KVP_Works()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 } });
            ICollection<KeyValuePair<string, object?>> col = (IDictionary<string, object?>)row;
            col.Add(new KeyValuePair<string, object?>("NewKey", 99));
            IDictionary<string, object?> dict = row;
            Assert.Equal(99, dict["NewKey"]);
        }

        // ── IDictionary.Add(key, value) ───────────────────────────────

        [Fact]
        public void DapperRow_IDictionary_Add_NewKey_Works()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 } });
            IDictionary<string, object?> dict = row;
            dict.Add("Extra", "value");
            Assert.Equal("value", dict["Extra"]);
        }

        [Fact]
        public void DapperRow_IDictionary_Add_DuplicateKey_Throws()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 } });
            IDictionary<string, object?> dict = row;
            Assert.Throws<System.ArgumentException>(() => dict.Add("Id", 999));
        }

        // ── IReadOnlyDictionary Count with removed entry ──────────────

        [Fact]
        public void DapperRow_IReadOnlyCollection_Count_AfterRemove()
        {
            var row = GetRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } });
            IDictionary<string, object?> dict = row;
            dict.Remove("Name");
            IReadOnlyCollection<KeyValuePair<string, object?>> roc = (IReadOnlyCollection<KeyValuePair<string, object?>>)dict;
            Assert.Equal(1, roc.Count); // only Id remains
        }
    }
}
#endif
