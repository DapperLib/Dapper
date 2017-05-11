using BenchmarkDotNet.Attributes;

namespace Dapper.Tests.Performance
{
    public class SomaBenchmarks : BenchmarkBase
    {
        private dynamic _sdb;

        [Setup]
        public void Setup()
        {
            BaseSetup();
            _sdb = Simple.Data.Database.OpenConnection(ConnectionString);
        }

        [Benchmark(Description = "Soma: FindById", OperationsPerInvoke = Iterations)]
        public dynamic Query()
        {
            Step();
            return _sdb.Posts.FindById(i).FirstOrDefault();
        }
    }
}