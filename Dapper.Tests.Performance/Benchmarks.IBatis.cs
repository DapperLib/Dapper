using System;
using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using IBatisNet.DataMapper;
using IBatisNet.DataMapper.Configuration;

namespace Dapper.Tests.Performance
{
    [Description("IBatis")]
    public class IBatisBenchmarks : BenchmarkBase
    {
        private ISqlMapper sqlMapper;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();

            sqlMapper = new DomSqlMapBuilder().Configure("./IBatis/sqlMap.config");
        }

        [Benchmark(Description = "QueryForObject")]
        public Post QueryForObject()
        {
            Step();
            return sqlMapper.QueryForObject<Post>("Post.GetById", i);
        }
    }
}
