using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests.Providers
{
    public class FirebirdTests : TestBase
    {
        [Fact(Skip = "Bug in Firebird; a PR to fix it has been submitted")]
        public void Issue178_Firebird()
        {
            const string cs = @"initial catalog=localhost:database;user id=SYSDBA;password=masterkey";

            using (var connection = new FbConnection(cs))
            {
                connection.Open();
                const string sql = @"select count(*) from Issue178";
                try { connection.Execute("drop table Issue178"); }
                catch { /* don't care */ }
                connection.Execute("create table Issue178(id int not null)");
                connection.Execute("insert into Issue178(id) values(42)");
                // raw ADO.net
                using (var sqlCmd = new FbCommand(sql, connection))
                using (IDataReader reader1 = sqlCmd.ExecuteReader())
                {
                    Assert.IsTrue(reader1.Read());
                    reader1.GetInt32(0).IsEqualTo(1);
                    Assert.IsFalse(reader1.Read());
                    Assert.IsFalse(reader1.NextResult());
                }

                // dapper
                using (var reader2 = connection.ExecuteReader(sql))
                {
                    Assert.IsTrue(reader2.Read());
                    reader2.GetInt32(0).IsEqualTo(1);
                    Assert.IsFalse(reader2.Read());
                    Assert.IsFalse(reader2.NextResult());
                }

                var count = connection.Query<int>(sql).Single();
                count.IsEqualTo(1);
            }
        }
    }
}