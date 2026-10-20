#if !NET481
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Advanced DapperRow/DapperRowMetaObject tests: dynamic set, delete via IDictionary,
    /// TypeDescriptor integration, and DapperTable sharing.
    /// </summary>
    public class FakeDbDynamicRowAdvancedTests
    {
        // ── Dynamic SET member (BindSetMember on DapperRowMetaObject) ──

        [Fact]
        public void DynamicRow_DynamicSet_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            dynamic row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();

            // Trigger BindSetMember on DapperRowMetaObject
            row.Name = "Updated";
            Assert.Equal("Updated", (string)row.Name);
        }

        [Fact]
        public void DynamicRow_DynamicSet_NewMember_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            dynamic row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            row.NewField = "added";
            Assert.Equal("added", (string)row.NewField);
        }

        // ── DapperTable operations ─────────────────────────────────────

        [Fact]
        public void DapperRow_IDictionary_Clear_RemovesAllEntries()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 }, { "C", 3 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT A, B, C FROM T").Single();
            var dict = (IDictionary<string, object>)row;

            dict.Clear();
            Assert.Empty(dict);
        }

        [Fact]
        public void DapperRow_IDictionary_Contains_KeyValuePair()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 42 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            var coll = (ICollection<KeyValuePair<string, object>>)row;

            Assert.True(coll.Contains(new KeyValuePair<string, object>("Id", 42)));
            Assert.False(coll.Contains(new KeyValuePair<string, object>("Id", 99)));
        }

        [Fact]
        public void DapperRow_IDictionary_Remove_KeyValuePair_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id, Name FROM T").Single();
            var coll = (ICollection<KeyValuePair<string, object>>)row;

            bool removed = coll.Remove(new KeyValuePair<string, object>("Name", "Alice"));
            Assert.True(removed);
            Assert.False(((IDictionary<string, object>)row).ContainsKey("Name"));
        }

        // ── DapperRow.ToString() ───────────────────────────────────────

        [Fact]
        public void DapperRow_ToString_DoesNotThrow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var row = (IDictionary<string, object>)conn.Query<dynamic>("SELECT Id FROM T").Single();
            // Calling ToString via IDictionary cast (not dynamic dispatch)
            var str = row.ToString();
            // DapperRow.ToString() is not overridden, so returns type name
        }

        // ── DapperRow as IEnumerable (non-generic) ────────────────────

        [Fact]
        public void DapperRow_NonGenericEnumerable_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT A, B FROM T").Single();
            var count = 0;
            foreach (KeyValuePair<string, object> pair in (System.Collections.IEnumerable)row)
            {
                count++;
            }
            Assert.Equal(2, count);
        }

        // ── DapperRow equality/comparisons ────────────────────────────

        [Fact]
        public void DapperRow_GetHashCode_DoesNotThrow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var row = conn.Query<dynamic>("SELECT Id FROM T").Single();
            var _ = row.GetHashCode();
        }
    }
}
#endif
