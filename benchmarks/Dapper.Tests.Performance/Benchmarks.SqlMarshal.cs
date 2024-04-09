using BenchmarkDotNet.Attributes;
using System.ComponentModel;

namespace Dapper.Tests.Performance
{
    [Description("SqlMarshal")]
    public partial class SqlMarshalBenchmarks : BenchmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
        }

        [Benchmark(Description = "SqlCommand")]
        public Post SqlCommand()
        {
            Step();
            return ReadPost("select Top 1 * from Posts where Id = @id", i);
        }

        [SqlMarshal("")]
        private partial Post ReadPost([RawSql]string sql, int id);
    }
}
