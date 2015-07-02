#if ASYNC
using System.Linq;
using Dapper;
using SqlMapper;
using System.Data;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Data.SqlClient;

#if DNXCORE50
using IDbCommand = global::System.Data.Common.DbCommand;
using IDbDataParameter = global::System.Data.Common.DbParameter;
using IDbConnection = global::System.Data.Common.DbConnection;
using IDbTransaction = global::System.Data.Common.DbTransaction;
using IDataReader = global::System.Data.Common.DbDataReader;
#endif

namespace DapperTests_NET45
{
    public class Tests
    {
        public void TestBasicStringUsageAsync()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.IsSequenceEqualTo(new[] { "abc", "def" });
            }
        }
        public void TestBasicStringUsageAsyncNonBuffered()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<string>(new CommandDefinition("select 'abc' as [Value] union all select @txt", new { txt = "def" }, flags: CommandFlags.None));
                var arr = query.Result.ToArray();
                arr.IsSequenceEqualTo(new[] { "abc", "def" });
            }
        }

        public void TestLongOperationWithCancellation()
        {
            using (var connection = Program.GetClosedConnection())
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
        }

        public void TestBasicStringUsageClosedAsync()
        {
            using (var connection = Program.GetClosedConnection())
            {
                var query = connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.IsSequenceEqualTo(new[] { "abc", "def" });
            }
        }

        public void TestQueryDynamicAsync()
        {
            using (var connection = Program.GetClosedConnection())
            {
                var row = connection.QueryAsync("select 'abc' as [Value]").Result.Single();
                string value = row.Value;
                value.IsEqualTo("abc");
            }
        }

        public void TestClassWithStringUsageAsync()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });
            }
        }

        public void TestExecuteAsync()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.ExecuteAsync("declare @foo table(id int not null); insert @foo values(@id);", new { id = 1 });
                var val = query.Result;
                val.Equals(1);
            }
        }
        public void TestExecuteClosedConnAsync()
        {
            using (var connection = Program.GetClosedConnection())
            {
                var query = connection.ExecuteAsync("declare @foo table(id int not null); insert @foo values(@id);", new { id = 1 });
                var val = query.Result;
                val.Equals(1);
            }
        }

        public void TestMultiMapWithSplitAsync()
        {
            var sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            using (var connection = Program.GetOpenConnection())
            {
                var productQuery = connection.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
                {
                    prod.Category = cat;
                    return prod;
                });

                var product = productQuery.Result.First();
                // assertions
                product.Id.IsEqualTo(1);
                product.Name.IsEqualTo("abc");
                product.Category.Id.IsEqualTo(2);
                product.Category.Name.IsEqualTo("def");
            }
        }

        public void TestMultiMapArbitraryWithSplitAsync() {
            var sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            using (var connection = Program.GetOpenConnection()) {
                var productQuery = connection.QueryAsync<Product>(sql, new[] { typeof(Product), typeof(Category) }, (objects) => {
                    var prod = (Product)objects[0];
                    prod.Category = (Category)objects[1];
                    return prod;
                });

                var product = productQuery.Result.First();
                // assertions
                product.Id.IsEqualTo(1);
                product.Name.IsEqualTo("abc");
                product.Category.Id.IsEqualTo(2);
                product.Category.Name.IsEqualTo("def");
            }
        }

        public void TestMultiMapWithSplitClosedConnAsync()
        {
            var sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            using (var connection = Program.GetClosedConnection())
            {
                var productQuery = connection.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
                {
                    prod.Category = cat;
                    return prod;
                });

                var product = productQuery.Result.First();
                // assertions
                product.Id.IsEqualTo(1);
                product.Name.IsEqualTo("abc");
                product.Category.Id.IsEqualTo(2);
                product.Category.Name.IsEqualTo("def");
            }
        }

        public void TestMultiAsync()
        {
            using (var conn = Program.GetOpenConnection())
            {
                using (Dapper.SqlMapper.GridReader multi = conn.QueryMultipleAsync("select 1; select 2").Result)
                {
                    multi.ReadAsync<int>().Result.Single().IsEqualTo(1);
                    multi.ReadAsync<int>().Result.Single().IsEqualTo(2);
                }
            }
        }
        public void TestMultiClosedConnAsync()
        {
            using (var conn = Program.GetClosedConnection())
            {
                using (Dapper.SqlMapper.GridReader multi = conn.QueryMultipleAsync("select 1; select 2").Result)
                {
                    multi.ReadAsync<int>().Result.Single().IsEqualTo(1);
                    multi.ReadAsync<int>().Result.Single().IsEqualTo(2);
                }
            }
        }
