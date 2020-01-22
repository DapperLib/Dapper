#if OLEDB
using System;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class OLEDBProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => OleDbFactory.Instance;
        public override string GetConnectionString() =>
            IsAppVeyor
                ? @"Provider=SQLOLEDB;Data Source=(local)\SQL2016;Initial Catalog=tempdb;User Id=sa;Password=Password12!"
                : "Provider=SQLOLEDB;Data Source=.;Initial Catalog=tempdb;Integrated Security=SSPI";
    }

    public class OLDEBTests : TestBase<OLEDBProvider>
    {
        public OleDbConnection GetOleDbConnection() => (OleDbConnection) Provider.GetOpenConnection();

        // see https://stackoverflow.com/q/18847510/23354
        [Fact]
        public void TestOleDbParameters()
        {
            using (var conn = GetOleDbConnection())
            {
                var row = conn.Query("select Id = ?, Age = ?",
                    new { foo = 12, bar = 23 } // these names DO NOT MATTER!!!
                ).Single();
                int age = row.Age;
                int id = row.Id;
                Assert.Equal(23, age);
                Assert.Equal(12, id);
            }
        }

        [Fact]
        public void PseudoPositionalParameters_Simple()
        {
            using (var connection = GetOleDbConnection())
            {
                int value = connection.Query<int>("select ?x? + ?y_2? + ?z?", new { x = 1, y_2 = 3, z = 5, z2 = 24 }).Single();
                Assert.Equal(9, value);
            }
        }

        [Fact]
        public void Issue601_InternationalParameterNamesWork_OleDb()
        {
            // pseudo-positional
            using (var connection = GetOleDbConnection())
            {
                int value = connection.QuerySingle<int>("select ?æøå٦?", new { æøå٦ = 42 });
            }
        }

        [Fact]
        public void PseudoPositionalParameters_Dynamic()
        {
            using (var connection = GetOleDbConnection())
            {
                var args = new DynamicParameters();
                args.Add("x", 1);
                args.Add("y_2", 3);
                args.Add("z", 5);
                args.Add("z2", 24);
                int value = connection.Query<int>("select ?x? + ?y_2? + ?z?", args).Single();
                Assert.Equal(9, value);
            }
        }

        [Fact]
        public void PseudoPositionalParameters_ReusedParameter()
        {
            using (var connection = GetOleDbConnection())
            {
                var ex = Assert.Throws<InvalidOperationException>(() => connection.Query<int>("select ?x? + ?y_2? + ?x?", new { x = 1, y_2 = 3 }).Single());
                Assert.Equal("When passing parameters by position, each parameter can only be referenced once", ex.Message);
            }
        }

        [Fact]
        public void Issue569_SO38527197_PseudoPositionalParameters_In_And_Other_Condition()
        {
            const string sql = @"select s1.value as id, s2.value as score 
                        from string_split('1,2,3,4,5',',') s1, string_split('1,2,3,4,5',',') s2
                        where s1.value in ?ids? and s2.value = ?score?";
            using (var connection = GetOleDbConnection())
            {
                const int score = 2;
                int[] ids = { 1, 2, 5, 7 };
                var list = connection.Query<int>(sql, new { ids, score }).AsList();
                list.Sort();
                Assert.Equal("1,2,5", string.Join(",", list));
            }
        }

        [Fact]
        public void Issue569_SO38527197_PseudoPositionalParameters_In()
        {
            using (var connection = GetOleDbConnection())
            {
                int[] ids = { 1, 2, 5, 7 };
                var list = connection.Query<int>("select * from string_split('1,2,3,4,5',',') where value in ?ids?", new { ids }).AsList();
                list.Sort();
                Assert.Equal("1,2,5", string.Join(",", list));
            }
        }

        [Fact]
        public void PseudoPositional_CanUseVariable()
        {
            using (var connection = GetOleDbConnection())
            {
                const int id = 42;
                var row = connection.QuerySingle("declare @id int = ?id?; select @id as [A], @id as [B];", new { id });
                int a = (int)row.A;
                int b = (int)row.B;
                Assert.Equal(42, a);
                Assert.Equal(42, b);
            }
        }

        [Fact]
        public void PseudoPositional_CannotUseParameterMultipleTimes()
        {
            using (var connection = GetOleDbConnection())
            {
                var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    const int id = 42;
                    connection.QuerySingle("select ?id? as [A], ?id? as [B];", new { id });
                });
                Assert.Equal("When passing parameters by position, each parameter can only be referenced once", ex.Message);
            }
        }

        [Fact]
        public void PseudoPositionalParameters_ExecSingle()
        {
            using (var connection = GetOleDbConnection())
            {
                var data = new { x = 6 };
                connection.Execute("create table #named_single(val int not null)");
                int count = connection.Execute("insert #named_single (val) values (?x?)", data);
                int sum = (int)connection.ExecuteScalar("select sum(val) from #named_single");
                Assert.Equal(1, count);
                Assert.Equal(6, sum);
            }
        }

        [Fact]
        public void PseudoPositionalParameters_ExecMulti()
        {
            using (var connection = GetOleDbConnection())
            {
                var data = new[]
                {
                    new { x = 1, y = 1 },
                    new { x = 3, y = 1 },
                    new { x = 6, y = 1 },
                };
                connection.Execute("create table #named_multi(val int not null)");
                int count = connection.Execute("insert #named_multi (val) values (?x?)", data);
                int sum = (int)connection.ExecuteScalar("select sum(val) from #named_multi");
                Assert.Equal(3, count);
                Assert.Equal(10, sum);
            }
        }

        [Fact]
        public void Issue457_NullParameterValues()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ? -- ODBC parameter
