using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection("QueryMultipleTests")]
    public sealed class SystemSqlClientQueryMultipleTests : QueryMultipleTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("QueryMultipleTests")]
    public sealed class MicrosoftSqlClientQueryMultipleTests : QueryMultipleTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class QueryMultipleTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
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

                Assert.Equal(1, a.Single());
                Assert.Equal(2, b.Single());
                Assert.Equal(3, c.Single());
                Assert.Equal(4, d.Single());
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

                Assert.Equal(1, a);
                Assert.Equal(2, b);
                Assert.Equal(3, c);
                Assert.Equal(4, d);
            }
        }

        [Fact]
        public void TestMultiReaderBasic()
        {
            const string sql = "select 1 as Id union all select 2 as Id     select 'abc' as name   select 1 as Id union all select 2 as Id";
            int i, j;
            string s;
            using (var multi = connection.QueryMultiple(sql))
            {
                i = multi.Read<int>().First();
                s = multi.Read<string>().Single();
                j = multi.Read<int>().Sum();
            }
            Assert.Equal(1, i);
            Assert.Equal("abc", s);
            Assert.Equal(3, j);
        }

        [Fact]
        public void TestReadDynamicWithGridReader()
        {
            const string createSql = @"
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

                const string sql = @"SELECT * FROM #Users ORDER BY Id
                        SELECT * FROM #Posts ORDER BY Id DESC";

                var grid = connection.QueryMultiple(sql);

                var users = grid.Read().ToList();
                var posts = grid.Read().ToList();

                Assert.Equal(2, users.Count);
                Assert.Equal(3, posts.Count);

                Assert.Equal(2, (int)users[0].Id);
                Assert.Equal(3, (int)posts[0].Id);
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
            Assert.Equal(3, retVal);
        }

        [Fact]
        public void Issue524_QueryMultiple_Cast()
        {
            // aka: Read<int> should work even if the data is a <long>
            // using regular API
            Assert.Equal(42, connection.Query<int>("select cast(42 as bigint)").Single());
            Assert.Equal(42, connection.QuerySingle<int>("select cast(42 as bigint)"));

            // using multi-reader API
            using (var reader = connection.QueryMultiple("select cast(42 as bigint); select cast(42 as bigint)"))
            {
                Assert.Equal(42, reader.Read<int>().Single());
                Assert.Equal(42, reader.ReadSingle<int>());
            }
        }

        [Fact]
        public void QueryMultipleFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                using (var multi = conn.QueryMultiple("select 1; select 'abc';"))
                {
                    Assert.Equal(1, multi.Read<int>().Single());
                    Assert.Equal("abc", multi.Read<string>().Single());
                }
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
        }

        [Fact]
        public void QueryMultiple2FromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                Assert.Equal(ConnectionState.Closed, conn.State);
                using (var multi = conn.QueryMultiple("select 1 select 2 select 3"))
                {
                    Assert.Equal(1, multi.Read<int>().Single());
                    Assert.Equal(2, multi.Read<int>().Single());
                    // not reading 3 is intentional here
                }
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
        }

        [Fact]
        public void SO35554284_QueryMultipleUntilConsumed()
        {
            using (var reader = connection.QueryMultiple("select 1 as Id; select 2 as Id; select 3 as Id;"))
            {
                var items = new List<HazNameId>();
                while (!reader.IsConsumed)
                {
                    items.AddRange(reader.Read<HazNameId>());
                }
                Assert.Equal(3, items.Count);
                Assert.Equal(1, items[0].Id);
                Assert.Equal(2, items[1].Id);
                Assert.Equal(3, items[2].Id);
            }
        }

        [Fact]
        public void QueryMultipleInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                Assert.ThrowsAny<Exception>(() => conn.QueryMultiple("select gibberish"));
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
        }

        [Fact]
        public void TestMultiSelectWithSomeEmptyGridsUnbuffered() => TestMultiSelectWithSomeEmptyGrids(false);

        [Fact]
        public void TestMultiSelectWithSomeEmptyGridsBuffered() => TestMultiSelectWithSomeEmptyGrids(true);

        private void TestMultiSelectWithSomeEmptyGrids(bool buffered)
        {
            using (var reader = connection.QueryMultiple("select 1; select 2 where 1 = 0; select 3 where 1 = 0; select 4;"))
            {
                var one = reader.Read<int>(buffered: buffered).ToArray();
                var two = reader.Read<int>(buffered: buffered).ToArray();
                var three = reader.Read<int>(buffered: buffered).ToArray();
                var four = reader.Read<int>(buffered: buffered).ToArray();
                try
                { // only returned four grids; expect a fifth read to fail
                    reader.Read<int>(buffered: buffered);
                    throw new InvalidOperationException("this should not have worked!");
                }
                catch (ObjectDisposedException ex)
                { // expected; success
                    Assert.Equal("The reader has been disposed; this can happen after all data has been consumed\r\nObject name: 'Dapper.SqlMapper+GridReader'.", ex.Message);
                }

                Assert.Single(one);
                Assert.Equal(1, one[0]);
                Assert.Empty(two);
                Assert.Empty(three);
                Assert.Single(four);
                Assert.Equal(4, four[0]);
            }
        }

        [Fact]
        public void TypeBasedViaTypeMulti()
        {
            Type type = Common.GetSomeType();

            dynamic first, second;
            using (var multi = connection.QueryMultiple("select @A as [A], @B as [B]; select @C as [A], @D as [B]",
                new { A = 123, B = "abc", C = 456, D = "def" }))
            {
                first = multi.Read(type).Single();
                second = multi.Read(type).Single();
            }
            Assert.Equal(((object)first).GetType(), type);
            int a = first.A;
            string b = first.B;
            Assert.Equal(123, a);
            Assert.Equal("abc", b);

            Assert.Equal(((object)second).GetType(), type);
            a = second.A;
            b = second.B;
            Assert.Equal(456, a);
            Assert.Equal("def", b);
        }
    }
}
