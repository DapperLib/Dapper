﻿using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using RepoDb;
using RepoDb.DbHelpers;
using RepoDb.DbSettings;
using RepoDb.StatementBuilders;

namespace Dapper.Tests.Performance
{
    [Description("RepoDB")]
    public class RepoDbBenchmarks : BenchmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            GlobalConfiguration.Setup().UseSqlServer();

            // We need this since benchmarks using System.Data.SqlClient
            var dbSetting = new SqlServerDbSetting();
            DbSettingMapper
                .Add<System.Data.SqlClient.SqlConnection>(dbSetting, true);
            DbHelperMapper
                .Add<System.Data.SqlClient.SqlConnection>(new SqlServerDbHelper(), true);
            StatementBuilderMapper
                .Add<System.Data.SqlClient.SqlConnection>(new SqlServerStatementBuilder(dbSetting), true);

            ClassMapper.Add<Post>("Posts");
        }

        [Benchmark(Description = "Query<T>")]
        public Post Query()
        {
            Step();
            return _connection.Query<Post>(i).First();
        }

        [Benchmark(Description = "QueryWhere<T>")]
        public Post QueryWhere()
        {
            Step();
            return _connection.Query<Post>(x => x.Id == i).First();
        }

        [Benchmark(Description = "QueryDynamic<T>")]
        public Post QueryDynamic()
        {
            Step();
            return _connection.Query<Post>(new { Id = i }).First();
        }

        [Benchmark(Description = "QueryField<T>")]
        public Post QueryField()
        {
            Step();
            return _connection.Query<Post>(new QueryField[] { new(nameof(Post.Id), i) }).First();
        }

        [Benchmark(Description = "ExecuteQuery<T>")]
        public Post ExecuteQuery()
        {
            Step();
            return _connection.ExecuteQuery<Post>("select * from Posts where Id = @Id", new { Id = i }).First();
        }
    }
}
