using System.Linq;
using System.Data;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Data.Common;
using Xunit.Abstractions;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientAsyncTests : AsyncTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientAsyncTests : AsyncTests<MicrosoftSqlClientProvider> { }
#endif

    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientAsyncQueryCacheTests : AsyncQueryCacheTests<SystemSqlClientProvider>
    {
        public SystemSqlClientAsyncQueryCacheTests(ITestOutputHelper log) : base(log) { }
    }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientAsyncQueryCacheTests : AsyncQueryCacheTests<MicrosoftSqlClientProvider>
    {
        public MicrosoftSqlClientAsyncQueryCacheTests(ITestOutputHelper log) : base(log) { }
    }
#endif


    public abstract class AsyncTests<TProvider> : TestBase<TProvider> where TProvider : SqlServerDatabaseProvider
    {
        private DbConnection _marsConnection;

        private DbConnection MarsConnection => _marsConnection ?? (_marsConnection = Provider.GetOpenConnection(true));

        [Fact]
        public async Task TestBasicStringUsageAsync()
        {
            var query = await connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" }).ConfigureAwait(false);
            var arr = query.ToArray();
            Assert.Equal(new[] { "abc", "def" }, arr);
        }

        [Fact]
        public async Task TestBasicStringUsageQueryFirstAsync()
        {
            var str = await connection.QueryFirstAsync<string>(new CommandDefinition("select 'abc' as [Value] union all select @txt", new { txt = "def" })).ConfigureAwait(false);
            Assert.Equal("abc", str);
        }

        [Fact]
        public async Task TestBasicStringUsageQueryFirstAsyncDynamic()
        {
            var str = await connection.QueryFirstAsync("select 'abc' as [Value] union all select @txt", new { txt = "def" }).ConfigureAwait(false);
            Assert.Equal("abc", str.Value);
        }

        [Fact]
        public async Task TestBasicStringUsageQueryFirstOrDefaultAsync()
        {
            var str = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition("select null as [Value] union all select @txt", new { txt = "def" })).ConfigureAwait(false);
            Assert.Null(str);
        }

        [Fact]
        public async Task TestBasicStringUsageQueryFirstOrDefaultAsyncDynamic()
        {
            var str = await connection.QueryFirstOrDefaultAsync("select null as [Value] union all select @txt", new { txt = "def" }).ConfigureAwait(false);
            Assert.Null(str.Value);
        }

        [Fact]
        public async Task TestBasicStringUsageQuerySingleAsyncDynamic()
        {
            var str = await connection.QuerySingleAsync<string>(new CommandDefinition("select 'abc' as [Value]")).ConfigureAwait(false);
            Assert.Equal("abc", str);
        }

        [Fact]
        public async Task TestBasicStringUsageQuerySingleAsync()
        {
            var str = await connection.QuerySingleAsync("select 'abc' as [Value]").ConfigureAwait(false);
            Assert.Equal("abc", str.Value);
        }

        [Fact]
        public async Task TestBasicStringUsageQuerySingleOrDefaultAsync()
        {
            var str = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition("select null as [Value]")).ConfigureAwait(false);
            Assert.Null(str);
        }

        [Fact]
        public async Task TestBasicStringUsageQuerySingleOrDefaultAsyncDynamic()
        {
            var str = await connection.QuerySingleOrDefaultAsync("select null as [Value]").ConfigureAwait(false);
            Assert.Null(str.Value);
        }

        [Fact]
        public async Task TestBasicStringUsageAsyncNonBuffered()
        {
            var query = await connection.QueryAsync<string>(new CommandDefinition("select 'abc' as [Value] union all select @txt", new { txt = "def" }, flags: CommandFlags.None)).ConfigureAwait(false);
            var arr = query.ToArray();
            Assert.Equal(new[] { "abc", "def" }, arr);
        }

        [Fact]
        public void TestLongOperationWithCancellation()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var task = connection.QueryAsync<int>(new CommandDefinition("waitfor delay '00:00:10';select 1", cancellationToken: cancel.Token));
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
                var query = await conn.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" }).ConfigureAwait(false);
                var arr = query.ToArray();
                Assert.Equal(new[] { "abc", "def" }, arr);
            }
        }

        [Fact]
        public async Task TestQueryDynamicAsync()
        {
            var row = (await connection.QueryAsync("select 'abc' as [Value]").ConfigureAwait(false)).Single();
            string value = row.Value;
            Assert.Equal("abc", value);
        }

        [Fact]
        public async Task TestClassWithStringUsageAsync()
        {
            var query = await connection.QueryAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" }).ConfigureAwait(false);
            var arr = query.ToArray();
            Assert.Equal(new[] { "abc", "def" }, arr.Select(x => x.Value));
        }

        [Fact]
        public async Task TestExecuteAsync()
        {
            var val = await connection.ExecuteAsync("declare @foo table(id int not null); insert @foo values(@id);", new { id = 1 }).ConfigureAwait(false);
            Assert.Equal(1, val);
        }

        [Fact]
        public void TestExecuteClosedConnAsyncInner()
        {
            using (var conn = GetClosedConnection())
            {
                var query = conn.ExecuteAsync("declare @foo table(id int not null); insert @foo values(@id);", new { id = 1 });
                var val = query.Result;
                Assert.Equal(1, val);
            }
        }

        [Fact]
        public async Task TestMultiMapWithSplitAsync()
        {
            const string sql = "select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            var productQuery = await connection.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
            {
                prod.Category = cat;
                return prod;
            }).ConfigureAwait(false);

            var product = productQuery.First();
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
            var productQuery = await connection.QueryAsync<Product>(sql, new[] { typeof(Product), typeof(Category) }, (objects) =>
            {
                var prod = (Product)objects[0];
                prod.Category = (Category)objects[1];
                return prod;
            }).ConfigureAwait(false);

            var product = productQuery.First();
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
                var productQuery = await conn.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
                {
                    prod.Category = cat;
                    return prod;
                }).ConfigureAwait(false);

                var product = productQuery.First();
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
                Assert.Equal(1, multi.ReadAsync<int>().Result.Single());
                Assert.Equal(2, multi.ReadAsync<int>().Result.Single());
            }
        }

        [Fact]
        public async Task TestMultiAsyncViaFirstOrDefault()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2; select 3; select 4; select 5").ConfigureAwait(false))
            {
                Assert.Equal(1, multi.ReadFirstOrDefaultAsync<int>().Result);
                Assert.Equal(2, multi.ReadAsync<int>().Result.Single());
                Assert.Equal(3, multi.ReadFirstOrDefaultAsync<int>().Result);
                Assert.Equal(4, multi.ReadAsync<int>().Result.Single());
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
                    Assert.Equal(1, multi.ReadAsync<int>().Result.Single());
                    Assert.Equal(2, multi.ReadAsync<int>().Result.Single());
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
                    Assert.Equal(2, multi.ReadAsync<int>().Result.Single());
                    Assert.Equal(3, multi.ReadFirstOrDefaultAsync<int>().Result);
                    Assert.Equal(4, multi.ReadAsync<int>().Result.Single());
                    Assert.Equal(5, multi.ReadFirstOrDefaultAsync<int>().Result);
                }
            }
        }

        [Fact]
        public async Task ExecuteReaderOpenAsync()
        {
            var dt = new DataTable();
            dt.Load(await connection.ExecuteReaderAsync("select 3 as [three], 4 as [four]").ConfigureAwait(false));
            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal("three", dt.Columns[0].ColumnName);
            Assert.Equal("four", dt.Columns[1].ColumnName);
            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(3, (int)dt.Rows[0][0]);
            Assert.Equal(4, (int)dt.Rows[0][1]);
        }

        [Fact]
        public async Task ExecuteReaderClosedAsync()
        {
            using (var conn = GetClosedConnection())
            {
                var dt = new DataTable();
                dt.Load(await conn.ExecuteReaderAsync("select 3 as [three], 4 as [four]").ConfigureAwait(false));
                Assert.Equal(2, dt.Columns.Count);
                Assert.Equal("three", dt.Columns[0].ColumnName);
                Assert.Equal("four", dt.Columns[1].ColumnName);
                Assert.Equal(1, dt.Rows.Count);
                Assert.Equal(3, (int)dt.Rows[0][0]);
                Assert.Equal(4, (int)dt.Rows[0][1]);
            }
        }

        [Fact]
        public async Task LiteralReplacementOpen()
        {
            await LiteralReplacement(connection).ConfigureAwait(false);
        }

        [Fact]
        public async Task LiteralReplacementClosed()
        {
            using (var conn = GetClosedConnection()) await LiteralReplacement(conn).ConfigureAwait(false);
        }

        private async Task LiteralReplacement(IDbConnection conn)
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
            var count = (await conn.QueryAsync<int>("select count(1) from literal1 where id={=foo}", new { foo = 123 }).ConfigureAwait(false)).Single();
            Assert.Equal(1, count);
            int sum = (await conn.QueryAsync<int>("select sum(id) + sum(foo) from literal1").ConfigureAwait(false)).Single();
            Assert.Equal(sum, 123 + 456 + 1 + 2 + 3 + 4);
        }

        [Fact]
        public async Task LiteralReplacementDynamicOpen()
        {
            await LiteralReplacementDynamic(connection).ConfigureAwait(false);
        }

        [Fact]
        public async Task LiteralReplacementDynamicClosed()
        {
            using (var conn = GetClosedConnection()) await LiteralReplacementDynamic(conn).ConfigureAwait(false);
        }

        private async Task LiteralReplacementDynamic(IDbConnection conn)
        {
            var args = new DynamicParameters();
            args.Add("id", 123);
            try { await conn.ExecuteAsync("drop table literal2").ConfigureAwait(false); }
            catch { /* don't care */ }
            await conn.ExecuteAsync("create table literal2 (id int not null)").ConfigureAwait(false);
            await conn.ExecuteAsync("insert literal2 (id) values ({=id})", args).ConfigureAwait(false);

            args = new DynamicParameters();
            args.Add("foo", 123);
            var count = (await conn.QueryAsync<int>("select count(1) from literal2 where id={=foo}", args).ConfigureAwait(false)).Single();
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
            var count = (await connection.QueryAsync<int>("select count(1) from #literalin where id in {=ids}",
                new { ids = new[] { 1, 3, 4 } }).ConfigureAwait(false)).Single();
            Assert.Equal(2, count);
        }

        [FactLongRunning]
        public async Task RunSequentialVersusParallelAsync()
        {
            var ids = Enumerable.Range(1, 20000).Select(id => new { id }).ToArray();
            await MarsConnection.ExecuteAsync(new CommandDefinition("select @id", ids.Take(5), flags: CommandFlags.None)).ConfigureAwait(false);

            var watch = Stopwatch.StartNew();
            await MarsConnection.ExecuteAsync(new CommandDefinition("select @id", ids, flags: CommandFlags.None)).ConfigureAwait(false);
            watch.Stop();
            Console.WriteLine("No pipeline: {0}ms", watch.ElapsedMilliseconds);

            watch = Stopwatch.StartNew();
            await MarsConnection.ExecuteAsync(new CommandDefinition("select @id", ids, flags: CommandFlags.Pipelined)).ConfigureAwait(false);
            watch.Stop();
            Console.WriteLine("Pipeline: {0}ms", watch.ElapsedMilliseconds);
        }

        [FactLongRunning]
        public void RunSequentialVersusParallelSync()
        {
            var ids = Enumerable.Range(1, 20000).Select(id => new { id }).ToArray();
            MarsConnection.Execute(new CommandDefinition("select @id", ids.Take(5), flags: CommandFlags.None));

            var watch = Stopwatch.StartNew();
            MarsConnection.Execute(new CommandDefinition("select @id", ids, flags: CommandFlags.None));
            watch.Stop();
            Console.WriteLine("No pipeline: {0}ms", watch.ElapsedMilliseconds);

            watch = Stopwatch.StartNew();
            MarsConnection.Execute(new CommandDefinition("select @id", ids, flags: CommandFlags.Pipelined));
            watch.Stop();
            Console.WriteLine("Pipeline: {0}ms", watch.ElapsedMilliseconds);
        }

        private class BasicType
        {
            public string Value { get; set; }
        }

        [Fact]
        public async Task TypeBasedViaTypeAsync()
        {
            Type type = Common.GetSomeType();

            dynamic actual = (await MarsConnection.QueryAsync(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).ConfigureAwait(false)).FirstOrDefault();
            Assert.Equal(((object)actual).GetType(), type);
            int a = actual.A;
            string b = actual.B;
            Assert.Equal(123, a);
            Assert.Equal("abc", b);
        }

        [Fact]
        public async Task TypeBasedViaTypeAsyncFirstOrDefault()
        {
            Type type = Common.GetSomeType();

            dynamic actual = await MarsConnection.QueryFirstOrDefaultAsync(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).ConfigureAwait(false);
            Assert.Equal(((object)actual).GetType(), type);
            int a = actual.A;
            string b = actual.B;
            Assert.Equal(123, a);
            Assert.Equal("abc", b);
        }

        [Fact]
        public async Task Issue22_ExecuteScalarAsync()
        {
            int i = await connection.ExecuteScalarAsync<int>("select 123").ConfigureAwait(false);
            Assert.Equal(123, i);

            i = await connection.ExecuteScalarAsync<int>("select cast(123 as bigint)").ConfigureAwait(false);
            Assert.Equal(123, i);

            long j = await connection.ExecuteScalarAsync<long>("select 123").ConfigureAwait(false);
            Assert.Equal(123L, j);

            j = await connection.ExecuteScalarAsync<long>("select cast(123 as bigint)").ConfigureAwait(false);
            Assert.Equal(123L, j);

            int? k = await connection.ExecuteScalarAsync<int?>("select @i", new { i = default(int?) }).ConfigureAwait(false);
            Assert.Null(k);
        }

        [Fact]
        public async Task Issue346_QueryAsyncConvert()
        {
            int i = (await connection.QueryAsync<int>("Select Cast(123 as bigint)").ConfigureAwait(false)).First();
            Assert.Equal(123, i);
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressionsAsync()
        {
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2, Index = new Index() } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);
                p.Output(bob, b => b.Address.Index.Id);

                await connection.ExecuteAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