#if EXTERNALS
        public void ExecuteReaderOpenAsync()
        {
            using (var conn = Program.GetOpenConnection())
            {
                var dt = new DataTable();
                dt.Load(conn.ExecuteReaderAsync("select 3 as [three], 4 as [four]").Result);
                dt.Columns.Count.IsEqualTo(2);
                dt.Columns[0].ColumnName.IsEqualTo("three");
                dt.Columns[1].ColumnName.IsEqualTo("four");
                dt.Rows.Count.IsEqualTo(1);
                ((int)dt.Rows[0][0]).IsEqualTo(3);
                ((int)dt.Rows[0][1]).IsEqualTo(4);
            }
        }
        public void ExecuteReaderClosedAsync()
        {
            using (var conn = Program.GetClosedConnection())
            {
                var dt = new DataTable();
                dt.Load(conn.ExecuteReaderAsync("select 3 as [three], 4 as [four]").Result);
                dt.Columns.Count.IsEqualTo(2);
                dt.Columns[0].ColumnName.IsEqualTo("three");
                dt.Columns[1].ColumnName.IsEqualTo("four");
                dt.Rows.Count.IsEqualTo(1);
                ((int)dt.Rows[0][0]).IsEqualTo(3);
                ((int)dt.Rows[0][1]).IsEqualTo(4);
            }
        }
