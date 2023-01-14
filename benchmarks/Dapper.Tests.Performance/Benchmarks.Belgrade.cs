using BenchmarkDotNet.Attributes;
using Belgrade.SqlClient.SqlDb;
using Belgrade.SqlClient;
using System.ComponentModel;
using System.Threading.Tasks;

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

        [Benchmark(Description = "FirstOrDefault")]
        public Task<Post> FirstOrDefault()
        {
            Step();
            return _mapper.Sql("SELECT TOP 1 * FROM Posts WHERE Id = @Id").Param("Id", i).FirstOrDefault(
                reader => new Post
                    {
                        Id = reader.GetInt32(0),
                        Text = reader.GetString(1),
                        CreationDate = reader.GetDateTime(2),
                        LastChangeDate = reader.GetDateTime(3),

                        Counter1 = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                        Counter2 = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                        Counter3 = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                        Counter4 = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7),
                        Counter5 = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8),
                        Counter6 = reader.IsDBNull(9) ? null : (int?)reader.GetInt32(9),
                        Counter7 = reader.IsDBNull(10) ? null : (int?)reader.GetInt32(10),
                        Counter8 = reader.IsDBNull(11) ? null : (int?)reader.GetInt32(11),
                        Counter9 = reader.IsDBNull(12) ? null : (int?)reader.GetInt32(12),
                    });
        }
    }
}
