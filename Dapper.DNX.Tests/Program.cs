using System;
using System.Linq;
using System.Data.SqlClient;

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
            }
        }
        class Foo
        {
            public int X { get; set; }
        }
    }
}
