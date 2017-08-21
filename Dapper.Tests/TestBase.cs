using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Xunit;
#if !NETCOREAPP1_0
using System.Threading;
#endif

namespace Dapper.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected static readonly bool IsAppVeyor = Environment.GetEnvironmentVariable("Appveyor")?.ToUpperInvariant() == "TRUE";

        public static string ConnectionString =>
            IsAppVeyor
                ? @"Server=(local)\SQL2016;Database=tempdb;User ID=sa;Password=Password12!"
                : "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        protected SqlConnection _connection;
        protected SqlConnection connection => _connection ?? (_connection = GetOpenConnection());

        public static SqlConnection GetOpenConnection(bool mars = false)
        {
            var cs = ConnectionString;
            if (mars)
            {
                var scsb = new SqlConnectionStringBuilder(cs)
                {
                    MultipleActiveResultSets = true
                };
                cs = scsb.ConnectionString;
            }
            var connection = new SqlConnection(cs);
            connection.Open();
            return connection;
        }

        public SqlConnection GetClosedConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            if (conn.State != ConnectionState.Closed) throw new InvalidOperationException("should be closed!");
            return conn;
        }

        protected static CultureInfo ActiveCulture
        {
#if NETCOREAPP1_0
            get { return CultureInfo.CurrentCulture; }
            set { CultureInfo.CurrentCulture = value; }
#else
            get { return Thread.CurrentThread.CurrentCulture; }
            set { Thread.CurrentThread.CurrentCulture = value; }
#endif
        }

        static TestBase()
        {
            Console.WriteLine("Dapper: " + typeof(SqlMapper).AssemblyQualifiedName);
            Console.WriteLine("Using Connectionstring: {0}", ConnectionString);
#if NETCOREAPP1_0
            Console.WriteLine("CoreCLR (netcoreapp1.0)");
#else
            Console.WriteLine(".NET: " + Environment.Version);
            Console.Write("Loading native assemblies for SQL types...");
            try
            {
                SqlServerTypesLoader.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
                Console.WriteLine("done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed.");
                Console.Error.WriteLine(ex.Message);
            }
#endif
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class NonParallelDefinition : TestBase
    {
        public const string Name = "NonParallel";
    }
}
