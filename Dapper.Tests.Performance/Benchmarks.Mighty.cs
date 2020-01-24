using BenchmarkDotNet.Attributes;
using Mighty;
using System.ComponentModel;
using System.Linq;

namespace Dapper.Tests.Performance
{
    [Description("Mighty")]
    public class MightyBenchmarks : BenchmarkBase
    {
        private MightyOrm<Post> _model;
        private MightyOrm _dynamicModel;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _model = new MightyOrm<Post>(ConnectionString);
            _dynamicModel = new MightyOrm(ConnectionString);
        }

        [Benchmark(Description = "Query<T>")]
        public Post Query()
        {
            Step();
            return _model.Query("select * from Posts where Id = @0", _connection, i).First();
        }

        [Benchmark(Description = "Query<dynamic>")]
        public dynamic QueryDynamic()
        {
            Step();
            return _dynamicModel.Query("select * from Posts where Id = @0", _connection, i).First();
        }

        [Benchmark(Description = "SingleFromQuery<T>")]
        public Post SingleFromQuery()
        {
            Step();
            return _model.SingleFromQuery("select * from Posts where Id = @0", _connection, i);
        }

        [Benchmark(Description = "SingleFromQuery<dynamic>")]
        public dynamic SingleFromQueryDynamic()
        {
            Step();
            return _dynamicModel.SingleFromQuery("select * from Posts where Id = @0", _connection, i);
        }
    }
}
