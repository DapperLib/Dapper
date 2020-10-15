using System;
using System.Data.Common;
using Dapper.ProviderTools;
using Xunit;

namespace Dapper.Tests
{
    public class ProviderTests
    {
        [Fact]
        public void BulkCopy_SystemDataSqlClient()
        {
            using (var conn = new System.Data.SqlClient.SqlConnection())
            {
                Test<System.Data.SqlClient.SqlBulkCopy>(conn);
            }
        }

        [Fact]
        public void BulkCopy_MicrosoftDataSqlClient()
        {
            using (var conn = new Microsoft.Data.SqlClient.SqlConnection())
            {
                Test<Microsoft.Data.SqlClient.SqlBulkCopy>(conn);
            }
        }

        [Fact]
        public void ClientId_SystemDataSqlClient()
            => TestClientId<SystemSqlClientProvider>();

        [Fact]
        public void ClearPool_SystemDataSqlClient()
            => ClearPool<SystemSqlClientProvider>();

        [Fact]
        public void ClearAllPools_SystemDataSqlClient()
            => ClearAllPools<SystemSqlClientProvider>();

#if MSSQLCLIENT
        [Fact]
        public void ClientId_MicrosoftDataSqlClient()
            => TestClientId<MicrosoftSqlClientProvider>();

        [Fact]
        public void ClearPool_MicrosoftDataSqlClient()
            => ClearPool<MicrosoftSqlClientProvider>();

        [Fact]
        public void ClearAllPools_MicrosoftDataSqlClient()
            => ClearAllPools<MicrosoftSqlClientProvider>();
#endif

        private static void TestClientId<T>()
             where T : SqlServerDatabaseProvider, new()
        {
            var provider = new T();
            using (var conn = provider.GetOpenConnection())
            {
                Assert.True(conn.TryGetClientConnectionId(out var id));
                Assert.NotEqual(Guid.Empty, id);
            }
        }

        private static void ClearPool<T>()
     where T : SqlServerDatabaseProvider, new()
        {
            var provider = new T();
            using (var conn = provider.GetOpenConnection())
            {
                Assert.True(conn.TryClearPool());
            }
        }

        private static void ClearAllPools<T>()
     where T : SqlServerDatabaseProvider, new()
        {
            var provider = new T();
            using (var conn = provider.GetOpenConnection())
            {
                Assert.True(conn.TryClearAllPools());
            }
        }

        private static void Test<T>(DbConnection connection)
        {
            using (var bcp = BulkCopy.TryCreate(connection))
            {
                Assert.NotNull(bcp);
                Assert.IsType<T>(bcp.Wrapped);
                bcp.EnableStreaming = true;
            }
        }

        [Theory]
        [InlineData(51000, 51000, true)]
        [InlineData(51000, 43, false)]
        public void DbNumber_SystemData(int create, int test, bool result)
            => Test<SystemSqlClientProvider>(create, test, result);

#if MSSQLCLIENT
        [Theory]
        [InlineData(51000, 51000, true)]
        [InlineData(51000, 43, false)]
        public void DbNumber_MicrosoftData(int create, int test, bool result)
            => Test<MicrosoftSqlClientProvider>(create, test, result);
#endif

        private void Test<T>(int create, int test, bool result)
            where T : SqlServerDatabaseProvider, new()
        {
            var provider = new T();
            using (var conn = provider.GetOpenConnection())
            {
                try
                {
                    conn.Execute("throw @create, 'boom', 1;", new { create });
                    Assert.False(true);
                }
                catch(DbException err)
                {
                    Assert.Equal(result, err.IsNumber(test));
                }
            }
        }
    }
}
