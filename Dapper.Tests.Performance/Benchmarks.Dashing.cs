using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.Dashing;
using Dashing;

namespace Dapper.Tests.Performance
{
    public class DashingBenchmarks : BenchmarkBase
    {
        private ISession session;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            var configuration = new DashingConfiguration();
            var database = new SqlDatabase(configuration, ConnectionString);
            this.session = database.BeginTransactionLessSession(_connection);
        }

        [Benchmark(Description = "Get By Id")]
        public Dashing.Post QueryBuffered()
        {
            Step();
            return session.Get<Dashing.Post>(i);
        }
    }
}
