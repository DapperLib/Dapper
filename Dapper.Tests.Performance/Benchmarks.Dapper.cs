using BenchmarkDotNet.Attributes;
using Dapper.Contrib.Extensions;
using System.Linq;

namespace Dapper.Tests.Performance
{
    public class DapperBenchmarks : BenchmarkBase
    {
        [Setup]
        public void Setup()
        {
            BaseSetup();
        }

        [Benchmark(Description = "Query<T> (buffered)", OperationsPerInvoke = Iterations)]
        public Post QueryBuffered()
        {
            Step();
            return _connection.Query<Post>("select * from Posts where Id = @Id", new { Id = i }, buffered: true).First();
        }
        [Benchmark(Description = "Query<dyanmic> (buffered)", OperationsPerInvoke = Iterations)]
        public dynamic QueryBufferedDynamic()
        {
            Step();
            return _connection.Query("select * from Posts where Id = @Id", new { Id = i }, buffered: true).First();
        }

        [Benchmark(Description = "Query<T> (unbuffered)", OperationsPerInvoke = Iterations)]
        public Post QueryUnbuffered()
        {
            Step();
            return _connection.Query<Post>("select * from Posts where Id = @Id", new { Id = i }, buffered: false).First();
        }
        [Benchmark(Description = "Query<dyanmic> (unbuffered)", OperationsPerInvoke = Iterations)]
        public dynamic QueryUnbufferedDynamic()
        {
            Step();
            return _connection.Query("select * from Posts where Id = @Id", new { Id = i }, buffered: false).First();
        }

        [Benchmark(Description = "QueryFirstOrDefault<T>", OperationsPerInvoke = Iterations)]
        public Post QueryFirstOrDefault()
        {
            Step();
            return _connection.QueryFirstOrDefault<Post>("select * from Posts where Id = @Id", new { Id = i });
        }
        [Benchmark(Description = "QueryFirstOrDefault<dyanmic>", OperationsPerInvoke = Iterations)]
        public dynamic QueryFirstOrDefaultDynamic()
        {
            Step();
            return _connection.QueryFirstOrDefault("select * from Posts where Id = @Id", new { Id = i }).First();
        }
        
        [Benchmark(Description = "Contrib Get<T>", OperationsPerInvoke = Iterations)]
        public Post ContribGet()
        {
            Step();
            return _connection.Get<Post>(i);
        }
    }
}