using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.Tests;

public class EmptyMultiMapTests
{
    internal class Parent
    {
        public string Id { get; }
        public Child? Child { get; set; }
        public Parent(string id) => Id = id;
    }
    internal class Child { public int ChildId { get; set; } public string? Name { get; set; } }
    internal static readonly Type[] Types = [typeof(Parent), typeof(Child)];
    internal const string Select = "select Id, 1 as ChildId, 'child' as Name from Test order by rowid";
    internal static Parent Join(Parent parent, Child child) { parent.Child = child; return parent; }
    internal static Parent Join(object[] values) => Join((Parent)values[0], (Child)values[1]);

    internal static async Task<List<Parent>> Read(DbConnection connection, int path, string sql, object? args = null, string splitOn = "ChildId")
    {
        bool buffered = path < 4;
        IEnumerable<Parent> rows;
        if (path is 0 or 4) rows = connection.Query<Parent, Child, Parent>(sql, Join, args, buffered: buffered, splitOn: splitOn);
        else if (path is 1 or 6) rows = await connection.QueryAsync<Parent, Child, Parent>(sql, Join, args, buffered: buffered, splitOn: splitOn);
        else if (path is 2 or 5) rows = connection.Query(sql, Types, Join, args, buffered: buffered, splitOn: splitOn);
        else rows = await connection.QueryAsync(sql, Types, Join, args, buffered: buffered, splitOn: splitOn);
        return rows.ToList();
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
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public async Task EmptyAndPopulatedResults_Issue1275(int path, bool initiallyClosed)
    {
        string cs = "Data Source=" + Guid.NewGuid() + ";Mode=Memory;Cache=Shared";
        using var keeper = new SqliteConnection(cs);
        keeper.Open();
        keeper.Execute("create table Test (Id BLOB)");
        using var connection = new SqliteConnection(cs);
        if (!initiallyClosed) connection.Open();
        foreach (int count in new[] { 0, 1, 3, 0, 2 })
        {
            keeper.Execute("delete from Test");
            var expected = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
            foreach (var id in expected) keeper.Execute("insert into Test values (@id)", new { id });
            var rows = await Read(connection, path, Select);
            Assert.Equal(expected, rows.Select(row => Guid.Parse(row.Id)));
            Assert.All(rows, row => { Assert.Equal(1, row.Child!.ChildId); Assert.Equal("child", row.Child.Name); });
            Assert.Equal(initiallyClosed ? ConnectionState.Closed : ConnectionState.Open, connection.State);
        }
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 2)]
    public void GridReaderKeepsNextResult_Issue1275(bool types, bool buffered, int count)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("create table Test (Id BLOB)");
        for (int i = 0; i < count; i++) connection.Execute("insert into Test values (@id)", new { id = Guid.NewGuid() });
        using var grid = connection.QueryMultiple(Select + "; select 42");
        var rows = types ? grid.Read(Types, Join, "ChildId", buffered) : grid.Read<Parent, Child, Parent>(Join, "ChildId", buffered);
        Assert.Equal(count, rows.Count());
        Assert.Equal(42, grid.ReadSingle<int>());
        Assert.True(grid.IsConsumed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EarlyDisposalClosesConnection_Issue1275(bool types)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        const string sql = "select 'first' as Id, 1 as ChildId, 'child' as Name union all select 'second', 2, 'other'";
        var rows = types ? connection.Query(sql, Types, Join, buffered: false, splitOn: "ChildId")
            : connection.Query<Parent, Child, Parent>(sql, Join, buffered: false, splitOn: "ChildId");
        Assert.Equal("first", Assert.Single(rows.Take(1)).Id);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task PopulatedResultStillValidatesSplitOn_Issue1275(int path)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        const string sql = "select 'parent' as Id, 1 as ChildId, 'child' as Name";
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Read(connection, path, sql, splitOn: "MissingColumn"));
        Assert.Contains("MissingColumn", error.Message);
    }
}

public sealed class SystemSqlClientEmptyMultiMapTests : EmptyMultiMapSqlServerTests<SystemSqlClientProvider> { }
public sealed class MicrosoftSqlClientEmptyMultiMapTests : EmptyMultiMapSqlServerTests<MicrosoftSqlClientProvider> { }

public abstract class EmptyMultiMapSqlServerTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
{
    private class Target { public int Value { get; set; } = -1; }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task CompletionAndLateErrors_Issue1275(int path)
    {
        foreach (int result in new[] { 0, 1, 2 })
        {
            var target = new Target();
            var args = new DynamicParameters(new { unused = 1 });
            args.Output(target, item => item.Value);
            string sql = "set @Value = 42;";
            if (result == 1) sql += "select cast(null as varbinary(16)) as Id, 1 as ChildId, 'child' as Name where 1 = 0;";
            if (result == 2) sql += "select 'parent' as Id, 1 as ChildId, 'child' as Name;";
            var rows = await EmptyMultiMapTests.Read(connection, path, sql, args);
            Assert.Equal(result == 2 ? 1 : 0, rows.Count);
            Assert.Equal(42, target.Value);
        }
        const string failingSql = "select cast(null as varbinary(16)) as Id, 1 as ChildId, 'child' as Name where 1 = 0; throw 50000, 'Issue1275-late-error', 1;";
        var error = await Assert.ThrowsAnyAsync<DbException>(() => EmptyMultiMapTests.Read(connection, path, failingSql));
        Assert.Contains("Issue1275-late-error", error.Message);
    }
}
