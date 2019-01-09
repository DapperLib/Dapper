using BenchmarkDotNet.Attributes;
using Belgrade.SqlClient.SqlDb;
using Belgrade.SqlClient;
using System.ComponentModel;

namespace Dapper.Tests.Performance
{
    [Description("Belgrade")]
    public class BelgradeBenchmarks : BenchmarkBase
    {
        private QueryMapper _mapper;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _mapper = new QueryMapper(ConnectionString);
        }

        [Benchmark(Description = "ExecuteReader")]
        public Post ExecuteReader()
        {
            Step();
            var post = new Post();
            _mapper.Sql("SELECT TOP 1 * FROM Posts WHERE Id = @Id").Param("Id", i).Map(
                        reader =>
                        {
                            post.Id = reader.GetInt32(0);
                            post.Text = reader.GetString(1);
                            post.CreationDate = reader.GetDateTime(2);
                            post.LastChangeDate = reader.GetDateTime(3);

                            post.Counter1 = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4);
                            post.Counter2 = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5);
                            post.Counter3 = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6);
                            post.Counter4 = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7);
                            post.Counter5 = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8);
                            post.Counter6 = reader.IsDBNull(9) ? null : (int?)reader.GetInt32(9);
                            post.Counter7 = reader.IsDBNull(10) ? null : (int?)reader.GetInt32(10);
                            post.Counter8 = reader.IsDBNull(11) ? null : (int?)reader.GetInt32(11);
                            post.Counter9 = reader.IsDBNull(12) ? null : (int?)reader.GetInt32(12);
                        });
            return post;
        }
    }
}
