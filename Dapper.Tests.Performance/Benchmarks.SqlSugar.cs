using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using SqlSugar;

namespace Dapper.Tests.Performance
{
    [Description("SqlSugar")]
    public class SqlSugarBenchmarks : BenchmarkBase
    {
        private SqlSugarClient _db;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = _connection.ConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = false,
                InitKeyType = InitKeyType.Attribute
            });
        }

        [Benchmark(Description = "First")]
        public Post First()
        {
            Step();
            return _db.Queryable<Post>().Where(post => post.Id == i).First();
        }
    }
}
