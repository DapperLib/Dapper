using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.Tests;

public class DynamicRowDictionaryTests
{
    private static IDictionary<string, object?>[] Rows(SqliteConnection connection, string sql)
        => connection.Query(sql).Cast<IDictionary<string, object?>>().ToArray();

    private static void AssertConsistent(IDictionary<string, object?> row, params string[] keys)
    {
        var readOnly = (IReadOnlyDictionary<string, object?>)row;
        Assert.Equal(keys.Length, row.Count);
        Assert.Equal(keys.Length, readOnly.Count);
        Assert.Equal(keys, row.Keys);
        Assert.Equal(keys, readOnly.Keys);
        var pairs = row.ToArray();
        Assert.Equal(keys, pairs.Select(pair => pair.Key));
        Assert.Equal(pairs.Select(pair => pair.Value), row.Values);
        Assert.Equal(pairs.Select(pair => pair.Value), readOnly.Values);
        Assert.Equal(keys.Length, ((IEnumerable)row).Cast<object>().Count());
        var copy = new KeyValuePair<string, object?>[row.Count + 2];
        row.CopyTo(copy, 1);
        Assert.Equal(pairs, copy.Skip(1).Take(row.Count));
        Assert.Null(copy[0].Key);
        Assert.Null(copy[copy.Length - 1].Key);
        foreach (var key in keys)
        {
            Assert.True(row.ContainsKey(key));
            Assert.True(row.TryGetValue(key, out var value));
            Assert.True(readOnly.ContainsKey(key));
            Assert.True(readOnly.TryGetValue(key, out var readOnlyValue));
            Assert.Equal(value, readOnlyValue);
            Assert.True(row.Contains(new KeyValuePair<string, object?>(key, value)));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void SiblingFieldsStayAbsent(int addedFields)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var rows = Rows(connection, "select 1 as Id union all select 2");
        for (int i = 0; i < addedFields; i++) rows[0].Add("Added" + i, i);
        var sibling = rows[1];
        var readOnly = (IReadOnlyDictionary<string, object?>)sibling;
        AssertConsistent(sibling, "Id");
        Assert.False(sibling.ContainsKey("Added0"));
        Assert.False(sibling.TryGetValue("Added0", out var value));
        Assert.False(readOnly.ContainsKey("Added0"));
        Assert.False(readOnly.TryGetValue("Added0", out var readOnlyValue));
        Assert.Null(value);
        Assert.Null(readOnlyValue);
        Assert.Null(sibling["Added0"]);
        Assert.Null(readOnly["Added0"]);
        Assert.False(sibling.Contains(new KeyValuePair<string, object?>("Added0", null)));
        Assert.False(sibling.Remove("Added0"));
        Assert.DoesNotContain("Added0", sibling.ToString());
    }

    [Fact]
    public void ClearAndRemoveStayConsistentAfterSchemaGrowth()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var rows = Rows(connection, "select 1 as Id union all select 2");
        rows[0].Add("Added", 42);
        Assert.True(rows[1].Remove("Id"));
        AssertConsistent(rows[1]);
        rows[1].Add("Id", 3);
        AssertConsistent(rows[1], "Id");
        rows[1].Clear();
        AssertConsistent(rows[1]);
        AssertConsistent(rows[0], "Id", "Added");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitNullRemainsPresent(bool useAdd)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var rows = Rows(connection, "select 1 as Id union all select 2");
        rows[0].Add("First", 1);
        rows[0].Add("Second", 2);
        if (useAdd) rows[1].Add("Second", null);
        else rows[1]["Second"] = null;
        AssertConsistent(rows[1], "Id", "Second");
        Assert.False(rows[1].TryGetValue("First", out _));
        Assert.True(rows[1].TryGetValue("Second", out var value));
        Assert.Null(value);
        Assert.Throws<ArgumentException>(() => rows[1].Add("Second", 3));
        rows[1].Remove("Second");
        AssertConsistent(rows[1], "Id");
        rows[1].Add("Second", null);
        AssertConsistent(rows[1], "Id", "Second");
    }

    [Fact]
    public void CachedQueryDoesNotExposePreviouslyAddedFields()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        const string sql = "select 1 as CachedId union all select 2";
        var first = Rows(connection, sql);
        first[0].Add("Added", 42);
        var later = Rows(connection, sql);
        Assert.All(later, row => AssertConsistent(row, "CachedId"));
        later[0]["Added"] = 43;
        AssertConsistent(later[0], "CachedId", "Added");
        AssertConsistent(first[1], "CachedId");
        Assert.Equal(42, first[0]["Added"]);
    }

    [Fact]
    public void OrdinaryAndNullDatabaseColumnsRemainPresent()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var row = Assert.Single(Rows(connection, "select 1 as OrdinaryId, null as NullValue"));
        AssertConsistent(row, "OrdinaryId", "NullValue");
        Assert.True(row.TryGetValue("NullValue", out var value));
        Assert.Null(value);
        row.Clear();
        AssertConsistent(row);
    }
}
