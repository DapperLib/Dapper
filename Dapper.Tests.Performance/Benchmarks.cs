using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Dapper.Tests.Performance
{
    [BenchmarkCategory("ORM")]
    public abstract class BenchmarkBase
    {
        protected static readonly Random _rand = new Random();
        protected SqlConnection _connection;
        public static string ConnectionString { get; } = ConfigurationManager.ConnectionStrings["Main"].ConnectionString;
        protected int i;

        protected void BaseSetup()
        {
            i = 0;
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
        }

        protected void Step()
        {
            i++;
            if (i > 5000) i = 1;
        }
    }
}
