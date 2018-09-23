using BenchmarkDotNet.Attributes;
using Dapper.Contrib.Extensions;
using System.ComponentModel;
using System.Linq;

namespace Dapper.Tests.Performance
{
    [Description("Dapper")]
    public class DapperBenchmarks : BenchmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
        }

        [Benchmark(Description = "Query<T> (buffered)")]
        public Post QueryBuffered()
        {
            Step();
            return _connection.Query<Post>("select * from Posts where Id = @Id", new { Id = i }, buffered: true).First();
        }

        [Benchmark(Description = "Query<dynamic> (buffered)")]
        public dynamic QueryBufferedDynamic()
        {
            Step();
            return _connection.Query("select * from Posts where Id = @Id", new { Id = i }, buffered: true).First();
        }

        [Benchmark(Description = "Query<T> (unbuffered)")]
        public Post QueryUnbuffered()
        {
            Step();
            return _connection.Query<Post>("select * from Posts where Id = @Id", new { Id = i }, buffered: false).First();
        }

        [Benchmark(Description = "Query<dynamic> (unbuffered)")]
        public dynamic QueryUnbufferedDynamic()
        {
            Step();
            return _connection.Query("select * from Posts where Id = @Id", new { Id = i }, buffered: false).First();
        }

        [Benchmark(Description = "QueryFirstOrDefault<T>")]
        public Post QueryFirstOrDefault()
        {
            Step();
            return _connection.QueryFirstOrDefault<Post>("select * from Posts where Id = @Id", new { Id = i });
        }

        [Benchmark(Description = "QueryFirstOrDefault<dynamic>")]
        public dynamic QueryFirstOrDefaultDynamic()
        {
            Step();
            return _connection.QueryFirstOrDefault("select * from Posts where Id = @Id", new { Id = i }).First();
        }

        [Benchmark(Description = "Contrib Get<T>")]
        public Post ContribGet()
        {
            Step();
            return _connection.Get<Post>(i);
        }
    }
}
