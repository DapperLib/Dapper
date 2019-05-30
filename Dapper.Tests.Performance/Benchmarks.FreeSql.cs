using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Dapper.Tests.Performance
{
    [Description("FreeSql")]
    public class FreeSqlBenchmarks : BenchmarkBase
    {
        private IFreeSql _fsql;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer,
                    _connection.ConnectionString)
                .Build();
        }

        [Benchmark(Description = "First")]
        public Post First()
        {
            Step();
            return _fsql.Queryable<Post>().Where(post => post.Id == i).First();
        }
        [Benchmark(Description = "FromSqlFirst")]
        public Post FromSqlFirst()
        {
            Step();
          return  _fsql.Ado.Query<Post>("select * from Posts where Id = @Id", new {Id = i}).First();
        }
        [Benchmark(Description = "FromSqlFirstDynamic")]
        public dynamic FromSqlFirstDynamic()
        {
            Step();
            return  _fsql.Ado.Query<dynamic>("select * from Posts where Id = @Id", new {Id = i}).First();
        }
    }
}
