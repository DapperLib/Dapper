using BenchmarkDotNet.Attributes;
using Massive;
using System.ComponentModel;
using System.Linq;

namespace Dapper.Tests.Performance
{
    [Description("Massive")]
    public class MassiveBenchmarks : BenchmarkBase
    {
        private DynamicModel _model;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _model = new DynamicModel(ConnectionString);
        }

        [Benchmark(Description = "Query (dynamic)")]
        public dynamic QueryDynamic()
        {
            Step();
            return _model.Query("select * from Posts where Id = @0", _connection, i).First();
        }
    }
}
