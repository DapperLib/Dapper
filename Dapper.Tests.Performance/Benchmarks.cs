using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using Dapper.Tests.Performance.Helpers;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Dapper.Tests.Performance
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [Config(typeof(Config))]
    [BenchmarkCategory("ORM")]
    public abstract class BenchmarkBase
    {
        public const int Iterations = 50;
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

    public class Config : ManualConfig
    {
        public Config()
        {
            Add(new MemoryDiagnoser());
            Add(new ORMColum());
            Add(TargetMethodColumn.Method);
            Add(new ReturnColum());
            Add(StatisticColumn.Mean);
            Add(StatisticColumn.StdDev);
            Add(StatisticColumn.Error);
            Add(BaselineScaledColumn.Scaled);
            Add(Job.Dry
                .WithLaunchCount(1)
                .WithWarmupCount(0)
                .WithInvocationCount(50)
                .WithIterationCount(BenchmarkBase.Iterations)
                .WithMaxIterationCount(BenchmarkBase.Iterations)
                .WithOutlierMode(OutlierMode.All)
            );
        }
    }
}
