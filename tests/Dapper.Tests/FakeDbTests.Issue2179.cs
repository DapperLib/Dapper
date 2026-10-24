#if !NET481
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue2179Tests
{
    [Fact]
    public void QueryMultipleInvokesCustomParameterCallbacks()
    {
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[] { new Dictionary<string, object?> { ["Value"] = 1 } });
        connection.Open();
        var parameters = new CallbackParameters();

        using (var grid = connection.QueryMultiple("select 1", parameters))
        {
            Assert.Equal(1, grid.Read<int>().Single());
        }

        Assert.Equal(1, parameters.CompletedCalls);
    }

    private sealed class CallbackParameters : SqlMapper.IParameterCallbacks
    {
        public int CompletedCalls { get; private set; }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity) { }

        void SqlMapper.IParameterCallbacks.OnCompleted() => CompletedCalls++;
    }
}
#endif
