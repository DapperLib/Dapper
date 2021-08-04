#if NET5_0 || NETCOREAPP3_1
using System.Linq;
using System.Data;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Data.Common;
using System.Linq;
using Xunit.Abstractions;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientAsyncStreamTests : AsyncStreamTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientAsyncStreamTests : AsyncStreamTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class AsyncStreamTests<TProvider> : TestBase<TProvider> where TProvider : SqlServerDatabaseProvider
    {
        private DbConnection _marsConnection;

        private DbConnection MarsConnection => _marsConnection ??= Provider.GetOpenConnection(true);

        [Fact]
        public async Task TestBasicStringUsageAsync()
        {
            var query = connection.StreamAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
            var arr = await query.ToListAsync().ConfigureAwait(false);
            Assert.Equal(new[] { "abc", "def" }, arr);
        }

        [Fact]
        public async Task TestBasicStringUsageAsyncNonBuffered()
        {
            var query = connection.StreamAsync<string>(new CommandDefinition("select 'abc' as [Value] union all select @txt", new { txt = "def" }, flags: CommandFlags.None));
            var arr = await query.ToArrayAsync().ConfigureAwait(false);
            Assert.Equal(new[] { "abc", "def" }, arr);
        }

        [Fact]
        public async Task TestLongOperationWithCancellation()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var task = connection.StreamAsync<int>(new CommandDefinition("waitfor delay '00:00:10';select 1", cancellationToken: cancel.Token)).ToListAsync().AsTask();
            try
            {
                if (!task.Wait(TimeSpan.FromSeconds(7)))
                {
                    throw new TimeoutException(); // should have cancelled
                }
            }
            catch (AggregateException agg)
            {
                Assert.True(agg.InnerException.GetType().Name == "SqlException");
            }
        }

        [Fact]
        public async Task TestBasicStringUsageClosedAsync()
        {
            using (var conn = GetClosedConnection())
            {
                var query = conn.StreamAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = await query.ToArrayAsync().ConfigureAwait(false);
                Assert.Equal(new[] { "abc", "def" }, arr);
            }
        }

        [Fact]
        public async Task TestQueryDynamicAsync()
        {
            var row = await connection.StreamAsync("select 'abc' as [Value]").SingleAsync().ConfigureAwait(false);
            string value = row.Value;
            Assert.Equal("abc", value);
        }

        [Fact]
        public async Task TestClassWithStringUsageAsync()
        {
            var query = connection.StreamAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
            var arr = await query.ToArrayAsync().ConfigureAwait(false);
            Assert.Equal(new[] { "abc", "def" }, arr.Select(x => x.Value));
        }

        [Fact]
        public async Task TestMultiMapWithSplitAsync()
        {
            const string sql = "select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            var productQuery = connection.StreamAsync<Product, Category, Product>(sql, (prod, cat) =>
            {
                prod.Category = cat;
                return prod;
            });

            var product = await productQuery.FirstAsync().ConfigureAwait(false);
            // assertions
            Assert.Equal(1, product.Id);
            Assert.Equal("abc", product.Name);
            Assert.Equal(2, product.Category.Id);
            Assert.Equal("def", product.Category.Name);
        }

        [Fact]
        public async Task TestMultiMapArbitraryWithSplitAsync()
        {
            const string sql = "select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            var productQuery = connection.StreamAsync<Product>(sql, new[] { typeof(Product), typeof(Category) }, (objects) =>
            {
                var prod = (Product)objects[0];
                prod.Category = (Category)objects[1];
                return prod;
            });

            var product = await productQuery.FirstAsync().ConfigureAwait(false);
            // assertions
            Assert.Equal(1, product.Id);
            Assert.Equal("abc", product.Name);
            Assert.Equal(2, product.Category.Id);
            Assert.Equal("def", product.Category.Name);
        }

        [Fact]
        public async Task TestMultiMapWithSplitClosedConnAsync()
        {
            const string sql = "select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            using (var conn = GetClosedConnection())
            {
                var productQuery = conn.StreamAsync<Product, Category, Product>(sql, (prod, cat) =>
                {
                    prod.Category = cat;
                    return prod;
                });

                var product = await productQuery.FirstAsync().ConfigureAwait(false);
                // assertions
                Assert.Equal(1, product.Id);
                Assert.Equal("abc", product.Name);
                Assert.Equal(2, product.Category.Id);
                Assert.Equal("def", product.Category.Name);
            }
        }

        [Fact]
        public async Task TestMultiAsync()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2").ConfigureAwait(false))
            {
                Assert.Equal(1, multi.StreamAsync<int>().SingleAsync().Result);
                Assert.Equal(2, multi.StreamAsync<int>().SingleAsync().Result);
            }
        }

        [Fact]
        public async Task TestMultiConversionAsync()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select Cast(1 as BigInt) Col1; select Cast(2 as BigInt) Col2").ConfigureAwait(false))
            {
                Assert.Equal(1, multi.StreamAsync<int>().SingleAsync().Result);
                Assert.Equal(2, multi.StreamAsync<int>().SingleAsync().Result);
            }
        }

        [Fact]
        public async Task TestMultiAsyncViaFirstOrDefault()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2; select 3; select 4; select 5").ConfigureAwait(false))
            {
                Assert.Equal(1, multi.ReadFirstOrDefaultAsync<int>().Result);
                Assert.Equal(2, multi.StreamAsync<int>().SingleAsync().Result);
                Assert.Equal(3, multi.ReadFirstOrDefaultAsync<int>().Result);
                Assert.Equal(4, multi.StreamAsync<int>().SingleAsync().Result);
                Assert.Equal(5, multi.ReadFirstOrDefaultAsync<int>().Result);
            }
        }

        [Fact]
        public async Task TestMultiClosedConnAsync()
        {
            using (var conn = GetClosedConnection())
            {
                using (SqlMapper.GridReader multi = await conn.QueryMultipleAsync("select 1; select 2").ConfigureAwait(false))
                {
                    Assert.Equal(1, multi.StreamAsync<int>().SingleAsync().Result);
                    Assert.Equal(2, multi.StreamAsync<int>().SingleAsync().Result);
                }
            }
        }

        [Fact]
        public async Task TestMultiClosedConnAsyncViaFirstOrDefault()
        {
            using (var conn = GetClosedConnection())
            {
                using (SqlMapper.GridReader multi = await conn.QueryMultipleAsync("select 1; select 2; select 3; select 4; select 5").ConfigureAwait(false))
                {
                    Assert.Equal(1, multi.ReadFirstOrDefaultAsync<int>().Result);
                    Assert.Equal(2, multi.StreamAsync<int>().SingleAsync().Result);
                    Assert.Equal(3, multi.ReadFirstOrDefaultAsync<int>().Result);
                    Assert.Equal(4, multi.StreamAsync<int>().SingleAsync().Result);
                    Assert.Equal(5, multi.ReadFirstOrDefaultAsync<int>().Result);
                }
            }
        }

        private static async Task LiteralReplacement(IDbConnection conn)
        {
            try
            {
                await conn.ExecuteAsync("drop table literal1").ConfigureAwait(false);
            }
            catch { /* don't care */ }
            await conn.ExecuteAsync("create table literal1 (id int not null, foo int not null)").ConfigureAwait(false);
            await conn.ExecuteAsync("insert literal1 (id,foo) values ({=id}, @foo)", new { id = 123, foo = 456 }).ConfigureAwait(false);
            var rows = new[] { new { id = 1, foo = 2 }, new { id = 3, foo = 4 } };
            await conn.ExecuteAsync("insert literal1 (id,foo) values ({=id}, @foo)", rows).ConfigureAwait(false);
            var count = await conn.StreamAsync<int>("select count(1) from literal1 where id={=foo}", new { foo = 123 }).SingleAsync().ConfigureAwait(false);
            Assert.Equal(1, count);
            int sum = await conn.StreamAsync<int>("select sum(id) + sum(foo) from literal1").SingleAsync().ConfigureAwait(false);
            Assert.Equal(sum, 123 + 456 + 1 + 2 + 3 + 4);
        }

        private static async Task LiteralReplacementDynamic(IDbConnection conn)
        {
            var args = new DynamicParameters();
            args.Add("id", 123);
            try { await conn.ExecuteAsync("drop table literal2").ConfigureAwait(false); }
            catch { /* don't care */ }
            await conn.ExecuteAsync("create table literal2 (id int not null)").ConfigureAwait(false);
            await conn.ExecuteAsync("insert literal2 (id) values ({=id})", args).ConfigureAwait(false);

            args = new DynamicParameters();
            args.Add("foo", 123);
            var count = await conn.StreamAsync<int>("select count(1) from literal2 where id={=foo}", args).SingleAsync().ConfigureAwait(false);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task LiteralInAsync()
        {
            await connection.ExecuteAsync("create table #literalin(id int not null);").ConfigureAwait(false);
            await connection.ExecuteAsync("insert #literalin (id) values (@id)", new[] {
                new { id = 1 },
                new { id = 2 },
                new { id = 3 },
            }).ConfigureAwait(false);
            var count = await connection.StreamAsync<int>("select count(1) from #literalin where id in {=ids}",
                new { ids = new[] { 1, 3, 4 } }).SingleAsync().ConfigureAwait(false);
            Assert.Equal(2, count);
        }

        private class BasicType
        {
            public string Value { get; set; }
        }

        [Fact]
        public async Task TypeBasedViaTypeAsync()
        {
            Type type = Common.GetSomeType();

            dynamic actual = await MarsConnection.StreamAsync(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).FirstOrDefaultAsync().ConfigureAwait(false);
            Assert.Equal(((object)actual).GetType(), type);
            int a = actual.A;
            string b = actual.B;
            Assert.Equal(123, a);
            Assert.Equal("abc", b);
        }

        [Fact]
        public async Task Issue346_StreamAsyncConvert()
        {
            int i = await connection.StreamAsync<int>("Select Cast(123 as bigint)").FirstAsync().ConfigureAwait(false);
            Assert.Equal(123, i);
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressions_Query_Default()
        {
            var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

            var p = new DynamicParameters(bob);
            p.Output(bob, b => b.PersonId);
            p.Output(bob, b => b.Occupation);
            p.Output(bob, b => b.NumberOfLegs);
            p.Output(bob, b => b.Address.Name);
            p.Output(bob, b => b.Address.PersonId);

            var result = await connection.StreamAsync<int>(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p).SingleAsync().ConfigureAwait(false);

            Assert.Equal("grillmaster", bob.Occupation);
            Assert.Equal(2, bob.PersonId);
            Assert.Equal(1, bob.NumberOfLegs);
            Assert.Equal("bobs burgers", bob.Address.Name);
            Assert.Equal(2, bob.Address.PersonId);
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressions_Query_BufferedAsync()
        {
            var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

            var p = new DynamicParameters(bob);
            p.Output(bob, b => b.PersonId);
            p.Output(bob, b => b.Occupation);
            p.Output(bob, b => b.NumberOfLegs);
            p.Output(bob, b => b.Address.Name);
            p.Output(bob, b => b.Address.PersonId);

            var result = await connection.StreamAsync<int>(new CommandDefinition(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, flags: CommandFlags.Buffered)).SingleAsync().ConfigureAwait(false);

            Assert.Equal("grillmaster", bob.Occupation);
            Assert.Equal(2, bob.PersonId);
            Assert.Equal(1, bob.NumberOfLegs);
            Assert.Equal("bobs burgers", bob.Address.Name);
            Assert.Equal(2, bob.Address.PersonId);
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressions_Query_NonBufferedAsync()
        {
            var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

            var p = new DynamicParameters(bob);
            p.Output(bob, b => b.PersonId);
            p.Output(bob, b => b.Occupation);
            p.Output(bob, b => b.NumberOfLegs);
            p.Output(bob, b => b.Address.Name);
            p.Output(bob, b => b.Address.PersonId);

            var result = await connection.StreamAsync<int>(new CommandDefinition(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, flags: CommandFlags.None)).SingleAsync().ConfigureAwait(false);

            Assert.Equal("grillmaster", bob.Occupation);
            Assert.Equal(2, bob.PersonId);
            Assert.Equal(1, bob.NumberOfLegs);
            Assert.Equal("bobs burgers", bob.Address.Name);
            Assert.Equal(2, bob.Address.PersonId);
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressions_QueryMultipleAsync()
        {
            var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

            var p = new DynamicParameters(bob);
            p.Output(bob, b => b.PersonId);
            p.Output(bob, b => b.Occupation);
            p.Output(bob, b => b.NumberOfLegs);
            p.Output(bob, b => b.Address.Name);
            p.Output(bob, b => b.Address.PersonId);

            int x, y;
            using (var multi = await connection.QueryMultipleAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
select 42
select 17
SET @AddressPersonId = @PersonId", p).ConfigureAwait(false))
            {
                x = multi.StreamAsync<int>().SingleAsync().Result;
                y = multi.StreamAsync<int>().SingleAsync().Result;
            }

            Assert.Equal("grillmaster", bob.Occupation);
            Assert.Equal(2, bob.PersonId);
            Assert.Equal(1, bob.NumberOfLegs);
            Assert.Equal("bobs burgers", bob.Address.Name);
            Assert.Equal(2, bob.Address.PersonId);
            Assert.Equal(42, x);
            Assert.Equal(17, y);
        }

        [Fact]
        public async Task TestSubsequentQueriesSuccessAsync()
        {
            var data0 = await connection.StreamAsync<AsyncFoo0>("select 1 as [Id] where 1 = 0").ToListAsync().ConfigureAwait(false);
            Assert.Empty(data0);

            var data1 = await connection.StreamAsync<AsyncFoo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ToListAsync().ConfigureAwait(false);
            Assert.Empty(data1);

            var data2 = await connection.StreamAsync<AsyncFoo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ToListAsync().ConfigureAwait(false);
            Assert.Empty(data2);

            data0 = await connection.StreamAsync<AsyncFoo0>("select 1 as [Id] where 1 = 0").ToListAsync().ConfigureAwait(false);
            Assert.Empty(data0);

            data1 = await connection.StreamAsync<AsyncFoo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ToListAsync().ConfigureAwait(false);
            Assert.Empty(data1);

            data2 = await connection.StreamAsync<AsyncFoo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ToListAsync().ConfigureAwait(false);
            Assert.Empty(data2);
        }

        private class AsyncFoo0 { public int Id { get; set; } }

        private class AsyncFoo1 { public int Id { get; set; } }

        private class AsyncFoo2 { public int Id { get; set; } }

        [Fact]
        public async Task TestMultiMapArbitraryMapsAsync()
        {
            // please excuse the trite example, but it is easier to follow than a more real-world one
            const string createSql = @"
                create table #ReviewBoards (Id int, Name varchar(20), User1Id int, User2Id int, User3Id int, User4Id int, User5Id int, User6Id int, User7Id int, User8Id int, User9Id int)
                create table #Users (Id int, Name varchar(20))

                insert #Users values(1, 'User 1')
                insert #Users values(2, 'User 2')
                insert #Users values(3, 'User 3')
                insert #Users values(4, 'User 4')
                insert #Users values(5, 'User 5')
                insert #Users values(6, 'User 6')
                insert #Users values(7, 'User 7')
                insert #Users values(8, 'User 8')
                insert #Users values(9, 'User 9')

                insert #ReviewBoards values(1, 'Review Board 1', 1, 2, 3, 4, 5, 6, 7, 8, 9)
";
            await connection.ExecuteAsync(createSql).ConfigureAwait(false);
            try
            {
                const string sql = @"
                select 
                    rb.Id, rb.Name,
                    u1.*, u2.*, u3.*, u4.*, u5.*, u6.*, u7.*, u8.*, u9.*
                from #ReviewBoards rb
                    inner join #Users u1 on u1.Id = rb.User1Id
                    inner join #Users u2 on u2.Id = rb.User2Id
                    inner join #Users u3 on u3.Id = rb.User3Id
                    inner join #Users u4 on u4.Id = rb.User4Id
                    inner join #Users u5 on u5.Id = rb.User5Id
                    inner join #Users u6 on u6.Id = rb.User6Id
                    inner join #Users u7 on u7.Id = rb.User7Id
                    inner join #Users u8 on u8.Id = rb.User8Id
                    inner join #Users u9 on u9.Id = rb.User9Id
";

                var types = new[] { typeof(ReviewBoard), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User) };

                Func<object[], ReviewBoard> mapper = (objects) =>
                {
                    var board = (ReviewBoard)objects[0];
                    board.User1 = (User)objects[1];
                    board.User2 = (User)objects[2];
                    board.User3 = (User)objects[3];
                    board.User4 = (User)objects[4];
                    board.User5 = (User)objects[5];
                    board.User6 = (User)objects[6];
                    board.User7 = (User)objects[7];
                    board.User8 = (User)objects[8];
                    board.User9 = (User)objects[9];
                    return board;
                };

                var data = await connection.StreamAsync<ReviewBoard>(sql, types, mapper).ToListAsync().ConfigureAwait(false);

                var p = data[0];
                Assert.Equal(1, p.Id);
                Assert.Equal("Review Board 1", p.Name);
                Assert.Equal(1, p.User1.Id);
                Assert.Equal(2, p.User2.Id);
                Assert.Equal(3, p.User3.Id);
                Assert.Equal(4, p.User4.Id);
                Assert.Equal(5, p.User5.Id);
                Assert.Equal(6, p.User6.Id);
                Assert.Equal(7, p.User7.Id);
                Assert.Equal(8, p.User8.Id);
                Assert.Equal(9, p.User9.Id);
                Assert.Equal("User 1", p.User1.Name);
                Assert.Equal("User 2", p.User2.Name);
                Assert.Equal("User 3", p.User3.Name);
                Assert.Equal("User 4", p.User4.Name);
                Assert.Equal("User 5", p.User5.Name);
                Assert.Equal("User 6", p.User6.Name);
                Assert.Equal("User 7", p.User7.Name);
                Assert.Equal("User 8", p.User8.Name);
                Assert.Equal("User 9", p.User9.Name);
            }
            finally
            {
                connection.Execute("drop table #Users drop table #ReviewBoards");
            }
        }

        [Fact]
        public async Task Issue157_ClosedReaderAsync()
        {
            var args = new { x = 42 };
            const string sql = "select 123 as [A], 'abc' as [B] where @x=42";
            var row = await connection.StreamAsync<SomeType>(new CommandDefinition(
                sql, args, flags: CommandFlags.None)).SingleAsync().ConfigureAwait(false);
            Assert.NotNull(row);
            Assert.Equal(123, row.A);
            Assert.Equal("abc", row.B);

            args = new { x = 5 };
            Assert.False(await connection.StreamAsync<SomeType>(new CommandDefinition(sql, args, flags: CommandFlags.None)).AnyAsync().ConfigureAwait(false));
        }

        [Fact]
        public async Task TestAtEscaping()
        {
            var id = await connection.StreamAsync<int>(@"
                declare @@Name int
                select @@Name = @Id+1
                select @@Name
                ", new Product { Id = 1 }).SingleAsync().ConfigureAwait(false);
            Assert.Equal(2, id);
        }

        [Fact]
        public async Task Issue563_StreamAsyncShouldThrowException()
        {
            try
            {
                var data = await connection.StreamAsync<int>("select 1 union all select 2; RAISERROR('after select', 16, 1);").ToListAsync().ConfigureAwait(false);
                Assert.True(false, "Expected Exception");
            }
            catch (Exception ex) when (ex.GetType().Name == "SqlException" && ex.Message == "after select") { /* swallow only this */ }
        }
    }
}
#endif // NET5_0 || NETCOREAPP3_1
