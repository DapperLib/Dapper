using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Chloe;
using Chloe.SqlServer;

namespace Dapper.Tests.Performance
{
    [Description("Chloe")]
    public class ChloeBenchmarks: BenchmarkBase
    {
        private IDbContext _context;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _context = new MsSqlContext(_connection.ConnectionString);
        }

        [Benchmark(Description = "First")]
        public Post First()
        {
            Step();
            return _context.Query<Post>().Where(post => post.Id == i).First();
        }
    }
}
