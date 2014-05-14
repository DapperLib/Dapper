using System.Linq;
using Dapper;
using SqlMapper;
using System.Data;
using System.Diagnostics;

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

        public void TestBasicStringUsageClosedAsync()
        {
            using (var connection = Program.GetClosedConnection())
            {
                var query = connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.IsSequenceEqualTo(new[] { "abc", "def" });
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
                    multi.Read<int>().Single().IsEqualTo(1);
                    multi.Read<int>().Single().IsEqualTo(2);
                }
            }
        }
        public void TestMultiClosedConnAsync()
        {
            using (var conn = Program.GetClosedConnection())
            {
                using (Dapper.SqlMapper.GridReader multi = conn.QueryMultipleAsync("select 1; select 2").Result)
                {
                    multi.Read<int>().Single().IsEqualTo(1);
                    multi.Read<int>().Single().IsEqualTo(2);
                }
            }
        }

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
    }
}