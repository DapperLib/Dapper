using System.Data.SqlClient;
using System.Linq;
using Dapper;
using SqlMapper;

namespace DapperTests_NET35
{
    public class Tests
    {
        SqlConnection connection = Program.GetOpenConnection();

        public void TestBasicStringUsage()
        {
            var arr = connection.Query<string>("select 'abc' as [Value] union all select @txt", new {txt = "def"}).ToArray();
            arr.IsSequenceEqualTo(new[] { "abc", "def" });
        }
        public void TestClassWithStringUsage()
        {
            var arr = connection.Query<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" }).ToArray();
            arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });
        }
        class BasicType
        { 
            public string Value { get; set; }
        }

        public void TestDynamicSimulatedQuery() {
            var rows = connection.Query("select 1 A, 2 B union all select 3, 4", null).ToList();

            ((int)rows[0]["A"])
                .IsEqualTo(1);

            ((int)rows[0]["B"])
                .IsEqualTo(2);

            ((int)rows[1]["A"])
                .IsEqualTo(3);

            ((int)rows[1]["B"])
                .IsEqualTo(4);
        }

    }
}