SET @customerCode = ? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = GetOleDbConnection())
            {
                DateTime? since = null; // DateTime.Now.Date;
                const string code = null;  // "abc";
                var row = connection.QuerySingle(sql, new
                {
                    since,
                    customerCode = code
                });
                var a = (DateTime?)row.Since;
                var b = (string)row.Code;

                Assert.Equal(since, a);
                Assert.Equal(code, b);
            }
        }

        [Fact]
        public void Issue457_NullParameterValues_Named()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ?since? -- ODBC parameter
SET @customerCode = ?customerCode? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = GetOleDbConnection())
            {
                DateTime? since = null; // DateTime.Now.Date;
                const string code = null;  // "abc";
                var row = connection.QuerySingle(sql, new
                {
                    since,
                    customerCode = code
                });
                var a = (DateTime?)row.Since;
                var b = (string)row.Code;

                Assert.Equal(since, a);
                Assert.Equal(code, b);
            }
        }

        [Fact]
        public async void Issue457_NullParameterValues_MultiAsync()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ? -- ODBC parameter
SET @customerCode = ? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = GetOleDbConnection())
            {
                DateTime? since = null; // DateTime.Now.Date;
                const string code = null;  // "abc";
                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    since,
                    customerCode = code
                }).ConfigureAwait(false))
                {
                    var row = await multi.ReadSingleAsync().ConfigureAwait(false);
                    var a = (DateTime?)row.Since;
                    var b = (string)row.Code;

                    Assert.Equal(a, since);
                    Assert.Equal(b, code);
                }
            }
        }

        [Fact]
        public async void Issue457_NullParameterValues_MultiAsync_Named()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ?since? -- ODBC parameter
SET @customerCode = ?customerCode? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = GetOleDbConnection())
            {
                DateTime? since = null; // DateTime.Now.Date;
                const string code = null;  // "abc";
                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    since,
                    customerCode = code
                }).ConfigureAwait(false))
                {
                    var row = await multi.ReadSingleAsync().ConfigureAwait(false);
                    var a = (DateTime?)row.Since;
                    var b = (string)row.Code;

                    Assert.Equal(a, since);
                    Assert.Equal(b, code);
                }
            }
        }
    }
}
#endif
