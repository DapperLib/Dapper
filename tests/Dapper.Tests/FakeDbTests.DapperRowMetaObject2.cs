#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DapperRowMetaObject coverage: lines 26-28 (constructor with value),
    /// lines 93-96 (BindInvokeMember).
    /// </summary>
    public class FakeDbDapperRowMetaObject2Tests
    {
        private static dynamic GetRow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();
            return conn.QueryFirst("SELECT * FROM T");
        }

        // ── BindGetMember on a missing property → null ─────────────────
        // DapperRowMetaObject.BindGetMember (already covered)

        [Fact]
        public void DapperRow_Dynamic_GetMissing_ReturnsNull()
        {
            dynamic row = GetRow();
            var val = row.NonExistentProp;
            Assert.Null(val);
        }

        // ── BindSetMember sets value ──────────────────────────────────

        [Fact]
        public void DapperRow_Dynamic_SetMember_Works()
        {
            dynamic row = GetRow();
            row.Name = "Updated";
            Assert.Equal("Updated", (string)row.Name);
        }

        // ── BindInvokeMember — calling a method on the dynamic row ─────
        // BindInvokeMember redirects to GetValue(binder.Name), so calling any method
        // returns GetValue of that method name (null if not a key).

        [Fact]
        public void DapperRow_Dynamic_InvokeMember_ReturnsGetValue()
        {
            dynamic row = GetRow();
            // Calling any method via dynamic dispatch hits BindInvokeMember,
            // which calls GetValue("SomeMethod") -> returns null (no such key)
            var result = row.NonExistentMethod();
            Assert.Null(result);
        }

        // ── GetDynamicMemberNames (lines 92-96) ───────────────────────

        [Fact]
        public void DapperRow_GetDynamicMemberNames_ReturnsMemberNames()
        {
            dynamic row = GetRow();
            var provider = (System.Dynamic.IDynamicMetaObjectProvider)row;
            var expr = System.Linq.Expressions.Expression.Constant((object)row);
            var meta = provider.GetMetaObject(expr);
            var names = meta.GetDynamicMemberNames().ToList();
            Assert.Contains("Id", names);
            Assert.Contains("Name", names);
        }
    }
}
#endif
