using System.Data;
using System.Linq;
using Dapper;
using SqlMapper;

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

        public void TestClassWithStringUsageAsync()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });
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

        public void TestDataSet()
        {
            string sql = "select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids";

            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryDataSetAsync(sql, new { Ids = new[] { 1, 2, 3 } });
                DataSet ds = query.Result;

                ds.IsNotNull();
                ds.Tables.Count.IsEqualTo(1);

                DataTable dt = ds.Tables[0];

                dt.Rows.Count.IsEqualTo(3);
                dt.Rows[0]["Id"].IsEqualTo(1);
                dt.Rows[1]["Id"].IsEqualTo(2);
                dt.Rows[2]["Id"].IsEqualTo(3);
            }
        }

        public void TestDataTable()
        {
            string sql = "select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids";

            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryDataTableAsync(sql, new { Ids = new[] { 1, 2, 3 } });
                DataTable dt = query.Result;

                dt.IsNotNull();
                dt.Rows.Count.IsEqualTo(3);
                dt.Rows[0]["Id"].IsEqualTo(1);
                dt.Rows[1]["Id"].IsEqualTo(2);
                dt.Rows[2]["Id"].IsEqualTo(3);
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