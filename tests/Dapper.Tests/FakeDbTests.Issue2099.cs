#if !NET481
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue2099Tests
{
    [Fact]
    public async Task UnbufferedAsyncMultiMapKeepsReaderAliveUntilEnumeration()
    {
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[]
        {
            new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2 },
        });
        connection.Open();

        var command = new CommandDefinition("select 1 as A, 2 as B", flags: CommandFlags.None);
        var results = await connection.QueryAsync<First, Second, int>(command, (a, b) => a.A + b.B, splitOn: "B");

        Assert.Equal(3, results.Single());
    }

    private sealed class First { public int A { get; set; } }
    private sealed class Second { public int B { get; set; } }
}
#endif
