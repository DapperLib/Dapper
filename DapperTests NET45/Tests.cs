using System.Linq;
using Dapper;
using SqlMapper;

namespace DapperTests_NET45
{
    public class Tests
    {
        public void AsyncTestBasicStringUsage()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new {txt = "def"});
                var arr = query.Result.ToArray();
                arr.IsSequenceEqualTo(new[] {"abc", "def"});
            }
        }
        
        public void AsyncTestClassWithStringUsage()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });    
            }
        }
        
        class BasicType
        { 
            public string Value { get; set; }
        }
    }
}