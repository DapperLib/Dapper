#if !NET481
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue2240Tests
{
    [Fact]
    public async Task FirstAsyncMultiMapExecutionDoesNotRecordSyntheticCacheHit()
    {
        SqlMapper.PurgeQueryCache();
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[]
        {
            new Dictionary<string, object?>
            {
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3,
            },
        });
        connection.Open();

        await connection.QueryAsync<First, Second, Third, int>(
            "select 1 as A, 2 as B, 3 as C",
            (a, b, c) => a.A + b.B + c.C,
            splitOn: "B,C");

        var cached = SqlMapper.GetCachedSQL().ToList();
        Assert.NotEmpty(cached);
        Assert.Equal(0, cached.Single().Item3);
    }

    private sealed class First { public int A { get; set; } }
    private sealed class Second { public int B { get; set; } }
    private sealed class Third { public int C { get; set; } }
}
#endif
