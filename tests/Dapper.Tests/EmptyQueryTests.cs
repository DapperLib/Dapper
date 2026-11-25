using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.Tests;

public class EmptyQueryTests
{
    internal class Row
    {
        public string Id { get; }
        public Row(string id) => Id = id;
    }

    internal static async Task<List<T>> Read<T>(DbConnection connection, int path, string sql)
    {
        if (path < 2) return connection.Query<T>(sql, buffered: path == 0).ToList();
        if (path < 4)
        {
            var command = new CommandDefinition(sql, flags: path == 2 ? CommandFlags.Buffered : CommandFlags.None);
            return (await connection.QueryAsync<T>(command)).ToList();
        }
#if NET5_0_OR_GREATER
        var rows = new List<T>();
        await foreach (var row in connection.QueryUnbufferedAsync<T>(sql)) rows.Add(row);
        return rows;
#else
        throw new NotSupportedException();
#endif
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
#if NET5_0_OR_GREATER
    [InlineData(4, false)]
    [InlineData(4, true)]
#endif
    public async Task EmptyAndPopulatedResults_Issue1836(int path, bool initiallyClosed)
    {
        string connectionString = "Data Source=" + Guid.NewGuid() + ";Mode=Memory;Cache=Shared";
        using var keeper = new SqliteConnection(connectionString);
        keeper.Open();
        keeper.Execute("create table Test (Id BLOB)");
        using var connection = new SqliteConnection(connectionString);
        if (!initiallyClosed) connection.Open();
        foreach (int count in new[] { 0, 1, 3, 0, 2 })
        {
            keeper.Execute("delete from Test");
            var expected = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
            foreach (var id in expected) keeper.Execute("insert into Test values (@id)", new { id });
            var rows = await Read<Row>(connection, path, "select Id as id from Test order by rowid");
            Assert.Equal(expected, rows.Select(row => Guid.Parse(row.Id)));
            Assert.Equal(initiallyClosed ? ConnectionState.Closed : ConnectionState.Open, connection.State);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
#if NET5_0_OR_GREATER
    [InlineData(4)]
#endif
    public async Task IncompatiblePopulatedResultStillThrows_Issue1836(int path)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("create table Test (Id BLOB)");
        connection.Execute("insert into Test values (@id)", new { id = new byte[] { 1, 2 } });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Read<Row>(connection, path, "select Id as id from Test"));
        Assert.Contains("System.Byte[]", error.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
#if NET5_0_OR_GREATER
    [InlineData(4)]
#endif
    public async Task EarlyDisposalClosesConnection_Issue1836(int path)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        const string sql = "select 'first' as id union all select 'second'";
        if (path == 1)
        {
            Assert.Equal("first", Assert.Single(connection.Query<Row>(sql, buffered: false).Take(1)).Id);
        }
        else if (path == 3)
        {
            var rows = await connection.QueryAsync<Row>(new CommandDefinition(sql, flags: CommandFlags.None));
            Assert.Equal("first", Assert.Single(rows.Take(1)).Id);
        }
#if NET5_0_OR_GREATER
        else
        {
            await foreach (var row in connection.QueryUnbufferedAsync<Row>(sql))
            {
                Assert.Equal("first", row.Id);
                break;
            }
        }
#endif
        Assert.Equal(ConnectionState.Closed, connection.State);
    }
}

public sealed class SystemSqlClientEmptyQueryTests : EmptyQuerySqlServerTests<SystemSqlClientProvider> { }
public sealed class MicrosoftSqlClientEmptyQueryTests : EmptyQuerySqlServerTests<MicrosoftSqlClientProvider> { }

public abstract class EmptyQuerySqlServerTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
#if NET5_0_OR_GREATER
    [InlineData(4)]
#endif
    public async Task EmptyResultStillReportsLaterSqlError_Issue1836(int path)
    {
        const string sql = "select cast(null as varbinary(16)) as id where 1 = 0; throw 50000, 'Issue1836-late-error', 1;";
        var error = await Assert.ThrowsAnyAsync<DbException>(() => EmptyQueryTests.Read<EmptyQueryTests.Row>(connection, path, sql));
        Assert.Contains("Issue1836-late-error", error.Message);
    }
}
