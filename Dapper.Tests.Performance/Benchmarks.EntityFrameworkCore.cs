using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.Linq2Sql;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Linq;
using System.Linq;

namespace Dapper.Tests.Performance
{
    public class EFCoreBenchmarks : BenchmarkBase
    {
        private EntityFrameworkCore.EFCoreContext Context;
        private static readonly Func<DataClassesDataContext, int, Linq2Sql.Post> compiledQuery =
            CompiledQuery.Compile((DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            Context = new EntityFrameworkCore.EFCoreContext(_connection.ConnectionString);
        }

        [Benchmark(Description = "Normal")]
        public Post Normal()
        {
            Step();
            return Context.Posts.First(p => p.Id == i);
        }

        [Benchmark(Description = "SqlQuery")]
        public Post SqlQuery()
        {
            Step();
            return Context.Posts.FromSql("select * from Posts where Id = {0}", i).First();
        }

        [Benchmark(Description = "No Tracking")]
        public Post NoTracking()
        {
            Step();
            return Context.Posts.AsNoTracking().First(p => p.Id == i);
        }
    }
}