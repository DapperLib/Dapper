#if ASYNC
using System.Linq;
using System.Data;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Data.SqlClient;
using Xunit;

namespace Dapper.Tests
{
    public partial class TestSuite
    {
        [Fact]
        public async Task TestBasicStringUsageAsync()
        {
            var query = await connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
            var arr = query.ToArray();
            arr.IsSequenceEqualTo(new[] { "abc", "def" });
        }

        [Fact]
        public async Task TestBasicStringUsageQueryFirstAsync()
        {
            var str = await connection.QueryFirstAsync<string>(new CommandDefinition("select 'abc' as [Value] union all select @txt", new {txt = "def"}));
            str.IsEqualTo("abc");
        }

        [Fact]
        public async Task TestBasicStringUsageQueryFirstOrDefaultAsync()
        {
            var str = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition("select null as [Value] union all select @txt", new {txt = "def"}));
            str.IsNull();
        }

        [Fact]
        public async Task TestBasicStringUsageQuerySingleAsync()
        {
            var str = await connection.QuerySingleAsync<string>(new CommandDefinition("select 'abc' as [Value]"));
            str.IsEqualTo("abc");
        }

        [Fact]
        public async Task TestBasicStringUsageQuerySingleOrDefaultAsync()
        {
            var str = await connection.QuerySingleAsync<string>(new CommandDefinition("select null as [Value]"));
            str.IsNull();
        }

        [Fact]
        public async Task TestBasicStringUsageAsyncNonBuffered()
        {
            var query = await connection.QueryAsync<string>(new CommandDefinition("select 'abc' as [Value] union all select @txt", new { txt = "def" }, flags: CommandFlags.None));
            var arr = query.ToArray();
            arr.IsSequenceEqualTo(new[] { "abc", "def" });
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
                (agg.InnerException is SqlException).IsTrue();
            }
        }

        [Fact]
        public async Task TestBasicStringUsageClosedAsync()
        {
            var query = await connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
            var arr = query.ToArray();
            arr.IsSequenceEqualTo(new[] { "abc", "def" });
        }

        [Fact]
        public async Task TestQueryDynamicAsync()
        {
            var row = (await connection.QueryAsync("select 'abc' as [Value]")).Single();
            string value = row.Value;
            value.IsEqualTo("abc");
        }

        [Fact]
        public async Task TestClassWithStringUsageAsync()
        {
            var query = await connection.QueryAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
            var arr = query.ToArray();
            arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });
        }

        [Fact]
        public async Task TestExecuteAsync()
        {
            var val = await connection.ExecuteAsync("declare @foo table(id int not null); insert @foo values(@id);", new { id = 1 });
            val.Equals(1);
        }

        [Fact]
        public void TestExecuteClosedConnAsync()
        {
            var query = connection.ExecuteAsync("declare @foo table(id int not null); insert @foo values(@id);", new { id = 1 });
            var val = query.Result;
            val.Equals(1);
        }

        [Fact]
        public async Task TestMultiMapWithSplitAsync()
        {
            const string sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            var productQuery = await connection.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
            {
                prod.Category = cat;
                return prod;
            });

            var product = productQuery.First();
            // assertions
            product.Id.IsEqualTo(1);
            product.Name.IsEqualTo("abc");
            product.Category.Id.IsEqualTo(2);
            product.Category.Name.IsEqualTo("def");
        }

        [Fact]
        public async Task TestMultiMapArbitraryWithSplitAsync()
        {
            const string sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            var productQuery = await connection.QueryAsync<Product>(sql, new[] { typeof(Product), typeof(Category) }, (objects) => {
                var prod = (Product)objects[0];
                prod.Category = (Category)objects[1];
                return prod;
            });

            var product = productQuery.First();
            // assertions
            product.Id.IsEqualTo(1);
            product.Name.IsEqualTo("abc");
            product.Category.Id.IsEqualTo(2);
            product.Category.Name.IsEqualTo("def");
        }

        [Fact]
        public async Task TestMultiMapWithSplitClosedConnAsync()
        {
            var sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            using (var conn = GetClosedConnection())
            {
                var productQuery = await conn.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
                {
                    prod.Category = cat;
                    return prod;
                });

                var product = productQuery.First();
                // assertions
                product.Id.IsEqualTo(1);
                product.Name.IsEqualTo("abc");
                product.Category.Id.IsEqualTo(2);
                product.Category.Name.IsEqualTo("def");
            }
        }

        [Fact]
        public async Task TestMultiAsync()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2"))
            {
                multi.ReadAsync<int>().Result.Single().IsEqualTo(1);
                multi.ReadAsync<int>().Result.Single().IsEqualTo(2);
            }
        }

        [Fact]
        public async Task TestMultiAsyncViaFirstOrDefault()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2; select 3; select 4; select 5"))
            {
                multi.ReadFirstOrDefaultAsync<int>().Result.IsEqualTo(1);
                multi.ReadAsync<int>().Result.Single().IsEqualTo(2);
                multi.ReadFirstOrDefaultAsync<int>().Result.IsEqualTo(3);
                multi.ReadAsync<int>().Result.Single().IsEqualTo(4);
                multi.ReadFirstOrDefaultAsync<int>().Result.IsEqualTo(5);
            }
        }

        [Fact]
        public async Task TestMultiClosedConnAsync()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2"))
            {
                multi.ReadAsync<int>().Result.Single().IsEqualTo(1);
                multi.ReadAsync<int>().Result.Single().IsEqualTo(2);
            }
        }

        [Fact]
        public async Task TestMultiClosedConnAsyncViaFirstOrDefault()
        {
            using (SqlMapper.GridReader multi = await connection.QueryMultipleAsync("select 1; select 2; select 3; select 4; select 5;"))
            {
                multi.ReadFirstOrDefaultAsync<int>().Result.IsEqualTo(1);
                multi.ReadAsync<int>().Result.Single().IsEqualTo(2);
                multi.ReadFirstOrDefaultAsync<int>().Result.IsEqualTo(3);
                multi.ReadAsync<int>().Result.Single().IsEqualTo(4);
                multi.ReadFirstOrDefaultAsync<int>().Result.IsEqualTo(5);
            }
        }

