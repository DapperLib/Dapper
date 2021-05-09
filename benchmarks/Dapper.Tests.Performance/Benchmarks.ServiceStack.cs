using BenchmarkDotNet.Attributes;
using ServiceStack.OrmLite;
using System.ComponentModel;
using System.Data;

namespace Dapper.Tests.Performance
{
    [Description("ServiceStack")]
    public class ServiceStackBenchmarks : BenchmarkBase
    {
        private IDbConnection _db;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServerDialect.Provider);
            _db = dbFactory.Open();
        }

        [Benchmark(Description = "SingleById<T>")]
        public Post Query()
        {
            Step();
            return _db.SingleById<Post>(i);
        }
    }
}
