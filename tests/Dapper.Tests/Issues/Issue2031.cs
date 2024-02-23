using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.Tests.Issues;

[Collection("DecimalTests")]
public sealed class SystemSqlClientIssue2031(ITestOutputHelper log) : Issue2031<SystemSqlClientProvider>(log) { }
#if MSSQLCLIENT
[Collection("DecimalTests")]
public sealed class MicrosoftSqlClientIssue2031(ITestOutputHelper log) : Issue2031<MicrosoftSqlClientProvider>(log) { }
#endif
public abstract class Issue2031<TProvider>(ITestOutputHelper Log) : TestBase<TProvider> where TProvider : DatabaseProvider
{
    private async Task<DbConnection> Init()
    {
        var conn = GetOpenConnection();
        await conn.ExecuteAsync("""
            create table #foo (id int identity(1,1), [name] nvarchar(200));
            insert #foo ([name]) values ('abc'), ('def'), ('ghi');
            SET STATISTICS XML ON;
            """);
        return conn;
    }
    [Fact]
    public async Task ExecuteViaAdoNet()
    {
        using var conn = await Init();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "select top 1 [name] from #foo";
        cmd.CommandType = CommandType.Text;
        using var reader = await cmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync(), "should have first row");
        var name = reader.GetString(0);
        Log.WriteLine(name);
        Assert.False(await reader.ReadAsync(), "should not have second row");

        Assert.True(await reader.NextResultAsync(), "should have second result-set");
        Assert.True(await reader.ReadAsync(), "should have query plan");
        string plan = reader.GetString(0);
        Log.WriteLine(plan);
        Assert.False(await reader.ReadAsync(), "should not have second row of query plan");

        Assert.False(await reader.NextResultAsync(), "should not have third result-set");
    }

    [Fact]
    public async Task ExecuteViaDapper()
    {
        using var conn = await Init();

        using (var multi = await conn.QueryMultipleAsync("select top 1 [name] from #foo"))
        {
            string name= Assert.Single(await multi.ReadAsync<string>());
            Log.WriteLine(name);
            string plan = await multi.ReadSingleAsync<string>();
            Log.WriteLine(plan);
        }
    }
}
