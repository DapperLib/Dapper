using System.Data.SqlClient;
using System.Linq;
using Dapper;
using SqlMapper;
using System.Data;

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
            var oldMatch = Dapper.DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;
                var arr = connection.Query<BasicType>("select 'abc' as [Value], '123' as [Another_Value] union all select @txt, @txt2", new { txt = "def", txt2 = "456" }).ToArray();
                arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });
                arr.Select(x => x.AnotherValue).IsSequenceEqualTo(new[] { "123", "456" });
            } finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = oldMatch;
            }
        }
        class BasicType
        { 
            public string Value { get; set; }
            public string AnotherValue { get; set; }
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

        public void TestMultiple()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select 3", null, CommandType.Text))
            {
                int i = grid.Read<int>().Single();
                int j = grid.Read<int>().Single();
                int k = grid.Read<int>().Single();
                i.Equals(1);
                j.Equals(2);
                k.Equals(3);
            }
        }

    }
}
