#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue2086Tests
{
    [Fact]
    public void ApplyNullValuesInvokesHandlerForValueTypeMembers()
    {
        var oldSetting = SqlMapper.Settings.ApplyNullValues;
        SqlMapper.Settings.ApplyNullValues = true;
        SqlMapper.ResetTypeHandlers();
        SqlMapper.PurgeQueryCache(); // deserializers bake in ApplyNullValues
        SqlMapper.AddTypeHandler(new SettableStringHandler());
        try
        {
            using var connection = new fakeDbConnection(new FakeDataStore());
            connection.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { ["MiddleName"] = DBNull.Value },
            });
            connection.Open();

            var result = connection.QuerySingle<Person>("select MiddleName");

            Assert.True(result.MiddleName.IsAssigned);
            Assert.Null(result.MiddleName.Value);
        }
        finally
        {
            SqlMapper.Settings.ApplyNullValues = oldSetting;
            SqlMapper.ResetTypeHandlers();
            SqlMapper.PurgeQueryCache();
        }
    }

    [Fact]
    public void DefaultSettingDoesNotPassDbNullToHandler()
    {
        var oldSetting = SqlMapper.Settings.ApplyNullValues;
        SqlMapper.Settings.ApplyNullValues = false;
        SqlMapper.ResetTypeHandlers();
        SqlMapper.PurgeQueryCache(); // deserializers bake in ApplyNullValues
        var handler = new SettableStringHandler();
        SqlMapper.AddTypeHandler(handler);
        try
        {
            using var connection = new fakeDbConnection(new FakeDataStore());
            connection.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { ["MiddleName"] = DBNull.Value },
            });
            connection.Open();

            var result = connection.QuerySingle<Person>("select MiddleName");

            Assert.False(result.MiddleName.IsAssigned);
            Assert.Equal(0, handler.ParseCalls);
        }
        finally
        {
            SqlMapper.Settings.ApplyNullValues = oldSetting;
            SqlMapper.ResetTypeHandlers();
            SqlMapper.PurgeQueryCache();
        }
    }

    private sealed class Person
    {
        public Settable<string?> MiddleName { get; set; }
    }

    private struct Settable<T>
    {
        public bool IsAssigned { get; init; }
        public T? Value { get; init; }
    }

    private sealed class SettableStringHandler : SqlMapper.TypeHandler<Settable<string?>>
    {
        public int ParseCalls { get; private set; }

        public override Settable<string?> Parse(object value)
        {
            ParseCalls++;
            return new() { IsAssigned = true, Value = value is DBNull ? null : (string)value };
        }

        public override void SetValue(IDbDataParameter parameter, Settable<string?> value) => parameter.Value = value.Value;
    }
}
#endif
