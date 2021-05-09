using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using System.Linq;
using RepoDb;

namespace Dapper.Tests.Performance
{
    [Description("RepoDB")]
    public class RepoDbBenchmarks : BenchmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            SqlServerBootstrap.Initialize();
            ClassMapper.Add<Post>("Posts");
        }

        [Benchmark(Description = "Query")]
        public Post Query()
        {
            Step();
            return _connection.Query<Post>(i).FirstOrDefault();
        }
    }
}
