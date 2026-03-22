#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbOutputParamTests
    {
        // ── ParameterDirection tests ──────────────────────────────────
        // fakeDb does not write actual output values back to command params,
        // so we test the DynamicParameters wiring rather than value round-trips.

        [Fact]
        public void DynamicParameters_OutputParam_IsAddedToParameterNames()
        {
            var dp = new DynamicParameters();
            dp.Add("@newId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            // DynamicParameters.Clean() strips @ prefix internally
            Assert.Contains("newId", dp.ParameterNames);
        }

        [Fact]
        public void DynamicParameters_ReturnValueParam_IsAddedToParameterNames()
        {
            var dp = new DynamicParameters();
            dp.Add("@ret", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            Assert.Contains("ret", dp.ParameterNames);
        }

        [Fact]
        public void DynamicParameters_InputOutputParam_IsAddedToParameterNames()
        {
            var dp = new DynamicParameters();
            dp.Add("@count", value: 0, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);

            Assert.Contains("count", dp.ParameterNames);
        }

        [Fact]
        public void DynamicParameters_Get_ReturnsNull_ForOutputParamWithDBNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            var dp = new DynamicParameters();
            dp.Add("@newId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            conn.Execute("EXEC CreateUser @newId OUTPUT", dp);

            // fakeDb doesn't set output values; Get<int?> returns null for DBNull
            var val = dp.Get<int?>("@newId");
            Assert.Null(val);
        }

        [Fact]
        public async Task ExecuteAsync_WithOutputParam_CommandExecutes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var dp = new DynamicParameters();
            dp.Add("@result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            // Just ensure execution doesn't throw
            await conn.ExecuteAsync("EXEC GetCount @result OUTPUT", dp);
        }

        [Fact]
        public void DynamicParameters_MultipleOutputParams_AllInParameterNames()
        {
            var dp = new DynamicParameters();
            dp.Add("@id",   dbType: DbType.Int32,  direction: ParameterDirection.Output);
            dp.Add("@name", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);

            var names = dp.ParameterNames.ToList();
            // DynamicParameters.Clean() strips @ prefix internally
            Assert.Contains("id",   names);
            Assert.Contains("name", names);
        }

        // ── Output<T> expression-based binding ────────────────────────
        // Tests that the expression wiring is set up without throwing.

        private class UserResult { public int? NewId { get; set; } }

        [Fact]
        public void DynamicParameters_Output_Expression_DoesNotThrow()
        {
            var target = new UserResult();
            var dp = new DynamicParameters();

            // Just verify Output<T> sets up the binding without throwing
            dp.Output(target, x => x.NewId);
        }

        [Fact]
        public void DynamicParameters_Output_Expression_ExecutesWithoutError()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var target = new UserResult();
            var dp = new DynamicParameters();
            dp.Output(target, x => x.NewId);
            dp.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            // Execute - fakeDb returns DBNull for output params, and int? accepts null
            conn.Execute("EXEC CreateUser @NewId OUTPUT", dp);
        }

        [Fact]
        public void DynamicParameters_Output_InvalidExpression_Throws()
        {
            var target = new UserResult();
            var dp = new DynamicParameters();

            // Non-member expression should throw
            Assert.ThrowsAny<Exception>(() =>
                dp.Output(target, x => (object?)(x.NewId == null ? 1 : 2)));
        }
    }
}
#endif
