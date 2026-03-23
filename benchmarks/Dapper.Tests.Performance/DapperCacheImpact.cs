using System.ComponentModel;
using BenchmarkDotNet.Attributes;

namespace Dapper.Tests.Performance
{
    [Description("Dapper cache impact")]
    [MemoryDiagnoser]
    public class DapperCacheImpact : BenchmarkBase
    {
        [GlobalSetup]
        public void Setup() => BaseSetup();

        private readonly object args = new { Id = 42, Name = "abc" };

        public class Foo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // note: custom BDN setup means [Params] is awkward; unroll manually instead
        [Benchmark]
        public void ExecuteNoParameters_Cache() => _connection.Execute(new CommandDefinition("select '42' as Id, 'abc' as Name", flags: CommandFlags.None));
        [Benchmark]
        public void ExecuteParameters_Cache() => _connection.Execute(new CommandDefinition("select @id as Id, @name as Name", args, flags: CommandFlags.None));
        [Benchmark]
        public void QueryFirstNoParameters_Cache() => _connection.QueryFirst<Foo>(new CommandDefinition("select '42' as Id, 'abc' as Name", flags: CommandFlags.None));
        [Benchmark]
        public void QueryFirstParameters_Cache() => _connection.QueryFirst<Foo>(new CommandDefinition("select @id as Id, @name as Name", args, flags: CommandFlags.None));
        [Benchmark]
        public void ExecuteNoParameters_NoCache() => _connection.Execute(new CommandDefinition("select '42' as Id, 'abc' as Name", flags: CommandFlags.NoCache));
        [Benchmark]
        public void ExecuteParameters_NoCache() => _connection.Execute(new CommandDefinition("select @id as Id, @name as Name", args, flags: CommandFlags.NoCache));
        [Benchmark]
        public void QueryFirstNoParameters_NoCache() => _connection.QueryFirst<Foo>(new CommandDefinition("select '42' as Id, 'abc' as Name", flags: CommandFlags.NoCache));
        [Benchmark]
        public void QueryFirstParameters_NoCache() => _connection.QueryFirst<Foo>(new CommandDefinition("select @id as Id, @name as Name", args, flags: CommandFlags.NoCache));
    }
}
