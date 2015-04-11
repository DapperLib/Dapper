using System;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Dapper.DNX.Tests
{
    public class Program
    {
        public void Main()
        {
            Console.WriteLine("Version: {0}", Environment.Version);
            const string connectionString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var row = conn.Query<Foo>("select @a as X", new { a = 123 }).Single();
                Console.WriteLine(row.X);

                var methods = typeof(Dapper.SqlMapper).GetMethods().Where(x => x.Name == "QueryAsync").ToList();

                row = conn.QueryAsync<Foo>("select @a as X", new { a = 123 }).Result.Single();
                Console.WriteLine(row.X);
            }
        }
        private static async Task<int> WithDelay(int i)
        {
            await Task.Delay(100);
            return i;
        }
        class Foo
        {
            public int X { get; set; }
        }
    }
}
