using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Dapper.Tests.Performance.Helpers;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Dapper.Tests.Performance
{
    [OrderProvider(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [Config(typeof(Config))]
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
            Add(new ReturnColum());
            Add(Job.Default
                .WithUnrollFactor(BenchmarkBase.Iterations)
                //.WithIterationTime(new TimeInterval(500, TimeUnit.Millisecond))
                .WithLaunchCount(1)
                .WithWarmupCount(0)
                .WithTargetCount(5)
                .WithRemoveOutliers(true)
            );
        }
    }
}