#if !NET462 // platform not supported exception
using System;
using System.Collections.Generic;
using System.IO;
using Snowflake.Data.Client;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.Tests
{
    public class SnowflakeTests
    {
        static readonly string? s_ConnectionString;
        static SnowflakeTests()
        {
            SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;

            try
            { // this *probably* won't exist (TODO: can we get a test account?)
                s_ConnectionString = File.ReadAllText(@"c:\Code\SnowflakeConnectionString.txt").Trim();
            } catch { }
        }

        public SnowflakeTests(ITestOutputHelper output)
            => Output = output;

        private ITestOutputHelper Output { get; }

        
        private static SnowflakeDbConnection GetConnection()
        {
            if (string.IsNullOrWhiteSpace(s_ConnectionString))
                Skip.Inconclusive("no snowflake connection-string");

            return new SnowflakeDbConnection
            {
                ConnectionString = s_ConnectionString
            };
        }

        [Fact]
        public void Connect()
        {
            using var connection = GetConnection();
            connection.Open();
        }


        [Fact]
        public void BasicQuery()
        {

            using var connection = GetConnection();
            var nations = connection.Query<Nation>(@"SELECT * FROM NATION").AsList();
            Assert.NotEmpty(nations);
            Output.WriteLine($"nations: {nations.Count}");
            foreach (var nation in nations)
            {
                Output.WriteLine($"{nation.N_NATIONKEY}: {nation.N_NAME} (region: {nation.N_REGIONKEY}), {nation.N_COMMENT}");
            }
        }

        [Fact]
        public void ParameterizedQuery()
        {
            using var connection = GetConnection();
            const int region = 1;
            var nations = connection.Query<Nation>(@"SELECT * FROM NATION WHERE N_REGIONKEY=?region?", new { region }).AsList();
            Assert.NotEmpty(nations);
            Output.WriteLine($"nations: {nations.Count}");
            foreach (var nation in nations)
            {
                Output.WriteLine($"{nation.N_NATIONKEY}: {nation.N_NAME} (region: {nation.N_REGIONKEY}), {nation.N_COMMENT}");
            }
        }

        public class Nation
        {
            public int N_NATIONKEY { get; set; }
            public string? N_NAME{ get; set; }
            public int N_REGIONKEY { get; set; }
            public string? N_COMMENT { get; set; }
        }
    }
}
#endif