#if !COREFX
        [Fact]
        public async Task ExecuteReaderOpenAsync()
        {
            var dt = new DataTable();
            dt.Load(await connection.ExecuteReaderAsync("select 3 as [three], 4 as [four]"));
            dt.Columns.Count.IsEqualTo(2);
            dt.Columns[0].ColumnName.IsEqualTo("three");
            dt.Columns[1].ColumnName.IsEqualTo("four");
            dt.Rows.Count.IsEqualTo(1);
            ((int)dt.Rows[0][0]).IsEqualTo(3);
            ((int)dt.Rows[0][1]).IsEqualTo(4);
        }
        
        [Fact]
        public async Task ExecuteReaderClosedAsync()
        {
            using (var conn = GetClosedConnection())
            {
                var dt = new DataTable();
                dt.Load(await conn.ExecuteReaderAsync("select 3 as [three], 4 as [four]"));
                dt.Columns.Count.IsEqualTo(2);
                dt.Columns[0].ColumnName.IsEqualTo("three");
                dt.Columns[1].ColumnName.IsEqualTo("four");
                dt.Rows.Count.IsEqualTo(1);
                ((int)dt.Rows[0][0]).IsEqualTo(3);
                ((int)dt.Rows[0][1]).IsEqualTo(4);
            }
        }
