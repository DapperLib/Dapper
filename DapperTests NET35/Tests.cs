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
    }
}
