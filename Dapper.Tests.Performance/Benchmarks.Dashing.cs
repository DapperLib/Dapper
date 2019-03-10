using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.Dashing;
using Dashing;

namespace Dapper.Tests.Performance
{
    [Description("Dashing")]
    public class DashingBenchmarks : BenchmarkBase
    {
        private ISession Session;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            var configuration = new DashingConfiguration();
            var database = new SqlDatabase(configuration, ConnectionString);
            Session = database.BeginTransactionLessSession(_connection);
        }

        [Benchmark(Description = "Get")]
        public Dashing.Post Get()
        {
            Step();
            return Session.Get<Dashing.Post>(i);
        }
    }
}