#endif

        [Fact]
        public async Task LiteralReplacementOpen()
        {
            await LiteralReplacement(connection);
        }
        [Fact]
        public async Task LiteralReplacementClosed()
        {
            using (var conn = GetClosedConnection()) await LiteralReplacement(conn);
        }
        private async Task LiteralReplacement(IDbConnection conn)
        {
            try
            {
                await conn.ExecuteAsync("drop table literal1");
            } catch { }
            await conn.ExecuteAsync("create table literal1 (id int not null, foo int not null)");
            await conn.ExecuteAsync("insert literal1 (id,foo) values ({=id}, @foo)", new { id = 123, foo = 456 });
            var rows = new[] { new { id = 1, foo = 2 }, new { id = 3, foo = 4 } };
            await conn.ExecuteAsync("insert literal1 (id,foo) values ({=id}, @foo)", rows);
            var count = (await conn.QueryAsync<int>("select count(1) from literal1 where id={=foo}", new { foo = 123 })).Single();
            count.IsEqualTo(1);
            int sum = (await conn.QueryAsync<int>("select sum(id) + sum(foo) from literal1")).Single();
            sum.IsEqualTo(123 + 456 + 1 + 2 + 3 + 4);
        }

        [Fact]
        public async Task LiteralReplacementDynamicOpen()
        {
            await LiteralReplacementDynamic(connection);
        }
        [Fact]
        public async Task LiteralReplacementDynamicClosed()
        {
            using (var conn = GetClosedConnection()) await LiteralReplacementDynamic(conn);
        }
        private async Task LiteralReplacementDynamic(IDbConnection conn)
        {
            var args = new DynamicParameters();
            args.Add("id", 123);
            try { await conn.ExecuteAsync("drop table literal2"); } catch { }
            await conn.ExecuteAsync("create table literal2 (id int not null)");
            await conn.ExecuteAsync("insert literal2 (id) values ({=id})", args);

            args = new DynamicParameters();
            args.Add("foo", 123);
            var count = (await conn.QueryAsync<int>("select count(1) from literal2 where id={=foo}", args)).Single();
            count.IsEqualTo(1);
        }

        [Fact]
        public async Task LiteralInAsync()
        {
            await connection.ExecuteAsync("create table #literalin(id int not null);");
            await connection.ExecuteAsync("insert #literalin (id) values (@id)", new[] {
                new { id = 1 },
                new { id = 2 },
                new { id = 3 },
            });
            var count = (await connection.QueryAsync<int>("select count(1) from #literalin where id in {=ids}",
                new { ids = new[] { 1, 3, 4 } })).Single();
            count.IsEqualTo(2);
        }

        [Fact]
        public async Task RunSequentialVersusParallelAsync()
        {
            var ids = Enumerable.Range(1, 20000).Select(id => new { id }).ToArray();
            await marsConnection.ExecuteAsync(new CommandDefinition("select @id", ids.Take(5), flags: CommandFlags.None));

            var watch = Stopwatch.StartNew();
            await marsConnection.ExecuteAsync(new CommandDefinition("select @id", ids, flags: CommandFlags.None));
            watch.Stop();
            Console.WriteLine("No pipeline: {0}ms", watch.ElapsedMilliseconds);

            watch = Stopwatch.StartNew();
            await marsConnection.ExecuteAsync(new CommandDefinition("select @id", ids, flags: CommandFlags.Pipelined));
            watch.Stop();
            Console.WriteLine("Pipeline: {0}ms", watch.ElapsedMilliseconds);
        }

        [Fact]
        public void RunSequentialVersusParallelSync()
        {
            var ids = Enumerable.Range(1, 20000).Select(id => new { id }).ToArray();
            marsConnection.Execute(new CommandDefinition("select @id", ids.Take(5), flags: CommandFlags.None));

            var watch = Stopwatch.StartNew();
            marsConnection.Execute(new CommandDefinition("select @id", ids, flags: CommandFlags.None));
            watch.Stop();
            Console.WriteLine("No pipeline: {0}ms", watch.ElapsedMilliseconds);

            watch = Stopwatch.StartNew();
            marsConnection.Execute(new CommandDefinition("select @id", ids, flags: CommandFlags.Pipelined));
            watch.Stop();
            Console.WriteLine("Pipeline: {0}ms", watch.ElapsedMilliseconds);
        }

        [Fact]
        public void AssertNoCacheWorksForQueryMultiple()
        {
            int a = 123, b = 456;
            var cmdDef = new CommandDefinition(@"select @a; select @b;", new
            {
                a, b
            }, commandType: CommandType.Text, flags: CommandFlags.NoCache);

            int c, d;
            SqlMapper.PurgeQueryCache();
            int before = SqlMapper.GetCachedSQLCount();
            using (var multi = marsConnection.QueryMultiple(cmdDef))
            {
                c = multi.Read<int>().Single();
                d = multi.Read<int>().Single();
            }
            int after = SqlMapper.GetCachedSQLCount();
            before.IsEqualTo(0);
            after.IsEqualTo(0);
            c.IsEqualTo(123);
            d.IsEqualTo(456);
        }
 
        class BasicType
        {
            public string Value { get; set; }
        }

        [Fact]
        public async Task TypeBasedViaTypeAsync()
        {
            Type type = GetSomeType();
            
            dynamic actual = (await marsConnection.QueryAsync(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" })).FirstOrDefault();
            ((object)actual).GetType().IsEqualTo(type);
            int a = actual.A;
            string b = actual.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");
        }

        [Fact]
        public async Task TypeBasedViaTypeAsyncFirstOrDefault()
        {
            Type type = GetSomeType();

            dynamic actual = (await marsConnection.QueryFirstOrDefaultAsync(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }));
            ((object)actual).GetType().IsEqualTo(type);
            int a = actual.A;
            string b = actual.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");
        }

        [Fact]
        public async Task Issue22_ExecuteScalarAsync()
        {
            int i = await connection.ExecuteScalarAsync<int>("select 123");
            i.IsEqualTo(123);

            i = await connection.ExecuteScalarAsync<int>("select cast(123 as bigint)");
            i.IsEqualTo(123);

            long j = await connection.ExecuteScalarAsync<long>("select 123");
            j.IsEqualTo(123L);

            j = await connection.ExecuteScalarAsync<long>("select cast(123 as bigint)");
            j.IsEqualTo(123L);

            int? k = await connection.ExecuteScalarAsync<int?>("select @i", new { i = default(int?) });
            k.IsNull();
        }

        [Fact]
        public async Task Issue346_QueryAsyncConvert()
        {
            int i = (await connection.QueryAsync<int>("Select Cast(123 as bigint)")).First();
            i.IsEqualTo(123);
        }

        [Fact]
        public async Task TestSupportForDynamicParametersOutputExpressionsAsync()
        {
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                await connection.ExecuteAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId", p);

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
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
select 42", p));

            bob.Occupation.IsEqualTo("grillmaster");
            bob.PersonId.IsEqualTo(2);
            bob.NumberOfLegs.IsEqualTo(1);
            bob.Address.Name.IsEqualTo("bobs burgers");
            bob.Address.PersonId.IsEqualTo(2);
            result.IsEqualTo(42);
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
select 42", p)).Single();

            bob.Occupation.IsEqualTo("grillmaster");
            bob.PersonId.IsEqualTo(2);
            bob.NumberOfLegs.IsEqualTo(1);
            bob.Address.Name.IsEqualTo("bobs burgers");
            bob.Address.PersonId.IsEqualTo(2);
            result.IsEqualTo(42);
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
select 42", p, flags: CommandFlags.Buffered))).Single();

            bob.Occupation.IsEqualTo("grillmaster");
            bob.PersonId.IsEqualTo(2);
            bob.NumberOfLegs.IsEqualTo(1);
            bob.Address.Name.IsEqualTo("bobs burgers");
            bob.Address.PersonId.IsEqualTo(2);
            result.IsEqualTo(42);
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
select 42", p, flags: CommandFlags.None))).Single();

            bob.Occupation.IsEqualTo("grillmaster");
            bob.PersonId.IsEqualTo(2);
            bob.NumberOfLegs.IsEqualTo(1);
            bob.Address.Name.IsEqualTo("bobs burgers");
            bob.Address.PersonId.IsEqualTo(2);
            result.IsEqualTo(42);
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
SET @AddressPersonId = @PersonId", p))
            {
                x = multi.ReadAsync<int>().Result.Single();
                y = multi.ReadAsync<int>().Result.Single();
            }

            bob.Occupation.IsEqualTo("grillmaster");
            bob.PersonId.IsEqualTo(2);
            bob.NumberOfLegs.IsEqualTo(1);
            bob.Address.Name.IsEqualTo("bobs burgers");
            bob.Address.PersonId.IsEqualTo(2);
            x.IsEqualTo(42);
            y.IsEqualTo(17);
        }

        [Fact]
        public async Task TestSubsequentQueriesSuccessAsync()
        {
            var data0 = (await connection.QueryAsync<AsyncFoo0>("select 1 as [Id] where 1 = 0")).ToList();
            data0.Count.IsEqualTo(0);

            var data1 = (await connection.QueryAsync<AsyncFoo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered))).ToList();
            data1.Count.IsEqualTo(0);

            var data2 = (await connection.QueryAsync<AsyncFoo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None))).ToList();
            data2.Count.IsEqualTo(0);

            data0 = (await connection.QueryAsync<AsyncFoo0>("select 1 as [Id] where 1 = 0")).ToList();
            data0.Count.IsEqualTo(0);

            data1 = (await connection.QueryAsync<AsyncFoo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered))).ToList();
            data1.Count.IsEqualTo(0);

            data2 = (await connection.QueryAsync<AsyncFoo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None))).ToList();
            data2.Count.IsEqualTo(0);
        }
        class AsyncFoo0 { public int Id { get; set; } }
        class AsyncFoo1 { public int Id { get; set; } }
        class AsyncFoo2 { public int Id { get; set; } }

        [Fact]
        public async Task TestSchemaChangedViaFirstOrDefaultAsync()
        {
            await connection.ExecuteAsync("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')");
            try
            {
                var d = await connection.QueryFirstOrDefaultAsync<Dog>("select * from #dog");
                d.Name.IsEqualTo("Alf");
                d.Age.IsEqualTo(1);
                connection.Execute("alter table #dog drop column Name");
                d = await connection.QueryFirstOrDefaultAsync<Dog>("select * from #dog");
                d.Name.IsNull();
                d.Age.IsEqualTo(1);
            }
            finally
            {
                await connection.ExecuteAsync("drop table #dog");
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
            await connection.ExecuteAsync(createSql);
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

                var data = (await connection.QueryAsync<ReviewBoard>(sql, types, mapper)).ToList();

                var p = data.First();
                p.Id.IsEqualTo(1);
                p.Name.IsEqualTo("Review Board 1");
                p.User1.Id.IsEqualTo(1);
                p.User2.Id.IsEqualTo(2);
                p.User3.Id.IsEqualTo(3);
                p.User4.Id.IsEqualTo(4);
                p.User5.Id.IsEqualTo(5);
                p.User6.Id.IsEqualTo(6);
                p.User7.Id.IsEqualTo(7);
                p.User8.Id.IsEqualTo(8);
                p.User9.Id.IsEqualTo(9);
                p.User1.Name.IsEqualTo("User 1");
                p.User2.Name.IsEqualTo("User 2");
                p.User3.Name.IsEqualTo("User 3");
                p.User4.Name.IsEqualTo("User 4");
                p.User5.Name.IsEqualTo("User 5");
                p.User6.Name.IsEqualTo("User 6");
                p.User7.Name.IsEqualTo("User 7");
                p.User8.Name.IsEqualTo("User 8");
                p.User9.Name.IsEqualTo("User 9");
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
            const string sql = @"select 123 as [A], 'abc' as [B] where @x=42";
            var row = (await connection.QueryAsync<SomeType>(new CommandDefinition(
                sql, args, flags:CommandFlags.None))).Single();
            row.IsNotNull();
            row.A.IsEqualTo(123);
            row.B.IsEqualTo("abc");

            args = new { x = 5 };
            (await connection.QueryAsync<SomeType>(new CommandDefinition(
                sql, args, flags: CommandFlags.None))).Any().IsFalse();
        }

        [Fact]
        public async Task TestAtEscaping()
        {
            var id = (await connection.QueryAsync<int>(@"
                declare @@Name int
                select @@Name = @Id+1
                select @@Name
                ", new Product { Id = 1 })).Single();
            id.IsEqualTo(2);
        }

        [Fact]
        public async Task Issue1281_DataReaderOutOfOrderAsync()
        {
            using (var reader = await connection.ExecuteReaderAsync("Select 0, 1, 2"))
            {
                reader.Read().IsTrue();
                reader.GetInt32(2).IsEqualTo(2);
                reader.GetInt32(0).IsEqualTo(0);
                reader.GetInt32(1).IsEqualTo(1);
                reader.Read().IsFalse();
            }
        }

        [Fact]
        public async Task Issue563_QueryAsyncShouldThrowException()
        {
            try
            {
                var data = (await connection.QueryAsync<int>("select 1 union all select 2; RAISERROR('after select', 16, 1);")).ToList();
                Assert.Fail();
            }
            catch (SqlException ex) when (ex.Message == "after select") { }
        }
    }
}
#endif