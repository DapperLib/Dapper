using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Dapper.Tests
{
    public class IParameterCallbacksTests
    {
        class TestParams : SqlMapper.IDynamicParameters, SqlMapper.IParameterCallbacks
        {
            public bool Completed { get; private set; }

            public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
            {
                // nothing needed here for this test
            }

            public void OnCompleted()
            {
                Completed = true;
            }
        }

        [Fact]
        public void QueryMultiple_Calls_OnCompleted()
        {
            using var connection = GetOpenConnection();
            var p = new TestParams();

            using (var grid = connection.QueryMultiple("select 1; select 2;", p))
            {
                var _ = grid.Read<int>();
                var __ = grid.Read<int>();
            }

            Assert.True(p.Completed);
        }

        [Fact]
        public async Task QueryMultipleAsync_Calls_OnCompleted()
        {
            using var connection = GetOpenConnection();
            var p = new TestParams();

            using (var grid = await connection.QueryMultipleAsync("select 1; select 2;", p))
            {
                var _ = await grid.ReadAsync<int>();
                var __ = await grid.ReadAsync<int>();
            }

            Assert.True(p.Completed);
        }

        private static IDbConnection GetOpenConnection()
        {
            // Please note that CI usually has a test DB so please adjust connection string as required
            var cs = "Server=localhost,1433;User Id=sa;Password=Str0ngPassw0rd!;TrustServerCertificate=true;";
            var connection = new SqlConnection(cs);
            connection.Open();
            return connection;
        }
    }
}
