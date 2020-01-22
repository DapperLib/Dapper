using BenchmarkDotNet.Attributes;
using PetaPoco;
using System.ComponentModel;
using System.Linq;

namespace Dapper.Tests.Performance
{
    [Description("PetaPoco")]
    public class PetaPocoBenchmarks : BenchmarkBase
    {
        private Database _db, _dbFast;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _db = new Database(ConnectionString, "System.Data.SqlClient");
            _db.OpenSharedConnection();
            _dbFast = new Database(ConnectionString, "System.Data.SqlClient");
            _dbFast.OpenSharedConnection();
            _dbFast.EnableAutoSelect = false;
            _dbFast.EnableNamedParams = false;
            _dbFast.ForceDateTimesToUtc = false;
        }

        [Benchmark(Description = "Fetch<T>")]
        public Post Fetch()
        {
            Step();
            return _db.Fetch<Post>("SELECT * from Posts where Id=@0", i).First();
        }

        [Benchmark(Description = "Fetch<T> (Fast)")]
        public Post FetchFast()
        {
            Step();
            return _dbFast.Fetch<Post>("SELECT * from Posts where Id=@0", i).First();
        }
    }
}
