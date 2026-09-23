#if !NET481
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests that exercise DataTableHandler by passing DataTable as anonymous object parameter
    /// (not via DynamicParameters.Add which bypasses the type handler).
    /// </summary>
    public class FakeDbDataTableParameterTests
    {
        // ── DataTable as anonymous object parameter ───────────────────
        // This triggers DataTableHandler.SetValue via the type handler lookup.

        [Fact]
        public void Execute_DataTable_AsAnonymousParam_CallsTypeHandler()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);
            dt.Rows.Add(2);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(2);
            conn.Open();

            // Pass DataTable as an anonymous object property — triggers DataTableHandler.SetValue
            conn.Execute("EXEC BulkInsert @ids", new { ids = dt });
        }

        [Fact]
        public void Query_DataTable_AsAnonymousParam_Works()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            // Triggers DataTableHandler.SetValue when expanding parameters
            var result = conn.Query<int>(
                "SELECT Id FROM T WHERE Id IN @ids", new { ids = dt }).ToList();

            Assert.Single(result);
        }

        // ── AsTableValuedParameter ────────────────────────────────────

        [Fact]
        public void DataTable_AsTableValuedParameter_Constructor()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);

            // Exercise the TableValuedParameter constructor paths
            var tvp = dt.AsTableValuedParameter();
            Assert.NotNull(tvp);
        }

        [Fact]
        public void DataTable_AsTableValuedParameter_WithTypeName()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));

            var tvp = dt.AsTableValuedParameter("dbo.IdList");
            Assert.NotNull(tvp);
        }

        // ── DataTable.SetTypeName / GetTypeName ───────────────────────

        [Fact]
        public void DataTable_SetTypeName_GetTypeName_Works()
        {
            var dt = new DataTable();
            dt.SetTypeName("dbo.MyType");
            Assert.Equal("dbo.MyType", dt.GetTypeName());
        }

        [Fact]
        public void DataTable_SetTypeName_Null_ClearsTypeName()
        {
            var dt = new DataTable();
            dt.SetTypeName("dbo.MyType");
            dt.SetTypeName(null!);
            Assert.Null(dt.GetTypeName());
        }
    }
}
#endif
