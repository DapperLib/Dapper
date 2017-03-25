using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class QueryMultipleTests : TestBase
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
            const string sql = @"select 1 as Id union all select 2 as Id     select 'abc' as name   select 1 as Id union all select 2 as Id";
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
        { 
            // aka: Read<int> should work even if the data is a <long>
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

        [Fact]
        public void QueryMultipleFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                using (var multi = conn.QueryMultiple("select 1; select 'abc';"))
                {
                    multi.Read<int>().Single().IsEqualTo(1);
                    multi.Read<string>().Single().IsEqualTo("abc");
                }
                conn.State.IsEqualTo(ConnectionState.Closed);
            }
        }

        [Fact]
        public void QueryMultiple2FromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                conn.State.IsEqualTo(ConnectionState.Closed);
                using (var multi = conn.QueryMultiple("select 1 select 2 select 3"))
                {
                    multi.Read<int>().Single().IsEqualTo(1);
                    multi.Read<int>().Single().IsEqualTo(2);
                    // not reading 3 is intentional here
                }
                conn.State.IsEqualTo(ConnectionState.Closed);
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
                items.Count.IsEqualTo(3);
                items[0].Id.IsEqualTo(1);
                items[1].Id.IsEqualTo(2);
                items[2].Id.IsEqualTo(3);
            }
        }

        [Fact]
        public void QueryMultipleInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                try
                {
                    conn.QueryMultiple("select gibberish");
                    false.IsEqualTo(true); // shouldn't have got here
                }
                catch
                {
                    conn.State.IsEqualTo(ConnectionState.Closed);
                }
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
                    ex.Message.IsEqualTo("The reader has been disposed; this can happen after all data has been consumed\r\nObject name: 'Dapper.SqlMapper+GridReader'.");
                }

                one.Length.IsEqualTo(1);
                one[0].IsEqualTo(1);
                two.Length.IsEqualTo(0);
                three.Length.IsEqualTo(0);
                four.Length.IsEqualTo(1);
                four[0].IsEqualTo(4);
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
            ((object)first).GetType().IsEqualTo(type);
            int a = first.A;
            string b = first.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");

            ((object)second).GetType().IsEqualTo(type);
            a = second.A;
            b = second.B;
            a.IsEqualTo(456);
            b.IsEqualTo("def");
        }
    }
}
