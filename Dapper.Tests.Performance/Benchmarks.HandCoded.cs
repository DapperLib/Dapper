using BenchmarkDotNet.Attributes;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Dapper.Tests.Performance
{
    public class HandCodedBenchmarks : BenchmarkBase
    {
        private SqlCommand _postCommand;
        private SqlParameter _idParam;
#if !NETCOREAPP1_0
        private DataTable _table;
#endif

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _postCommand = new SqlCommand()
            {
                Connection = _connection,
                CommandText = @"select Id, [Text], [CreationDate], LastChangeDate, 
                Counter1,Counter2,Counter3,Counter4,Counter5,Counter6,Counter7,Counter8,Counter9 from Posts where Id = @Id"
            };
            _idParam = _postCommand.Parameters.Add("@Id", SqlDbType.Int);
#if !NETCOREAPP1_0
            _table = new DataTable
            {
                Columns =
                    {
                        {"Id", typeof (int)},
                        {"Text", typeof (string)},
                        {"CreationDate", typeof (DateTime)},
                        {"LastChangeDate", typeof (DateTime)},
                        {"Counter1", typeof (int)},
                        {"Counter2", typeof (int)},
                        {"Counter3", typeof (int)},
                        {"Counter4", typeof (int)},
                        {"Counter5", typeof (int)},
                        {"Counter6", typeof (int)},
                        {"Counter7", typeof (int)},
                        {"Counter8", typeof (int)},
                        {"Counter9", typeof (int)},
                    }
            };
#endif
        }

        [Benchmark(Description = "SqlCommand", Baseline = true)]
        public Post SqlCommand()
        {
            Step();
            _idParam.Value = i;

            using (var reader = _postCommand.ExecuteReader())
            {
                reader.Read();
                var post = new Post();
                post.Id = reader.GetInt32(0);
                post.Text = reader.GetNullableString(1);
                post.CreationDate = reader.GetDateTime(2);
                post.LastChangeDate = reader.GetDateTime(3);

                post.Counter1 = reader.GetNullableValue<int>(4);
                post.Counter2 = reader.GetNullableValue<int>(5);
                post.Counter3 = reader.GetNullableValue<int>(6);
                post.Counter4 = reader.GetNullableValue<int>(7);
                post.Counter5 = reader.GetNullableValue<int>(8);
                post.Counter6 = reader.GetNullableValue<int>(9);
                post.Counter7 = reader.GetNullableValue<int>(10);
                post.Counter8 = reader.GetNullableValue<int>(11);
                post.Counter9 = reader.GetNullableValue<int>(12);
                return post;
            }
        }

        [Benchmark(Description = "DataTable")]
        public dynamic DataTableDynamic()
        {
            Step();
            _idParam.Value = i;
            var values = new object[13];
            using (var reader = _postCommand.ExecuteReader())
            {
                reader.Read();
                reader.GetValues(values);
                _table.Rows.Add(values);
                return _table.Rows[_table.Rows.Count - 1];
            }
        }
    }
}
