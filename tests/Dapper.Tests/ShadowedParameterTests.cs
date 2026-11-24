using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientShadowedParameterTests : ShadowedParameterTests<SystemSqlClientProvider> { }
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientShadowedParameterTests : ShadowedParameterTests<MicrosoftSqlClientProvider> { }

    public abstract class ShadowedParameterTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ShadowedCustomParameter_Issue381(bool useAsync, bool template)
        {
            object args = new CustomParameters();
            if (template) args = new DynamicParameters(args);
            const string sql = "select @Value + @Other";
            var value = useAsync ? await connection.QuerySingleAsync<string>(sql, args) : connection.QuerySingle<string>(sql, args);
            Assert.Equal("derived!", value);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ShadowedScalarParameter_Issue381(bool useAsync, bool template)
        {
            object args = new ScalarParameters();
            if (template) args = new DynamicParameters(args);
            const string sql = "select @Value";
            var value = useAsync ? await connection.ExecuteScalarAsync<int>(sql, args) : connection.ExecuteScalar<int>(sql, args);
            Assert.Equal(42, value);
        }

        [Theory]
        [InlineData(0, "Value", "base")]
        [InlineData(1, "Value", "derived")]
        [InlineData(2, "Value", 42)]
        [InlineData(3, "value", 43)]
        [InlineData(4, "VALUE", 44)]
        [InlineData(5, "Value", "same-type")]
        public void ParameterDiscovery_UsesMostDerivedProperty_Issue381(int scenario, string name, object expected)
        {
            object args = scenario switch
            {
                0 => new InheritedParameters(),
                1 => new CustomParameters(),
                2 => new ScalarParameters(),
                3 => new CasedParameters(),
                4 => new LeafParameters(),
                _ => new SameTypeParameters()
            };
            using var cnn = GetClosedConnection();
            using var command = cnn.CreateCommand();
            command.CommandText = "select @Value + @Other";
            var identity = new SqlMapper.Identity(command.CommandText, CommandType.Text, cnn, null, args.GetType());
            SqlMapper.CreateParamInfoGenerator(identity, false, true)(command, args);

            Assert.Equal(2, command.Parameters.Count);
            var value = Assert.IsAssignableFrom<IDbDataParameter>(command.Parameters[name]);
            Assert.Equal(name, value.ParameterName);
            Assert.Equal(expected, value.Value);
            var other = Assert.IsAssignableFrom<IDbDataParameter>(command.Parameters["Other"]);
            Assert.Equal("!", other.Value);
            if (scenario == 1) Assert.Equal(DbType.AnsiString, value.DbType);
        }

        [Fact]
        public void ParameterDiscovery_PreservesSameLevelCaseDistinctProperties_Issue381()
        {
            var args = new SameLevelParameters();
            using var cnn = GetClosedConnection();
            using var command = cnn.CreateCommand();
            command.CommandText = "select @Value, @value, @Other";
            var identity = new SqlMapper.Identity(command.CommandText, CommandType.Text, cnn, null, args.GetType());
            SqlMapper.CreateParamInfoGenerator(identity, false, true)(command, args);

            Assert.Equal(3, command.Parameters.Count);
            var parameters = new System.Collections.Generic.Dictionary<string, object?>();
            foreach (IDbDataParameter parameter in command.Parameters) parameters.Add(parameter.ParameterName, parameter.Value);
            Assert.Equal(1, parameters["Value"]);
            Assert.Equal(2, parameters["value"]);
        }

        [Fact]
        public void ParameterDiscovery_PreservesConstructorOrder_Issue381()
        {
            var args = new OrderedParameters("last", 42);
            using var cnn = GetClosedConnection();
            using var command = cnn.CreateCommand();
            command.CommandText = "select @Z, @Value";
            var identity = new SqlMapper.Identity(command.CommandText, CommandType.Text, cnn, null, args.GetType());
            SqlMapper.CreateParamInfoGenerator(identity, false, true)(command, args);

            Assert.Equal(2, command.Parameters.Count);
            Assert.Equal("Z", ((IDbDataParameter)command.Parameters[0]).ParameterName);
            Assert.Equal("Value", ((IDbDataParameter)command.Parameters[1]).ParameterName);
            Assert.Equal(42, ((IDbDataParameter)command.Parameters[1]).Value);
        }

        [Fact]
        public void ShadowedCustomParameterWithoutOtherProperties_Issue381()
        {
            Assert.Equal("derived", connection.QuerySingle<string>("select @Value", new CustomOnlyParameters()));
        }

        [Fact]
        public void LiteralReplacement_UsesMostDerivedProperty_Issue381()
        {
            var args = new ScalarParameters();
            using var cnn = GetClosedConnection();
            using var command = cnn.CreateCommand();
            command.CommandText = "select {=Value}";
            var identity = new SqlMapper.Identity(command.CommandText, CommandType.Text, cnn, null, args.GetType());
            SqlMapper.CreateParamInfoGenerator(identity, false, true)(command, args);

            Assert.Equal("select 42", command.CommandText);
            Assert.Empty(command.Parameters);
        }

        private class BaseParameters
        {
            public string Value { get; set; } = "base";
            public string Other { get; set; } = "!";
        }
        private class InheritedParameters : BaseParameters { }
        private class CustomParameters : BaseParameters
        {
            public new DbString Value { get; set; } = new DbString { Value = "derived", IsAnsi = true };
        }
        private class ScalarParameters : BaseParameters { public new int Value { get; set; } = 42; }
        private class CasedParameters : BaseParameters { public int value { get; set; } = 43; }
        private class LeafParameters : CasedParameters { public int VALUE { get; set; } = 44; }
        private class SameTypeParameters : BaseParameters { public new string Value { get; set; } = "same-type"; }
        private class SameLevelParameters : BaseParameters
        {
            public new int Value { get; set; } = 1;
            public int value { get; set; } = 2;
        }
        private class CustomOnlyBase { public string Value { get; set; } = "base"; }
        private class CustomOnlyParameters : CustomOnlyBase
        {
            public new DbString Value { get; set; } = new DbString { Value = "derived", IsAnsi = true };
        }
        private class OrderedBase { public string Value { get; set; } = "base"; }
        private class OrderedParameters : OrderedBase
        {
            public string Z { get; }
            public new int Value { get; }
            public OrderedParameters(string z, int value) { Z = z; Value = value; }
        }
    }
}