SET @AddressIndexId = '01088'", p).ConfigureAwait(false);

                Assert.Equal("grillmaster", bob.Occupation);
                Assert.Equal(2, bob.PersonId);
                Assert.Equal(1, bob.NumberOfLegs);
                Assert.Equal("bobs burgers", bob.Address.Name);
                Assert.Equal(2, bob.Address.PersonId);
                Assert.Equal("01088", bob.Address.Index.Id);
            }
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressions_ScalarAsync()
        {
            var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

            var p = new DynamicParameters(bob);
            p.Output(bob, b => b.PersonId);
            p.Output(bob, b => b.Occupation);
            p.Output(bob, b => b.NumberOfLegs);
            p.Output(bob, b => b.Address.Name);
            p.Output(bob, b => b.Address.PersonId);

            var result = (int)(await connection.ExecuteScalarAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p).ConfigureAwait(false));

            Assert.Equal("grillmaster", bob.Occupation);
            Assert.Equal(2, bob.PersonId);
            Assert.Equal(1, bob.NumberOfLegs);
            Assert.Equal("bobs burgers", bob.Address.Name);
            Assert.Equal(2, bob.Address.PersonId);
            Assert.Equal(42, result);
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

            var result = (await connection.QueryAsync<int>(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p).ConfigureAwait(false)).Single();

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

            var result = (await connection.QueryAsync<int>(new CommandDefinition(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, flags: CommandFlags.Buffered)).ConfigureAwait(false)).Single();

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

            var result = (await connection.QueryAsync<int>(new CommandDefinition(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, flags: CommandFlags.None)).ConfigureAwait(false)).Single();

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
                x = multi.ReadAsync<int>().Result.Single();
                y = multi.ReadAsync<int>().Result.Single();
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
            var data0 = (await connection.QueryAsync<AsyncFoo0>("select 1 as [Id] where 1 = 0").ConfigureAwait(false)).ToList();
            Assert.Empty(data0);

            var data1 = (await connection.QueryAsync<AsyncFoo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ConfigureAwait(false)).ToList();
            Assert.Empty(data1);

            var data2 = (await connection.QueryAsync<AsyncFoo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ConfigureAwait(false)).ToList();
            Assert.Empty(data2);

            data0 = (await connection.QueryAsync<AsyncFoo0>("select 1 as [Id] where 1 = 0").ConfigureAwait(false)).ToList();
            Assert.Empty(data0);

            data1 = (await connection.QueryAsync<AsyncFoo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ConfigureAwait(false)).ToList();
            Assert.Empty(data1);

            data2 = (await connection.QueryAsync<AsyncFoo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ConfigureAwait(false)).ToList();
            Assert.Empty(data2);
        }

        private class AsyncFoo0 { public int Id { get; set; } }

        private class AsyncFoo1 { public int Id { get; set; } }

        private class AsyncFoo2 { public int Id { get; set; } }

        [Fact]
        public async Task TestSchemaChangedViaFirstOrDefaultAsync()
        {
            await connection.ExecuteAsync("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')").ConfigureAwait(false);
            try
            {
                var d = await connection.QueryFirstOrDefaultAsync<Dog>("select * from #dog").ConfigureAwait(false);
                Assert.Equal("Alf", d.Name);
                Assert.Equal(1, d.Age);
                connection.Execute("alter table #dog drop column Name");
                d = await connection.QueryFirstOrDefaultAsync<Dog>("select * from #dog").ConfigureAwait(false);
                Assert.Null(d.Name);
                Assert.Equal(1, d.Age);
            }
            finally
            {
                await connection.ExecuteAsync("drop table #dog").ConfigureAwait(false);
            }
        }

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

                var data = (await connection.QueryAsync<ReviewBoard>(sql, types, mapper).ConfigureAwait(false)).ToList();

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
            var row = (await connection.QueryAsync<SomeType>(new CommandDefinition(
                sql, args, flags: CommandFlags.None)).ConfigureAwait(false)).Single();
            Assert.NotNull(row);
            Assert.Equal(123, row.A);
            Assert.Equal("abc", row.B);

            args = new { x = 5 };
            Assert.False((await connection.QueryAsync<SomeType>(new CommandDefinition(sql, args, flags: CommandFlags.None)).ConfigureAwait(false)).Any());
        }

        [Fact]
        public async Task TestAtEscaping()
        {
            var id = (await connection.QueryAsync<int>(@"
                declare @@Name int
                select @@Name = @Id+1
                select @@Name
                ", new Product { Id = 1 }).ConfigureAwait(false)).Single();
            Assert.Equal(2, id);
        }

        [Fact]
        public async Task Issue1281_DataReaderOutOfOrderAsync()
        {
            using (var reader = await connection.ExecuteReaderAsync("Select 0, 1, 2").ConfigureAwait(false))
            {
                Assert.True(reader.Read());
                Assert.Equal(2, reader.GetInt32(2));
                Assert.Equal(0, reader.GetInt32(0));
                Assert.Equal(1, reader.GetInt32(1));
                Assert.False(reader.Read());
            }
        }

        [Fact]
        public async Task Issue563_QueryAsyncShouldThrowException()
        {
            try
            {
                var data = (await connection.QueryAsync<int>("select 1 union all select 2; RAISERROR('after select', 16, 1);").ConfigureAwait(false)).ToList();
                Assert.True(false, "Expected Exception");
            }
            catch (Exception ex) when (ex.GetType().Name == "SqlException" && ex.Message == "after select") { /* swallow only this */ }
        }
    }

    [Collection(NonParallelDefinition.Name)]
    public abstract class AsyncQueryCacheTests<TProvider> : TestBase<TProvider> where TProvider : SqlServerDatabaseProvider
    {
        private readonly ITestOutputHelper _log;
        public AsyncQueryCacheTests(ITestOutputHelper log) => _log = log;
        private DbConnection _marsConnection;
        private DbConnection MarsConnection => _marsConnection ?? (_marsConnection = Provider.GetOpenConnection(true));

        public override void Dispose()
        {
            _marsConnection?.Dispose();
            _marsConnection = null;
            base.Dispose();
        }

        [Fact]
        public void AssertNoCacheWorksForQueryMultiple()
        {
            const int a = 123, b = 456;
            var cmdDef = new CommandDefinition("select @a; select @b;", new
            {
                a,
                b
            }, commandType: CommandType.Text, flags: CommandFlags.NoCache);

            int c, d;
            SqlMapper.PurgeQueryCache();
            int before = SqlMapper.GetCachedSQLCount();
            using (var multi = MarsConnection.QueryMultiple(cmdDef))
            {
                c = multi.Read<int>().Single();
                d = multi.Read<int>().Single();
            }
            int after = SqlMapper.GetCachedSQLCount();
            _log?.WriteLine($"before: {before}; after: {after}");
            // too brittle in concurrent tests to assert
            // Assert.Equal(0, before);
            // Assert.Equal(0, after);
            Assert.Equal(123, c);
            Assert.Equal(456, d);
        }
    }
}
