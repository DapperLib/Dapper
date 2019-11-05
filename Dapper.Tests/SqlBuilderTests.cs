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
    }
}
