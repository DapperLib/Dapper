using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.Dashing;
using Dashing;

namespace Dapper.Tests.Performance
{
    public class DashingBenchmarks : BenchmarkBase
    {
        private SqlDatabase database;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            var configuration = new DashingConfiguration();
            database = new SqlDatabase(configuration, ConnectionString);
        }

        [Benchmark(Description = "Get By Id")]
        public Dashing.Post QueryBuffered()
        {
            Step();
            using (var session = database.BeginTransactionLessSession(_connection))
            {
                return session.Get<Dashing.Post>(i);
            }
        }
    }
}
