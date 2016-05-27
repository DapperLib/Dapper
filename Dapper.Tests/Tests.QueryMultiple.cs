using Dapper;
using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public partial class TestSuite
    {
        [Fact]
        public void TestQueryMultipleBuffered()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select @x; select 4", new { x = 3 }))
            {
                var a = grid.Read<int>();
                var b = grid.Read<int>();
                var c = grid.Read<int>();
                var d = grid.Read<int>();

                a.Single().Equals(1);
                b.Single().Equals(2);
                c.Single().Equals(3);
                d.Single().Equals(4);
            }
        }

        [Fact]
        public void TestQueryMultipleNonBufferedIncorrectOrder()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select @x; select 4", new { x = 3 }))
            {
                var a = grid.Read<int>(false);
                try
                {
                    var b = grid.Read<int>(false);
                    throw new InvalidOperationException(); // should have thrown
                }
                catch (InvalidOperationException)
                {
                    // that's expected
                }
            }
        }

        [Fact]
        public void TestQueryMultipleNonBufferedCorrectOrder()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select @x; select 4", new { x = 3 }))
            {
                var a = grid.Read<int>(false).Single();
                var b = grid.Read<int>(false).Single();
                var c = grid.Read<int>(false).Single();
                var d = grid.Read<int>(false).Single();

                a.Equals(1);
                b.Equals(2);
                c.Equals(3);
                d.Equals(4);
            }
        }

        [Fact]
        public void TestMultiReaderBasic()
        {
            var sql = @"select 1 as Id union all select 2 as Id     select 'abc' as name   select 1 as Id union all select 2 as Id";
            int i, j;
            string s;
            using (var multi = connection.QueryMultiple(sql))
            {
                i = multi.Read<int>().First();
                s = multi.Read<string>().Single();
                j = multi.Read<int>().Sum();
            }
            Assert.IsEqualTo(i, 1);
            Assert.IsEqualTo(s, "abc");
            Assert.IsEqualTo(j, 3);
        }

        [Fact]
        public void TestReadDynamicWithGridReader()
        {
            var createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')";
            try
            {
                connection.Execute(createSql);

                var sql = @"SELECT * FROM #Users ORDER BY Id
                        SELECT * FROM #Posts ORDER BY Id DESC";

                var grid = connection.QueryMultiple(sql);

                var users = grid.Read().ToList();
                var posts = grid.Read().ToList();

                users.Count.IsEqualTo(2);
                posts.Count.IsEqualTo(3);

                ((int)users.First().Id).IsEqualTo(2);
                ((int)posts.First().Id).IsEqualTo(3);
            }
            finally
            {
                connection.Execute("drop table #Users drop table #Posts");
            }
        }

        [Fact]
        public void Issue268_ReturnQueryMultiple()
        {
            connection.Execute(@"create proc #TestProc268 (@a int, @b int, @c int)as 
begin
select @a;

select @b

return @c; 
end");
            var p = new DynamicParameters(new { a = 1, b = 2, c = 3 });
            p.Add("RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            using (var reader = connection.QueryMultiple("#TestProc268", p, commandType: CommandType.StoredProcedure))
            {
                reader.Read();
            }
            var retVal = p.Get<int>("RetVal");
            retVal.IsEqualTo(3);
        }
        
        [Fact]
        public void Issue524_QueryMultiple_Cast()
        { // aka: Read<int> should work even if the data is a <long>
            
            // using regular API
            connection.Query<int>("select cast(42 as bigint)").Single().IsEqualTo(42);
            connection.QuerySingle<int>("select cast(42 as bigint)").IsEqualTo(42);
            
            // using multi-reader API
            using(var reader = connection.QueryMultiple("select cast(42 as bigint); select cast(42 as bigint)"))
            {
                reader.Read<int>().Single().IsEqualTo(42);
                reader.ReadSingle<int>().IsEqualTo(42);
            }
        }
    }
}
