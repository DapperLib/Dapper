using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection("LiteralTests")]
    public sealed class SystemSqlClientLiteralTests : LiteralTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("LiteralTests")]
    public sealed class MicrosoftSqlClientLiteralTests : LiteralTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class LiteralTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void LiteralReplacementEnumAndString()
        {
            var args = new { x = AnEnum.B, y = 123.45M, z = AnotherEnum.A };
            var row = connection.Query("select {=x} as x,{=y} as y,cast({=z} as tinyint) as z", args).Single();
            AnEnum x = (AnEnum)(int)row.x;
            decimal y = row.y;
            AnotherEnum z = (AnotherEnum)(byte)row.z;
            Assert.Equal(AnEnum.B, x);
            Assert.Equal(123.45M, y);
            Assert.Equal(AnotherEnum.A, z);
        }

        [Fact]
        public void LiteralReplacementDynamicEnumAndString()
        {
            var args = new DynamicParameters();
            args.Add("x", AnEnum.B);
            args.Add("y", 123.45M);
            args.Add("z", AnotherEnum.A);
            var row = connection.Query("select {=x} as x,{=y} as y,cast({=z} as tinyint) as z", args).Single();
            AnEnum x = (AnEnum)(int)row.x;
            decimal y = row.y;
            AnotherEnum z = (AnotherEnum)(byte)row.z;
            Assert.Equal(AnEnum.B, x);
            Assert.Equal(123.45M, y);
            Assert.Equal(AnotherEnum.A, z);
        }

        [Fact]
        public void LiteralReplacementBoolean()
        {
            var row = connection.Query<int?>("select 42 where 1 = {=val}", new { val = true }).SingleOrDefault();
            Assert.NotNull(row);
            Assert.Equal(42, row);
            row = connection.Query<int?>("select 42 where 1 = {=val}", new { val = false }).SingleOrDefault();
            Assert.Null(row);
        }

        [Fact]
        public void LiteralReplacementWithIn()
        {
            var data = connection.Query<MyRow>("select @x where 1 in @ids and 1 ={=a}",
                new { x = 1, ids = new[] { 1, 2, 3 }, a = 1 }).ToList();
        }

        private class MyRow
        {
            public int x { get; set; }
        }

        [Fact]
        public void LiteralIn()
        {
            connection.Execute("create table #literalin(id int not null);");
            connection.Execute("insert #literalin (id) values (@id)", new[] {
                new { id = 1 },
                new { id = 2 },
                new { id = 3 },
            });
            var count = connection.Query<int>("select count(1) from #literalin where id in {=ids}",
                new { ids = new[] { 1, 3, 4 } }).Single();
            Assert.Equal(2, count);
        }

        [Fact]
        public void LiteralReplacement()
        {
            connection.Execute("create table #literal1 (id int not null, foo int not null)");
            connection.Execute("insert #literal1 (id,foo) values ({=id}, @foo)", new { id = 123, foo = 456 });
            var rows = new[] { new { id = 1, foo = 2 }, new { id = 3, foo = 4 } };
            connection.Execute("insert #literal1 (id,foo) values ({=id}, @foo)", rows);
            var count = connection.Query<int>("select count(1) from #literal1 where id={=foo}", new { foo = 123 }).Single();
            Assert.Equal(1, count);
            int sum = connection.Query<int>("select sum(id) + sum(foo) from #literal1").Single();
            Assert.Equal(sum, 123 + 456 + 1 + 2 + 3 + 4);
        }

        [Fact]
        public void LiteralReplacementDynamic()
        {
            var args = new DynamicParameters();
            args.Add("id", 123);
            connection.Execute("create table #literal2 (id int not null)");
            connection.Execute("insert #literal2 (id) values ({=id})", args);

            args = new DynamicParameters();
            args.Add("foo", 123);
            var count = connection.Query<int>("select count(1) from #literal2 where id={=foo}", args).Single();
            Assert.Equal(1, count);
        }
    }
}
