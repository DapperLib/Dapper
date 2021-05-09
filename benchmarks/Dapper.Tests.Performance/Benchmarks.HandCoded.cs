using BenchmarkDotNet.Attributes;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;

namespace Dapper.Tests.Performance
{
    [Description("Hand Coded")]
    public class HandCodedBenchmarks : BenchmarkBase
    {
        private SqlCommand _postCommand;
        private SqlParameter _idParam;
        private DataTable _table;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _postCommand = new SqlCommand("select Top 1 * from Posts where Id = @Id", _connection);
            _idParam = _postCommand.Parameters.Add("@Id", SqlDbType.Int);
            _postCommand.Prepare();
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
        }

        [Benchmark(Description = "SqlCommand")]
        public Post SqlCommand()
        {
            Step();
            _idParam.Value = i;

            using (var reader = _postCommand.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow))
            {
                reader.Read();
                return new Post
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetNullableString(1),
                    CreationDate = reader.GetDateTime(2),
                    LastChangeDate = reader.GetDateTime(3),

                    Counter1 = reader.GetNullableValue<int>(4),
                    Counter2 = reader.GetNullableValue<int>(5),
                    Counter3 = reader.GetNullableValue<int>(6),
                    Counter4 = reader.GetNullableValue<int>(7),
                    Counter5 = reader.GetNullableValue<int>(8),
                    Counter6 = reader.GetNullableValue<int>(9),
                    Counter7 = reader.GetNullableValue<int>(10),
                    Counter8 = reader.GetNullableValue<int>(11),
                    Counter9 = reader.GetNullableValue<int>(12)
                };
            }
        }

        [Benchmark(Description = "DataTable")]
        public dynamic DataTableDynamic()
        {
            Step();
            _idParam.Value = i;
            _table.Rows.Clear();
            var values = new object[13];
            using (var reader = _postCommand.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow))
            {
                reader.Read();
                reader.GetValues(values);
                return _table.Rows.Add(values);
            }
        }
    }
}
