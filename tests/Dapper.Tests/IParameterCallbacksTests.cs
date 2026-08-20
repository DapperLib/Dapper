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
                // no params needed
            }

            public void OnCompleted() => Completed = true;
        }

        [Fact]
        public void QueryMultiple_Calls_OnCompleted()
        {
            if (!TryOpenConnection(out var conn)) return;
            using var connection = conn!;
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
            if (!TryOpenConnection(out var conn)) return;
            using var connection = conn!;
            var p = new TestParams();

            using (var grid = await connection.QueryMultipleAsync("select 1; select 2;", p))
            {
                var _ = await grid.ReadAsync<int>();
                var __ = await grid.ReadAsync<int>();
            }

            Assert.True(p.Completed);
        }

        private static bool TryOpenConnection(out IDbConnection? connection)
        {
            connection = null;
            try
            {
                var cs = "Server=localhost,1433;User Id=sa;Password=Str0ngPassw0rd!;TrustServerCertificate=true;";
                var c = new SqlConnection(cs);
                c.Open();
                connection = c;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
