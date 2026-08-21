#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DynamicParameters coverage: sub-DynamicParameters with templates,
    /// PackListParameters (EnumerableMultiParameter), Get&lt;T&gt; with DBNull,
    /// parameter reuse, TypeHandler path, and Output callback basics.
    /// </summary>
    public class FakeDbDynamicParamsAdvancedTests
    {
        // ── Sub-DynamicParameters with templates ──────────────────────
        // Lines 63-69: when the sub-DP itself has templates

        [Fact]
        public void DynamicParameters_AddFromDynamicParameters_WithTemplate_Works()
        {
            // Create a DynamicParameters that uses an anonymous object template
            var inner = new DynamicParameters(new { Name = "Alice" });

            // Add it to another DynamicParameters (triggers lines 63-69 path)
            var outer = new DynamicParameters();
            outer.AddDynamicParams(inner);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            // If inner had templates, they should be copied to outer's templates
            conn.Execute("UPDATE T SET Name = @Name", outer);
        }

        // ── IEnumerable<KeyValuePair<string, object>> template ─────────

        [Fact]
        public void DynamicParameters_AddFromDictionary_Works()
        {
            var dict = new Dictionary<string, object> { { "Id", 1 }, { "Name", "Bob" } };
            var dp = new DynamicParameters();
            dp.AddDynamicParams(dict);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("UPDATE T SET Name = @Name WHERE Id = @Id", dp);
        }

        // ── PackListParameters (EnumerableMultiParameter) ─────────────
        // Lines 246-250: adding a list parameter triggers PackListParameters

        [Fact]
        public void DynamicParameters_ListParam_ExpandsInClause()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } },
                new Dictionary<string, object?> { { "Id", 2 } },
            });
            conn.Open();

            var ids = new[] { 1, 2, 3 };
            // When a list is passed as param it becomes EnumerableMultiParameter
            var results = conn.Query<int>("SELECT Id FROM T WHERE Id IN @ids", new { ids }).ToList();
            Assert.Equal(2, results.Count);
        }

        // ── Get<T> with DBNull — non-nullable type throws ─────────────
        // Lines 324-325

        [Fact]
        public void DynamicParameters_Get_DBNull_NonNullable_Throws()
        {
            var dp = new DynamicParameters();
            dp.Add("val", DBNull.Value, DbType.Int32, ParameterDirection.Input);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("SELECT @val", dp);

            // Get<int> where the attached param's value is DBNull throws ApplicationException
            Assert.Throws<ApplicationException>(() => dp.Get<int>("val"));
        }

        // ── Get<T> with DBNull — nullable type returns default ─────────

        [Fact]
        public void DynamicParameters_Get_DBNull_Nullable_ReturnsDefault()
        {
            var dp = new DynamicParameters();
            dp.Add("val", DBNull.Value, DbType.Int32, ParameterDirection.Input);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("SELECT @val", dp);

            var result = dp.Get<int?>("val");
            Assert.Null(result);
        }

        // ── TypeHandler path in AddParameters ─────────────────────────
        // Lines 285-290: when a custom TypeHandler is registered

        private class MyGuid
        {
            public Guid Value { get; }
            public MyGuid(Guid value) { Value = value; }
        }

        private class MyGuidHandler : SqlMapper.TypeHandler<MyGuid>
        {
            public override void SetValue(IDbDataParameter parameter, MyGuid value)
            {
                parameter.DbType = DbType.Guid;
                parameter.Value = value.Value;
            }
            public override MyGuid Parse(object value) => new MyGuid((Guid)value);
        }

        [Fact]
        public void DynamicParameters_TypeHandler_SetValue_Path()
        {
            SqlMapper.AddTypeHandler(new MyGuidHandler());
            try
            {
                var guid = new MyGuid(Guid.NewGuid());
                var dp = new DynamicParameters();
                dp.Add("g", guid, DbType.Guid);

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("SELECT @g", dp);
            }
            finally
            {
                SqlMapper.RemoveTypeMap(typeof(MyGuid));
            }
        }

        // ── Parameter already in command (reuse path) ─────────────────
        // Lines 261-263: when template adds param to command, then explicit param
        // with same name is processed — command.Parameters.Contains returns true

        [Fact]
        public void DynamicParameters_TemplateAndExplicit_SameName_ReusesParam()
        {
            // Template adds "Name" to command, then explicit "Name" param reuses it
            var dp = new DynamicParameters(new { Name = "FromTemplate" });
            dp.Add("Name", "Explicit"); // explicit param with same name as template property

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("UPDATE T SET Name = @Name", dp);
        }

        // ── ShouldSetDbType non-nullable overload ─────────────────────
        // Line 162

        [Fact]
        public void DynamicParameters_WithExplicitDbType_Works()
        {
            var dp = new DynamicParameters();
            dp.Add("n", 42, DbType.Int32);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("SELECT @n", dp);
        }
    }
}
#endif
