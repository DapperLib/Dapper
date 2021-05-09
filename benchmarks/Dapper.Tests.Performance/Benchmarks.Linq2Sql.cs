#if NET4X
using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.Linq2Sql;
using System;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;

namespace Dapper.Tests.Performance
{
    [Description("LINQ to SQL")]
    public class Linq2SqlBenchmarks : BenchmarkBase
    {
        private DataClassesDataContext Linq2SqlContext;

        private static readonly Func<DataClassesDataContext, int, Linq2Sql.Post> compiledQuery =
            CompiledQuery.Compile((DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            Linq2SqlContext = new DataClassesDataContext(_connection);
        }

        [Benchmark(Description = "First")]
        public Linq2Sql.Post First()
        {
            Step();
            return Linq2SqlContext.Posts.First(p => p.Id == i);
        }

        [Benchmark(Description = "First (Compiled)")]
        public Linq2Sql.Post Compiled()
        {
            Step();
            return compiledQuery(Linq2SqlContext, i);
        }

        [Benchmark(Description = "ExecuteQuery")]
        public Post ExecuteQuery()
        {
            Step();
            return Linq2SqlContext.ExecuteQuery<Post>("select * from Posts where Id = {0}", i).First();
        }
    }
}
#endif
