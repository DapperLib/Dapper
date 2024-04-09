using System;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection("SqlBuilderTests")]
    public sealed class SystemSqlClientSqlBuilderTests : SqlBuilderTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("SqlBuilderTests")]
    public sealed class MicrosoftSqlClientSqlBuilderTests : SqlBuilderTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class SqlBuilderTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestSqlBuilderWithDapperQuery()
        {
            var sb = new SqlBuilder();
            var template = sb.AddTemplate("SELECT /**select**/ FROM #Users /**where**/");
            sb.Where("Age <= @Age", new { Age = 18 })
              .Where("Country = @Country", new { Country = "USA" })
              .Select("Name,Age,Country");

            const string createSql = @"
                create table #Users (Name varchar(20),Age int,Country nvarchar(5));
                insert #Users values('Sam',16,'USA'),('Tom',25,'UK'),('Henry',14,'UK')";
            try
            {
                connection.Execute(createSql);

                var result = connection.Query(template.RawSql,template.Parameters).ToArray();

                Assert.Equal("SELECT Name,Age,Country\n FROM #Users WHERE Age <= @Age AND Country = @Country\n", template.RawSql);

                Assert.Single(result);

                Assert.Equal(16, (int)result[0].Age);
                Assert.Equal("Sam", (string)result[0].Name);
                Assert.Equal("USA", (string)result[0].Country);
            }
            finally
            {
                connection.Execute("drop table #Users");
            }
        }

        [Fact]
        public void TestSqlBuilderUpdateSet()
        {
            var id = 1;
            var vip = true;
            var updatetime = DateTime.Parse("2020/01/01");

            var sb = new SqlBuilder()
                   .Set("Vip = @vip", new { vip })
                   .Set("Updatetime = @updatetime", new { updatetime })
                   .Where("Id = @id", new { id })
            ;
            var template = sb.AddTemplate("update #Users /**set**/ /**where**/");

            const string createSql = @"
                create table #Users (Id int,Name varchar(20),Age int,Country nvarchar(5),Vip bit,Updatetime datetime);
                insert #Users (Id,Name,Age,Country) values(1,'Sam',16,'USA'),(2,'Tom',25,'UK'),(3,'Henry',14,'UK')";
            try
            {
                connection.Execute(createSql);

                var effectCount = connection.Execute(template.RawSql, template.Parameters);

                var result = connection.QueryFirst("select * from #Users where Id = 1");

                Assert.Equal("update #Users SET Vip = @vip , Updatetime = @updatetime\n WHERE Id = @id\n", template.RawSql);


                Assert.True((bool)result.Vip);
                Assert.Equal(updatetime, (DateTime)result.Updatetime);
            }
            finally
            {
                connection.Execute("drop table #Users");
            }
        }
    }
}
