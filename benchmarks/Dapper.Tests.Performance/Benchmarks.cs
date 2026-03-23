using BenchmarkDotNet.Attributes;
using System;
using System.Configuration;
using Microsoft.Data.SqlClient;

namespace Dapper.Tests.Performance
{
    [BenchmarkCategory("ORM")]
    public abstract class BenchmarkBase
    {
        protected static readonly Random _rand = new Random();
        protected SqlConnection _connection;
        public static ConnectionStringSettings ConnectionStringSettings { get; } = ConfigurationManager.ConnectionStrings["Main"];
        public static string ConnectionString { get; } = ConnectionStringSettings.ConnectionString;
        protected int i;

        protected void BaseSetup()
        {
            i = 0;
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
        }

        protected void RegisterSqlFactory()
        {
#if NETCOREAPP
            System.Data.Common.DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
#endif
        }

        protected void Step()
        {
            i++;
            if (i > 5000) i = 1;
        }
    }
}
