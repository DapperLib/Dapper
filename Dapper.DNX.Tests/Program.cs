using System;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Reflection;

namespace Dapper.DNX.Tests
{
    public class Program
    {
        public void Main()
        {
#if DNXCORE50
            Console.WriteLine("From: {0}", typeof(int).AssemblyQualifiedName);
#else
            Console.WriteLine("Version: {0}", Environment.Version);
#endif
            const string connectionString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var row = conn.Query<Foo>("select @a as X", new { a = 123 }).Single();
                Console.WriteLine(row.X);

                var methods = typeof(Dapper.SqlMapper).GetMethods().Where(x => x.Name == "QueryAsync").ToList();
#if ASYNC
                row = conn.QueryAsync<Foo>("select @a as X", new { a = 123 }).Result.Single();
#endif
                Console.WriteLine(row.X);
            }
        }
        class Foo
        {
            public int X { get; set; }
        }
    }
}
