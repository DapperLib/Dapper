using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public sealed class MySqlProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => MySql.Data.MySqlClient.MySqlClientFactory.Instance;
        public override string GetConnectionString() => IsAppVeyor
                ? "Server=localhost;Database=test;Uid=root;Pwd=Password12!;"
                : "Server=localhost;Database=tests;Uid=test;Pwd=pass;";

        public DbConnection GetMySqlConnection(bool open = true,
            bool convertZeroDatetime = false, bool allowZeroDatetime = false)
        {
            string cs = GetConnectionString();
            var csb = Factory.CreateConnectionStringBuilder();
            csb.ConnectionString = cs;
            ((dynamic)csb).AllowZeroDateTime = allowZeroDatetime;
            ((dynamic)csb).ConvertZeroDateTime = convertZeroDatetime;
            var conn = Factory.CreateConnection();
            conn.ConnectionString = csb.ConnectionString;
            if (open) conn.Open();
            return conn;
        }
    }
    public class MySQLTests : TestBase<MySqlProvider>
    {      
        [FactMySql]
        public void DapperEnumValue_Mysql()
        {
            using (var conn = Provider.GetMySqlConnection())
            {
                Common.DapperEnumValue(conn);
            }
        }

        [FactMySql(Skip = "See https://github.com/StackExchange/Dapper/issues/552, not resolved on the MySQL end.")]
        public void Issue552_SignedUnsignedBooleans()
        {
            using (var conn = Provider.GetMySqlConnection(true, false, false))
            {
                conn.Execute(@"
CREATE TEMPORARY TABLE IF NOT EXISTS `bar` (
  `id` INT NOT NULL,
  `bool_val` BOOL NULL,
  PRIMARY KEY (`id`));
  
  truncate table bar;
  insert bar (id, bool_val) values (1, null);
  insert bar (id, bool_val) values (2, 0);
  insert bar (id, bool_val) values (3, 1);
  insert bar (id, bool_val) values (4, null);
  insert bar (id, bool_val) values (5, 1);
  insert bar (id, bool_val) values (6, 0);
  insert bar (id, bool_val) values (7, null);
  insert bar (id, bool_val) values (8, 1);");

                var rows = conn.Query<MySqlHasBool>("select * from bar;").ToDictionary(x => x.Id);

                Assert.Null(rows[1].Bool_Val);
                Assert.False(rows[2].Bool_Val);
                Assert.True(rows[3].Bool_Val);
                Assert.Null(rows[4].Bool_Val);
                Assert.True(rows[5].Bool_Val);
                Assert.False(rows[6].Bool_Val);
                Assert.Null(rows[7].Bool_Val);
                Assert.True(rows[8].Bool_Val);
            }
        }

        private class MySqlHasBool
        {
            public int Id { get; set; }
            public bool? Bool_Val { get; set; }
        }

        [FactMySql]
        public void Issue295_NullableDateTime_MySql_Default()
        {
            using (var conn = Provider.GetMySqlConnection(true, false, false))
            {
                Common.TestDateTime(conn);
            }
        }

        [FactMySql]
        public void Issue295_NullableDateTime_MySql_ConvertZeroDatetime()
        {
            using (var conn = Provider.GetMySqlConnection(true, true, false))
            {
                Common.TestDateTime(conn);
            }
        }

        [FactMySql(Skip = "See https://github.com/StackExchange/Dapper/issues/295, AllowZeroDateTime=True is not supported")]
        public void Issue295_NullableDateTime_MySql_AllowZeroDatetime()
        {
            using (var conn = Provider.GetMySqlConnection(true, false, true))
            {
                Common.TestDateTime(conn);
            }
        }

        [FactMySql(Skip = "See https://github.com/StackExchange/Dapper/issues/295, AllowZeroDateTime=True is not supported")]
        public void Issue295_NullableDateTime_MySql_ConvertAllowZeroDatetime()
        {
            using (var conn = Provider.GetMySqlConnection(true, true, true))
            {
                Common.TestDateTime(conn);
            }
        }

        [FactMySql]
        public void Issue426_SO34439033_DateTimeGainsTicks()
        {
            using (var conn = Provider.GetMySqlConnection(true, true, true))
            {
                try { conn.Execute("drop table Issue426_Test"); } catch { /* don't care */ }
                try { conn.Execute("create table Issue426_Test (Id int not null, Time time not null)"); } catch { /* don't care */ }
                const long ticks = 553440000000;
                const int Id = 426;

                var localObj = new Issue426_Test
                {
                    Id = Id,
                    Time = TimeSpan.FromTicks(ticks) // from code example
                };
                conn.Execute("replace into Issue426_Test values (@Id,@Time)", localObj);

                var dbObj = conn.Query<Issue426_Test>("select * from Issue426_Test where Id = @id", new { id = Id }).Single();
                Assert.Equal(Id, dbObj.Id);
                Assert.Equal(ticks, dbObj.Time.Value.Ticks);
            }
        }

        [FactMySql]
        public void SO36303462_Tinyint_Bools()
        {
            using (var conn = Provider.GetMySqlConnection(true, true, true))
            {
                try { conn.Execute("drop table SO36303462_Test"); } catch { /* don't care */ }
                conn.Execute("create table SO36303462_Test (Id int not null, IsBold tinyint not null);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (1,1);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (2,0);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (3,1);");

                var rows = conn.Query<SO36303462>("select * from SO36303462_Test").ToDictionary(x => x.Id);
                Assert.Equal(3, rows.Count);
                Assert.True(rows[1].IsBold);
                Assert.False(rows[2].IsBold);
                Assert.True(rows[3].IsBold);
            }
        }

        [FactMySql]
        public void Issue1277_ReaderSync()
        {
            using (var conn = Provider.GetMySqlConnection())
            {
                try { conn.Execute("drop table Issue1277_Test"); } catch { /* don't care */ }
                conn.Execute("create table Issue1277_Test (Id int not null, IsBold tinyint not null);");
                conn.Execute("insert Issue1277_Test (Id, IsBold) values (1,1);");
                conn.Execute("insert Issue1277_Test (Id, IsBold) values (2,0);");
                conn.Execute("insert Issue1277_Test (Id, IsBold) values (3,1);");

                using (var reader = conn.ExecuteReader(
                    "select * from Issue1277_Test where Id < @id",
                    new { id = 42 }))
                {
                    var table = new DataTable();
                    table.Load(reader);
                    Assert.Equal(2, table.Columns.Count);
                    Assert.Equal(3, table.Rows.Count);
                }
            }
        }

        [FactMySql]
        public async Task Issue1277_ReaderAsync()
        {
            using (var conn = Provider.GetMySqlConnection())
            {
                try { await conn.ExecuteAsync("drop table Issue1277_Test"); } catch { /* don't care */ }
                await conn.ExecuteAsync("create table Issue1277_Test (Id int not null, IsBold tinyint not null);");
                await conn.ExecuteAsync("insert Issue1277_Test (Id, IsBold) values (1,1);");
                await conn.ExecuteAsync("insert Issue1277_Test (Id, IsBold) values (2,0);");
                await conn.ExecuteAsync("insert Issue1277_Test (Id, IsBold) values (3,1);");

                using (var reader = await conn.ExecuteReaderAsync(
                    "select * from Issue1277_Test where Id < @id",
                    new { id = 42 }))
                {
                    var table = new DataTable();
                    table.Load(reader);
                    Assert.Equal(2, table.Columns.Count);
                    Assert.Equal(3, table.Rows.Count);
                }
            }
        }

        private class SO36303462
        {
            public int Id { get; set; }
            public bool IsBold { get; set; }
        }

        public class Issue426_Test
        {
            public long Id { get; set; }
            public TimeSpan? Time { get; set; }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactMySqlAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactMySqlAttribute()
            {
                try
                {
                    using (DatabaseProvider<MySqlProvider>.Instance.GetMySqlConnection(true)) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"MySql is unavailable: {ex.Message}";
                }
            }
        }
    }
}
