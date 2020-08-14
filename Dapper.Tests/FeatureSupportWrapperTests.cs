using Npgsql;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using Xunit;

namespace Dapper.Tests
{
    public class FeatureSupportWrapperTests
    {
        [Fact]
        public void GetDbConnectionType_ByDelegate()
        {
            FeatureSupportWrapper.GetDbConnectionType = (conn) =>
            {
                if (conn is ProfiledDbConnection)
                {
                    return ((ProfiledDbConnection)conn).WrappedConnection.GetType();
                }
                return conn?.GetType();
            };

            using (var conn = new ProfiledDbConnection(new NpgsqlConnection(), MiniProfiler.Current))
            {
                Assert.Equal("npgsqlconnection", FeatureSupportWrapper.GetDbConnectionType(conn).Name.ToLower());
            }
        }
    }
}