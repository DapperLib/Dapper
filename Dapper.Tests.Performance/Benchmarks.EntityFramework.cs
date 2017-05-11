using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.Linq2Sql;
using System;
using System.Data.Linq;
using System.Linq;

namespace Dapper.Tests.Performance
{
    public class EntityFrameworkBenchmarks : BenchmarkBase
    {
        private EntityFramework.EFContext Context;
        private static readonly Func<DataClassesDataContext, int, Linq2Sql.Post> compiledQuery =
            CompiledQuery.Compile((DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));

        [Setup]
        public void Setup()
        {
            BaseSetup();
            Context = new EntityFramework.EFContext(_connection);
        }

        [Benchmark(Description = "EF6: Normal", OperationsPerInvoke = Iterations)]
        public Post Normal()
        {
            Step();
            return Context.Posts.First(p => p.Id == i);
        }

        [Benchmark(Description = "EF6: SqlQuery", OperationsPerInvoke = Iterations)]
        public Post SqlQuery()
        {
            Step();
            return Context.Database.SqlQuery<Post>("select * from Posts where Id = {0}", i).First();
        }

        [Benchmark(Description = "EF6: No Tracking", OperationsPerInvoke = Iterations)]
        public Post NoTracking()
        {
            Step();
            return Context.Posts.AsNoTracking().First(p => p.Id == i);
        }
    }
}