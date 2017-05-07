#if MYSQL
using System;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class MySQLTests : TestBase
    {
        private static MySql.Data.MySqlClient.MySqlConnection GetMySqlConnection(bool open = true,
            bool convertZeroDatetime = false, bool allowZeroDatetime = false)
        {
            string cs = IsAppVeyor
                ? "Server=localhost;Database=test;Uid=root;Pwd=Password12!;"
                : "Server=localhost;Database=tests;Uid=test;Pwd=pass;";
            var csb = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(cs);
            csb.AllowZeroDateTime = allowZeroDatetime;
            csb.ConvertZeroDateTime = convertZeroDatetime;
            var conn = new MySql.Data.MySqlClient.MySqlConnection(csb.ConnectionString);
            if (open) conn.Open();
            return conn;
        }

        [FactMySql]
        public void DapperEnumValue_Mysql()
        {
            using (var connection = GetMySqlConnection())
            {
                Common.DapperEnumValue(connection);
            }
        }

        [FactMySql(Skip = "See https://github.com/StackExchange/Dapper/issues/552, not resolved on the MySQL end.")]
        public void Issue552_SignedUnsignedBooleans()
        {
            using (var conn = GetMySqlConnection(true, false, false))
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

                rows[1].Bool_Val.IsNull();
                rows[2].Bool_Val.IsEqualTo(false);
                rows[3].Bool_Val.IsEqualTo(true);
                rows[4].Bool_Val.IsNull();
                rows[5].Bool_Val.IsEqualTo(true);
                rows[6].Bool_Val.IsEqualTo(false);
                rows[7].Bool_Val.IsNull();
                rows[8].Bool_Val.IsEqualTo(true);
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
            using (var conn = GetMySqlConnection(true, false, false))
            {
                Common.TestDateTime(connection);
            }
        }

        [FactMySql]
        public void Issue295_NullableDateTime_MySql_ConvertZeroDatetime()
        {
            using (var conn = GetMySqlConnection(true, true, false))
            {
                Common.TestDateTime(connection);
            }
        }

        [FactMySql]
        public void Issue295_NullableDateTime_MySql_AllowZeroDatetime()
        {
            using (var conn = GetMySqlConnection(true, false, true))
            {
                Common.TestDateTime(connection);
            }
        }

        [FactMySql]
        public void Issue295_NullableDateTime_MySql_ConvertAllowZeroDatetime()
        {
            using (var conn = GetMySqlConnection(true, true, true))
            {
                Common.TestDateTime(connection);
            }
        }

        [FactMySql]
        public void Issue426_SO34439033_DateTimeGainsTicks()
        {
            using (var conn = GetMySqlConnection(true, true, true))
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
                dbObj.Id.IsEqualTo(Id);
                dbObj.Time.Value.Ticks.IsEqualTo(ticks);
            }
        }

        [FactMySql]
        public void SO36303462_Tinyint_Bools()
        {
            using (var conn = GetMySqlConnection(true, true, true))
            {
                try { conn.Execute("drop table SO36303462_Test"); } catch { /* don't care */ }
                conn.Execute("create table SO36303462_Test (Id int not null, IsBold tinyint not null);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (1,1);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (2,0);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (3,1);");

                var rows = conn.Query<SO36303462>("select * from SO36303462_Test").ToDictionary(x => x.Id);
                rows.Count.IsEqualTo(3);
                rows[1].IsBold.IsTrue();
                rows[2].IsBold.IsFalse();
                rows[3].IsBold.IsTrue();
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
                    using (GetMySqlConnection(true)) { }
                }
                catch (Exception ex)
                {
                    unavailable = $"MySql is unavailable: {ex.Message}";
                }
            }
        }
    }
}
#endif