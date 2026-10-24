#if !NET481
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue1298Tests
{
    [Fact]
    public void CustomHandlerOverridesBuiltInTypeMap()
    {
        SqlMapper.ResetTypeHandlers();
        var handler = new StringHandler();
        SqlMapper.AddTypeHandler(handler);
        try
        {
            using var connection = new fakeDbConnection(new FakeDataStore());
            connection.EnqueueNonQueryResult(1);
            connection.Open();

            connection.Execute("select @value", new { value = "hello" });

            Assert.True(handler.Called);
        }
        finally
        {
            SqlMapper.ResetTypeHandlers();
        }
    }

    private sealed class StringHandler : SqlMapper.TypeHandler<string>
    {
        public bool Called { get; private set; }

        public override string Parse(object value) => (string)value;

        public override void SetValue(IDbDataParameter parameter, string? value)
        {
            Called = true;
            parameter.DbType = DbType.String;
            parameter.Value = value;
        }
    }
}
#endif
