#if NET4X
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

        // This needs love to be compatible with current SDKs (weaving doesn't work and shouldn't be used here anyway (competition).
        // I'll file an issue with Dashing to see if someone can help me out here since I can't figure out from the docs how to
        // make it work correctly.
        //[Benchmark(Description = "Get")]
        public Dashing.Post Get()
        {
            Step();
            return Session.Get<Dashing.Post>(i);
        }
    }
}
#endif
