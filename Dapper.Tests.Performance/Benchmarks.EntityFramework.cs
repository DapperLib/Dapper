#if NET4X
using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using System.Linq;

namespace Dapper.Tests.Performance
{
    [Description("EF 6")]
    public class EF6Benchmarks : BenchmarkBase
    {
        private EntityFramework.EFContext Context;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            Context = new EntityFramework.EFContext(_connection);
        }

        [Benchmark(Description = "First")]
        public Post First()
        {
            Step();
            return Context.Posts.First(p => p.Id == i);
        }

        [Benchmark(Description = "SqlQuery")]
        public Post SqlQuery()
        {
            Step();
            return Context.Database.SqlQuery<Post>("select * from Posts where Id = {0}", i).First();
        }

        [Benchmark(Description = "First (No Tracking)")]
        public Post NoTracking()
        {
            Step();
            return Context.Posts.AsNoTracking().First(p => p.Id == i);
        }
    }
}
#endif
