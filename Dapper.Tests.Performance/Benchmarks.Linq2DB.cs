using BenchmarkDotNet.Attributes;

using System;
using System.Linq;
using Dapper.Tests.Performance.Linq2Db;
using LinqToDB;
using LinqToDB.Data;

namespace Dapper.Tests.Performance
{
    public class Linq2DBBenchmarks : BenchmarkBase
    {
        private Linq2DBContext _dbContext;

        private static readonly Func<Linq2DBContext, int, Post> compiledQuery = CompiledQuery.Compile((Linq2DBContext db, int id) => db.Posts.First(c => c.Id == id));

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            DataConnection.DefaultSettings = new Linq2DBSettings(_connection.ConnectionString);
            _dbContext = new Linq2DBContext();
        }

        [Benchmark(Description = "Normal")]
        public Post Normal()
        {
            Step();
            return _dbContext.Posts.First(p => p.Id == i);
        }

        [Benchmark(Description = "Compiled")]
        public Post Compiled()
        {
            Step();
            return compiledQuery(_dbContext, i);
        }

        [Benchmark(Description = "SqlQuery")]
        public Post SqlQuery()
        {
            Step();
            return _dbContext.Query<Post>("select * from Posts where Id = @id", new { id = i }).First();
        }
    }
}
