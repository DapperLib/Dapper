#if !NET481
using System.Collections.Generic;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue1966Tests
{
    [Fact]
    public void ConstructorMappingConvertsNumericColumnToBoolean()
    {
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[]
        {
            new Dictionary<string, object?> { ["B"] = 1 },
        });
        connection.Open();

        var result = connection.QuerySingle<InConstructor>("select 1 as B");

        Assert.True(result.B);
    }

    private sealed record InConstructor(bool B);
}
#endif
