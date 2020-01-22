using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Dapper.Tests.Providers
{
    public class FirebirdProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => FirebirdClientFactory.Instance;
        public override string GetConnectionString() => "initial catalog=localhost:database;user id=SYSDBA;password=masterkey";
    }
    public class FirebirdTests : TestBase<FirebirdProvider>
    {
        private FbConnection GetOpenFirebirdConnection() => (FbConnection)Provider.GetOpenConnection();

        [Fact(Skip = "Bug in Firebird; a PR to fix it has been submitted")]
        public void Issue178_Firebird()
        {
            using (var connection = GetOpenFirebirdConnection())
            {
                const string sql = "select count(*) from Issue178";
                try { connection.Execute("drop table Issue178"); }
                catch { /* don't care */ }
                connection.Execute("create table Issue178(id int not null)");
                connection.Execute("insert into Issue178(id) values(42)");
                // raw ADO.net
                using (var sqlCmd = new FbCommand(sql, connection))
                using (IDataReader reader1 = sqlCmd.ExecuteReader())
                {
                    Assert.True(reader1.Read());
                    Assert.Equal(1, reader1.GetInt32(0));
                    Assert.False(reader1.Read());
                    Assert.False(reader1.NextResult());
                }

                // dapper
                using (var reader2 = connection.ExecuteReader(sql))
                {
                    Assert.True(reader2.Read());
                    Assert.Equal(1, reader2.GetInt32(0));
                    Assert.False(reader2.Read());
                    Assert.False(reader2.NextResult());
                }

                var count = connection.Query<int>(sql).Single();
                Assert.Equal(1, count);
            }
        }
    }
}
