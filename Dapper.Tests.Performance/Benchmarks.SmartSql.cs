using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using SmartSql;
using SmartSql.DbSession;
using System.ComponentModel;
using SmartSql.ConfigBuilder;
using SmartSql.Data;
using SmartSql.DataSource;

namespace Dapper.Tests.Performance
{
    [Description("SmartSql")]
    public class SmartSqlBenchmarks : BenchmarkBase
    {
        private IDbSession _dbSession;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            var smartSqlBuilder = new SmartSqlBuilder()
                .UseProperties(new[]
                {
                    new KeyValuePair<string, string>("ConnectionString", _connection.ConnectionString)
                })
                .UseXmlConfig(ResourceType.File, "SmartSql/SmartSqlMapConfig.xml")
                .UseCache(false).Build();
            _dbSession = smartSqlBuilder.GetDbSessionFactory().Open(_connection.ConnectionString);
        }

        [Benchmark(Description = "QuerySingleFromXml")]
        public Post QuerySingleFromXml()
        {
            Step();
            return _dbSession.QuerySingle<Post>(new RequestContext
            {
                Scope = nameof(Post), SqlId = "GetById", Request = new {Id = i}
            });
        }

        [Benchmark(Description = "QuerySingle")]
        public Post QuerySingle()
        {
            Step();
            return _dbSession.QuerySingle<Post>(new RequestContext
            {
                RealSql = "select * from Posts where Id = @Id", Request = new {Id = i}
            });
        }

        [Benchmark(Description = "GetById")]
        public Post GetById()
        {
            Step();
            return _dbSession.GetById<Post, int>(i);
        }

        [Benchmark(Description = "QuerySingleStrongRequest")]
        public Post QuerySingleStrongRequest()
        {
            Step();
            return _dbSession.QuerySingle<Post>(new RequestContext<QueryRequest>
            {
                RealSql = "select * from Posts where Id = @Id", Request = new QueryRequest {Id = i}
            });
        }

        [Benchmark(Description = "QuerySingleSqlParameterCollection")]
        public Post QuerySingleSqlParameterCollection()
        {
            Step();
            SqlParameterCollection sqlParameterCollection = new SqlParameterCollection();
            sqlParameterCollection.Add("Id", new SqlParameter("Id", i));
            return _dbSession.QuerySingle<Post>(new RequestContext
            {
                RealSql = "select * from Posts where Id = @Id", Request = sqlParameterCollection
            });
        }

        [Benchmark(Description = "QuerySingleSqlParameterCollection<dynamic>")]
        public dynamic QuerySingleSqlParameterCollectionDynamic()
        {
            Step();
            SqlParameterCollection sqlParameterCollection = new SqlParameterCollection();
            sqlParameterCollection.Add("Id", new SqlParameter("Id", i));
            return _dbSession.QuerySingle<dynamic>(new RequestContext
            {
                RealSql = "select * from Posts where Id = @Id", Request = sqlParameterCollection
            });
        }

        [Benchmark(Description = "QuerySingleStrongRequest<dynamic>")]
        public dynamic QuerySingleStrongRequestDynamic()
        {
            Step();
            return _dbSession.QuerySingle<dynamic>(new RequestContext<QueryRequest>
            {
                RealSql = "select * from Posts where Id = @Id", Request = new QueryRequest {Id = i}
            });
        }

        public class QueryRequest
        {
            public int Id { get; set; }
        }
    }
}
