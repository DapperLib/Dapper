using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using Xunit;

namespace Dapper.Tests
{
    public static class DatabaseProvider<TProvider> where TProvider : DatabaseProvider
    {
        public static TProvider Instance { get; } = Activator.CreateInstance<TProvider>();
    }
    public abstract class DatabaseProvider
    {
        public abstract DbProviderFactory Factory { get; }

        public virtual void Dispose() { }
        public abstract string GetConnectionString();

        protected static string GetConnectionString(string name, string defaultConnectionString) =>
            Environment.GetEnvironmentVariable(name) ?? defaultConnectionString;

        public DbConnection GetOpenConnection()
        {
            var conn = Factory.CreateConnection()!;
            conn.ConnectionString = GetConnectionString();
            conn.Open();
            if (conn.State != ConnectionState.Open) throw new InvalidOperationException("should be open!");
            return conn;
        }

        public DbConnection GetClosedConnection()
        {
            var conn = Factory.CreateConnection()!;
            conn.ConnectionString = GetConnectionString();
            if (conn.State != ConnectionState.Closed) throw new InvalidOperationException("should be closed!");
            return conn;
        }

        public DbParameter CreateRawParameter(string name, object value)
        {
            var p = Factory.CreateParameter()!;
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }

    public abstract class SqlServerDatabaseProvider : DatabaseProvider
    {
        public override string GetConnectionString() => GetConnectionString(false);

        private string GetConnectionString(bool mars)
        {
            var builder = Factory.CreateConnectionStringBuilder()!;
            builder.ConnectionString = GetConnectionString("SqlServerConnectionString", "Data Source=.;Initial Catalog=tempdb;Integrated Security=True");
            builder["TrustServerCertificate"] = true;
            if (mars)
            {
                ((dynamic)builder).MultipleActiveResultSets = true;
            }
            return builder.ConnectionString;
        }

        public DbConnection GetOpenConnection(bool mars)
        {
            var conn = Factory.CreateConnection()!;
            conn.ConnectionString = GetConnectionString(mars);
            conn.Open();
            if (conn.State != ConnectionState.Open) throw new InvalidOperationException("should be open!");
            return conn;
        }
    }
    public sealed class SystemSqlClientProvider : SqlServerDatabaseProvider
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public override DbProviderFactory Factory => System.Data.SqlClient.SqlClientFactory.Instance;
#pragma warning restore CS0618 // Type or member is obsolete
    }
#if MSSQLCLIENT
    public sealed class MicrosoftSqlClientProvider : SqlServerDatabaseProvider
    {
        public override DbProviderFactory Factory => Microsoft.Data.SqlClient.SqlClientFactory.Instance;
    }
#endif

    public abstract class TestBase<TProvider> : IDisposable where TProvider : DatabaseProvider
    {
        protected void SkipIfMsDataClient()
            => Skip.If<Microsoft.Data.SqlClient.SqlConnection>(connection);

        protected DbConnection GetOpenConnection() => Provider.GetOpenConnection();
        protected DbConnection GetClosedConnection() => Provider.GetClosedConnection();
        protected DbConnection? _connection;
        protected DbConnection connection => _connection ??= Provider.GetOpenConnection();

        public TProvider Provider { get; } = DatabaseProvider<TProvider>.Instance;

        protected static CultureInfo ActiveCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
            set { Thread.CurrentThread.CurrentCulture = value; }
        }

        static TestBase()
        {
            Console.WriteLine("Dapper: " + typeof(SqlMapper).AssemblyQualifiedName);
            var provider = DatabaseProvider<TProvider>.Instance;
            Console.WriteLine("Using Connectionstring: {0}", provider.GetConnectionString());
            var factory = provider.Factory;
            Console.WriteLine("Using Provider: {0}", factory.GetType().FullName);
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
        }

        public virtual void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
            Provider?.Dispose();
        }
    }

    public static class NonParallelDefinition
    {
        public const string Name = "NonParallel";
    }
}
