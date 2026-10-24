#if !NET481
using System;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue1234Tests
{
    [Fact]
    public void NullValueIsPassedToCustomTypeHandler()
    {
        SqlMapper.ResetTypeHandlers();
        var handler = new NullAwareHandler();
        SqlMapper.AddTypeHandler(handler);
        try
        {
            using var connection = new fakeDbConnection(new FakeDataStore());
            connection.EnqueueNonQueryResult(1);
            connection.Open();

            connection.Execute("select @value", new { value = (NullAwareValue?)null });

            Assert.True(handler.Called);
            Assert.Null(handler.Value);
            Assert.Equal(DbType.Binary, handler.LastParameter!.DbType);
            Assert.Equal(DBNull.Value, handler.LastParameter.Value);
        }
        finally
        {
            SqlMapper.ResetTypeHandlers();
        }
    }

    private sealed class NullAwareValue;

    private sealed class NullAwareHandler : SqlMapper.TypeHandler<NullAwareValue>
    {
        public bool Called { get; private set; }
        public NullAwareValue? Value { get; private set; }
        public IDbDataParameter? LastParameter { get; private set; }

        public override NullAwareValue? Parse(object value) => null;

        public override void SetValue(IDbDataParameter parameter, NullAwareValue? value)
        {
            Called = true;
            Value = value;
            LastParameter = parameter;
            parameter.DbType = DbType.Binary;
            parameter.Value = DBNull.Value;
        }
    }
}
#endif
