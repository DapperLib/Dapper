using System.Linq;
using Dapper;
using SqlMapper;
using System.Data;

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
            using(var conn = Program.GetOpenConnection())
            {
                using(Dapper.SqlMapper.GridReader multi = conn.QueryMultipleAsync("select 1; select 2").Result)
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