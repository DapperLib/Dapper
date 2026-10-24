#if !NET481
using System.Collections.Generic;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue1900Tests
{
    [Fact]
    public void QuerySingleNullableStructMapsTheRow()
    {
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[]
        {
            new Dictionary<string, object?> { ["ID"] = 1, ["Name"] = "Something" },
        });
        connection.Open();

        var result = connection.QuerySingle<Foo?>("select ID, Name");

        Assert.True(result.HasValue);
        Assert.Equal(1, result.Value.ID);
        Assert.Equal("Something", result.Value.Name);
    }

    private struct Foo
    {
        public int ID { get; set; }
        public string? Name { get; set; }
    }
}
#endif