#endif

        public void LiteralReplacementOpen()
        {
            using (var conn = Program.GetOpenConnection()) LiteralReplacement(conn);
        }
        public void LiteralReplacementClosed()
        {
            using (var conn = Program.GetClosedConnection()) LiteralReplacement(conn);
        }
        private void LiteralReplacement(IDbConnection connection)
        {
            try { connection.ExecuteAsync("drop table literal1").Wait(); } catch { }
            connection.ExecuteAsync("create table literal1 (id int not null, foo int not null)").Wait();
            connection.ExecuteAsync("insert literal1 (id,foo) values ({=id}, @foo)", new { id = 123, foo = 456 }).Wait();
            var rows = new[] { new { id = 1, foo = 2 }, new { id = 3, foo = 4 } };
            connection.ExecuteAsync("insert literal1 (id,foo) values ({=id}, @foo)", rows).Wait();
            var count = connection.QueryAsync<int>("select count(1) from literal1 where id={=foo}", new { foo = 123 }).Result.Single();
            count.IsEqualTo(1);
            int sum = connection.QueryAsync<int>("select sum(id) + sum(foo) from literal1").Result.Single();
            sum.IsEqualTo(123 + 456 + 1 + 2 + 3 + 4);
        }

        public void LiteralReplacementDynamicOpen()
        {
            using (var conn = Program.GetOpenConnection()) LiteralReplacementDynamic(conn);
        }
        public void LiteralReplacementDynamicClosed()
        {
            using (var conn = Program.GetClosedConnection()) LiteralReplacementDynamic(conn);
        }
        private void LiteralReplacementDynamic(IDbConnection connection)
        {
            var args = new DynamicParameters();
            args.Add("id", 123);
            try { connection.ExecuteAsync("drop table literal2").Wait(); } catch { }
            connection.ExecuteAsync("create table literal2 (id int not null)").Wait();
            connection.ExecuteAsync("insert literal2 (id) values ({=id})", args).Wait();

            args = new DynamicParameters();
            args.Add("foo", 123);
            var count = connection.QueryAsync<int>("select count(1) from literal2 where id={=foo}", args).Result.Single();
            count.IsEqualTo(1);
        }

        public void LiteralIn()
        {
            using (var connection = Program.GetOpenConnection())
            {
                connection.ExecuteAsync("create table #literalin(id int not null);").Wait();
                connection.ExecuteAsync("insert #literalin (id) values (@id)", new[] {
                    new { id = 1 },
                    new { id = 2 },
                    new { id = 3 },
                }).Wait();
                var count = connection.QueryAsync<int>("select count(1) from #literalin where id in {=ids}",
                    new { ids = new[] { 1, 3, 4 } }).Result.Single();
                count.IsEqualTo(2);
            }
        }


        public void RunSequentialVersusParallelAsync()
        {

            var ids = Enumerable.Range(1, 20000).Select(id => new { id }).ToArray();
            using (var connection = Program.GetOpenConnection(true))
            {
                connection.ExecuteAsync(new CommandDefinition("select @id", ids.Take(5), flags: CommandFlags.None)).Wait();

                var watch = Stopwatch.StartNew();
                connection.ExecuteAsync(new CommandDefinition("select @id", ids, flags: CommandFlags.None)).Wait();
                watch.Stop();
                System.Console.WriteLine("No pipeline: {0}ms", watch.ElapsedMilliseconds);

                watch = Stopwatch.StartNew();
                connection.ExecuteAsync(new CommandDefinition("select @id", ids, flags: CommandFlags.Pipelined)).Wait();
                watch.Stop();
                System.Console.WriteLine("Pipeline: {0}ms", watch.ElapsedMilliseconds);
            }
        }

        public void RunSequentialVersusParallelSync()
        {

            var ids = Enumerable.Range(1, 20000).Select(id => new { id }).ToArray();
            using (var connection = Program.GetOpenConnection(true))
            {
                connection.Execute(new CommandDefinition("select @id", ids.Take(5), flags: CommandFlags.None));

                var watch = Stopwatch.StartNew();
                connection.Execute(new CommandDefinition("select @id", ids, flags: CommandFlags.None));
                watch.Stop();
                System.Console.WriteLine("No pipeline: {0}ms", watch.ElapsedMilliseconds);

                watch = Stopwatch.StartNew();
                connection.Execute(new CommandDefinition("select @id", ids, flags: CommandFlags.Pipelined));
                watch.Stop();
                System.Console.WriteLine("Pipeline: {0}ms", watch.ElapsedMilliseconds);
            }
        }

        public void AssertNoCacheWorksForQueryMultiple()
        {
            int a = 123, b = 456;
            var cmdDef = new CommandDefinition(@"select @a; select @b;", new
            {
                a, b
            }, commandType: CommandType.Text, flags: CommandFlags.NoCache);

            int c, d;
            Dapper.SqlMapper.PurgeQueryCache();
            int before = Dapper.SqlMapper.GetCachedSQLCount();
            using (var sqlConnection = Program.GetOpenConnection(true))
            {
                using (var multi = sqlConnection.QueryMultiple(cmdDef))
                {
                    c = multi.Read<int>().Single();
                    d = multi.Read<int>().Single();
                }
            }
            int after = Dapper.SqlMapper.GetCachedSQLCount();
            before.IsEqualTo(0);
            after.IsEqualTo(0);
            c.IsEqualTo(123);
            d.IsEqualTo(456);


        }
        class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Category Category { get; set; }
        }
        class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        class BasicType
        {
            public string Value { get; set; }
        }


        public void TypeBasedViaType()
        {
            Type type = GetSomeType();

            using (var connection = Program.GetOpenConnection(true))
            {
                dynamic actual = connection.QueryAsync(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).Result.FirstOrDefault();
                ((object)actual).GetType().IsEqualTo(type);
                int a = actual.A;
                string b = actual.B;
                a.IsEqualTo(123);
                b.IsEqualTo("abc");
            }
        }

        static Type GetSomeType()
        {
            return typeof(SomeType);
        }
        public class SomeType
        {
            public int A { get; set; }
            public string B { get; set; }
        }

        public void Issue22_ExecuteScalar()
        {
            using (var connection = Program.GetOpenConnection())
            {
                int i = connection.ExecuteScalarAsync<int>("select 123").Result;
                i.IsEqualTo(123);

                i = connection.ExecuteScalarAsync<int>("select cast(123 as bigint)").Result;
                i.IsEqualTo(123);

                long j = connection.ExecuteScalarAsync<long>("select 123").Result;
                j.IsEqualTo(123L);

                j = connection.ExecuteScalarAsync<long>("select cast(123 as bigint)").Result;
                j.IsEqualTo(123L);

                int? k = connection.ExecuteScalar<int?>("select @i", new { i = default(int?) });
                k.IsNull();
            }
        }

        public void TestSupportForDynamicParametersOutputExpressions()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                connection.ExecuteAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId", p).Wait();

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
            }
        }
        public void TestSupportForDynamicParametersOutputExpressions_Scalar()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = (int)connection.ExecuteScalarAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p).Result;

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }
        public void TestSupportForDynamicParametersOutputExpressions_Query_Default()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = connection.QueryAsync<int>(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p).Result.Single();

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }
        public void TestSupportForDynamicParametersOutputExpressions_Query_Buffered()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = connection.QueryAsync<int>(new CommandDefinition(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, flags: CommandFlags.Buffered)).Result.Single();

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }
        public void TestSupportForDynamicParametersOutputExpressions_Query_NonBuffered()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = connection.QueryAsync<int>(new CommandDefinition(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, flags: CommandFlags.None)).Result.Single();

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }
        public void TestSupportForDynamicParametersOutputExpressions_QueryMultiple()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                int x, y;
                using (var multi = connection.QueryMultipleAsync(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
select 42
select 17
SET @AddressPersonId = @PersonId", p).Result)
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
        }

        public void TestSubsequentQueriesSuccess()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var data0 = connection.QueryAsync<Foo0>("select 1 as [Id] where 1 = 0").Result.ToList();
                data0.Count().IsEqualTo(0);

                var data1 = connection.QueryAsync<Foo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).Result.ToList();
                data1.Count().IsEqualTo(0);

                var data2 = connection.QueryAsync<Foo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).Result.ToList();
                data2.Count().IsEqualTo(0);

                data0 = connection.QueryAsync<Foo0>("select 1 as [Id] where 1 = 0").Result.ToList();
                data0.Count().IsEqualTo(0);

                data1 = connection.QueryAsync<Foo1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).Result.ToList();
                data1.Count().IsEqualTo(0);

                data2 = connection.QueryAsync<Foo2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).Result.ToList();
                data2.Count().IsEqualTo(0);

            }
        }
        class Foo0 { public int Id { get; set; } }
        class Foo1 { public int Id { get; set; } }
        class Foo2 { public int Id { get; set; } }
        class Person
        {
            public int PersonId { get; set; }
            public string Name { get; set; }
            public string Occupation { get; private set; }
            public int NumberOfLegs = 2;
            public Address Address { get; set; }
        }
        class Address
        {
            public int AddressId { get; set; }
            public string Name { get; set; }
            public int PersonId { get; set; }
        }

        class ReviewBoard
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public User User1 { get; set; }
            public User User2 { get; set; }
            public User User3 { get; set; }
            public User User4 { get; set; }
            public User User5 { get; set; }
            public User User6 { get; set; }
            public User User7 { get; set; }
            public User User8 { get; set; }
            public User User9 { get; set; }
        }
        class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public void TestMultiMapArbitraryMaps()
        {
            // please excuse the trite example, but it is easier to follow than a more real-world one
            var createSql = @"
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
            using (var connection = Program.GetOpenConnection())
            {
                connection.ExecuteAsync(createSql).Wait();
                try
                {
                    var sql = @"
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

                    var data = connection.QueryAsync<ReviewBoard>(sql, types, mapper).Result.ToList();

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
        }

        public void Issue157_ClosedReaderAsync()
        {
            using(var conn = Program.GetOpenConnection())
            {
                var args = new { x = 42 };
                const string sql = @"select 123 as [A], 'abc' as [B] where @x=42";
                var row = conn.QueryAsync<SomeType>(new CommandDefinition(
                    sql, args, flags:CommandFlags.None)).Result.Single();
                row.IsNotNull();
                row.A.IsEqualTo(123);
                row.B.IsEqualTo("abc");

                args = new { x = 5 };
                conn.QueryAsync<SomeType>(new CommandDefinition(
                    sql, args, flags: CommandFlags.None)).Result.Any().IsFalse();
            }
        }

        public void TestAtEscaping()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var id = connection.QueryAsync<int>(@"
                    declare @@Name int
                    select @@Name = @Id+1
                    select @@Name
                    ", new Product { Id = 1 }).Result.Single();
                id.IsEqualTo(2);

            }
        }
    }
}
#endif