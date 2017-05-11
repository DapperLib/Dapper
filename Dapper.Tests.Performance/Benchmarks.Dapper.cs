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

        [Benchmark(Description = "Dapper: Query<T> (buffered)", OperationsPerInvoke = Iterations)]
        public Post QueryBuffered()
        {
            Step();
            return _connection.Query<Post>("select * from Posts where Id = @Id", new { Id = i }, buffered: true).First();
        }

        [Benchmark(Description = "Dapper: Query<T> (unbuffered)", OperationsPerInvoke = Iterations)]
        public Post QueryUnbuffered()
        {
            Step();
            return _connection.Query<Post>("select * from Posts where Id = @Id", new { Id = i }, buffered: false).First();
        }

        [Benchmark(Description = "Dapper: QueryFirstOrDefault<T>", OperationsPerInvoke = Iterations)]
        public Post QueryFirstOrDefault()
        {
            Step();
            return _connection.QueryFirstOrDefault<Post>("select * from Posts where Id = @Id", new { Id = i });
        }

        [Benchmark(Description = "Dapper: Query<dyanmic> (buffered)", OperationsPerInvoke = Iterations)]
        public object QueryBufferedDynamic()
        {
            Step();
            return _connection.Query("select * from Posts where Id = @Id", new { Id = i }, buffered: true).First();
        }

        [Benchmark(Description = "Dapper: Query<dyanmic> (unbuffered)", OperationsPerInvoke = Iterations)]
        public object QueryUnbufferedDynamic()
        {
            Step();
            return _connection.Query("select * from Posts where Id = @Id", new { Id = i }, buffered: false).First();
        }

        [Benchmark(Description = "Dapper: QueryFirstOrDefault<dyanmic>", OperationsPerInvoke = Iterations)]
        public object QueryFirstOrDefaultDynamic()
        {
            Step();
            return _connection.QueryFirstOrDefault("select * from Posts where Id = @Id", new { Id = i }).First();
        }

        [Benchmark(Description = "Dapper: Contrib Get<T>", OperationsPerInvoke = Iterations)]
        public object ContribGet()
        {
            Step();
            return _connection.Get<Post>(i);
        }
    }
}