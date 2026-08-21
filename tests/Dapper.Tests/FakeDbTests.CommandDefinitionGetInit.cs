#if !NET481
using System;
using System.Data;
using System.Reflection;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers CommandDefinition.GetInit (lines 147-199) and GetBasicPropertySetter (lines 201-209).
    /// GetInit generates IL delegates for Oracle-specific command properties (BindByName,
    /// InitialLONGFetchSize, FetchSize). GetBasicPropertySetter finds settable properties.
    /// </summary>
    public class FakeDbCommandDefinitionGetInitTests
    {
        private static Action<IDbCommand>? CallGetInit(Type? type)
        {
            CommandDefinition.ResetCommandInitCache();
            var method = typeof(CommandDefinition).GetMethod("GetInit",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            return (Action<IDbCommand>?)method.Invoke(null, new object?[] { type });
        }

        // ── L150: null type → returns null (GIGO) ─────────────────────

        [Fact]
        public void GetInit_NullType_ReturnsNull()
        {
            var result = CallGetInit(null);
            Assert.Null(result);
        }

        // ── No Oracle props → action is null, stored in cache ─────────

        [Fact]
        public void GetInit_NoOracleProps_ReturnsNull()
        {
            var result = CallGetInit(typeof(MinimalFakeCommand));
            Assert.Null(result);
        }

        // ── Cache hit: second call same type uses cached value ─────────

        [Fact]
        public void GetInit_CacheHit_ReturnsCachedValue()
        {
            CommandDefinition.ResetCommandInitCache();
            var method = typeof(CommandDefinition).GetMethod("GetInit",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            // First call populates cache
            var a1 = (Action<IDbCommand>?)method.Invoke(null, new object?[] { typeof(MinimalFakeCommand) });
            // Second call hits cache (TryGet returns true)
            var a2 = (Action<IDbCommand>?)method.Invoke(null, new object?[] { typeof(MinimalFakeCommand) });

            Assert.Equal(a1, a2); // both null for a command with no Oracle props
        }

        // ── BindByName property → IL generation ───────────────────────

        [Fact]
        public void GetInit_BindByName_GeneratesDelegate_And_SetsTrue()
        {
            var action = CallGetInit(typeof(FakeCmdWithBindByName));
            Assert.NotNull(action);

            var cmd = new FakeCmdWithBindByName();
            action!(cmd);
            Assert.True(cmd.BindByName);
        }

        // ── InitialLONGFetchSize property → IL generation ─────────────

        [Fact]
        public void GetInit_InitialLONGFetchSize_GeneratesDelegate_And_SetsNegativeOne()
        {
            var action = CallGetInit(typeof(FakeCmdWithLongFetch));
            Assert.NotNull(action);

            var cmd = new FakeCmdWithLongFetch();
            action!(cmd);
            Assert.Equal(-1, cmd.InitialLONGFetchSize);
        }

        // ── FetchSize property with FetchSize >= 0 → emits constant ───

        [Fact]
        public void GetInit_FetchSize_NonNegative_GeneratesDelegate_And_SetsFetchSize()
        {
            var originalFetchSize = SqlMapper.Settings.FetchSize;
            try
            {
                SqlMapper.Settings.FetchSize = 512L;
                var action = CallGetInit(typeof(FakeCmdWithFetchSize));
                Assert.NotNull(action);

                var cmd = new FakeCmdWithFetchSize();
                action!(cmd);
                Assert.Equal(512L, cmd.FetchSize);
            }
            finally
            {
                SqlMapper.Settings.FetchSize = originalFetchSize;
            }
        }

        // ── FetchSize property with FetchSize < 0 → no emit, but method still built ──

        [Fact]
        public void GetInit_FetchSize_Negative_StillBuildsMethod()
        {
            var originalFetchSize = SqlMapper.Settings.FetchSize;
            try
            {
                SqlMapper.Settings.FetchSize = -1L;
                // FetchSize < 0 → the if-block (L184) is false, no IL for FetchSize,
                // but method is still built (because FetchSize property exists)
                var action = CallGetInit(typeof(FakeCmdWithFetchSize));
                Assert.NotNull(action); // delegate still built (FetchSize prop was found)

                var cmd = new FakeCmdWithFetchSize();
                action!(cmd); // runs the empty method (just Ret)
                Assert.Equal(0L, cmd.FetchSize); // unchanged
            }
            finally
            {
                SqlMapper.Settings.FetchSize = originalFetchSize;
            }
        }

        // ── All three Oracle props ─────────────────────────────────────

        [Fact]
        public void GetInit_AllOracleProps_SetsAll()
        {
            var originalFetchSize = SqlMapper.Settings.FetchSize;
            try
            {
                SqlMapper.Settings.FetchSize = 1024L;
                var action = CallGetInit(typeof(FakeCmdWithAllOracleProps));
                Assert.NotNull(action);

                var cmd = new FakeCmdWithAllOracleProps();
                action!(cmd);
                Assert.True(cmd.BindByName);
                Assert.Equal(-1, cmd.InitialLONGFetchSize);
                Assert.Equal(1024L, cmd.FetchSize);
            }
            finally
            {
                SqlMapper.Settings.FetchSize = originalFetchSize;
            }
        }

        // ── GetBasicPropertySetter: valid property → returns setter ────
        // Line 205-206: returns prop.GetSetMethod()

        [Fact]
        public void GetBasicPropertySetter_ValidProperty_ReturnsSetter()
        {
            var method = typeof(CommandDefinition).GetMethod("GetBasicPropertySetter",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var result = (MethodInfo?)method.Invoke(null,
                new object[] { typeof(FakeCmdWithBindByName), "BindByName", typeof(bool) });

            Assert.NotNull(result);
        }

        // ── GetBasicPropertySetter: wrong type → returns null ─────────

        [Fact]
        public void GetBasicPropertySetter_WrongType_ReturnsNull()
        {
            var method = typeof(CommandDefinition).GetMethod("GetBasicPropertySetter",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var result = (MethodInfo?)method.Invoke(null,
                new object[] { typeof(FakeCmdWithBindByName), "BindByName", typeof(int) }); // wrong type

            Assert.Null(result);
        }
    }

    // ── Minimal IDbCommand base ────────────────────────────────────────

    internal abstract class MinimalFakeCommandBase : IDbCommand
    {
        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDbConnection? Connection { get; set; }
        public IDataParameterCollection Parameters { get; } = new FakeParameterCollection();
        public IDbTransaction? Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public IDbDataParameter CreateParameter() => new MinimalDbParameter2();
        public void Dispose() { }
        public int ExecuteNonQuery() => 0;
        public IDataReader ExecuteReader() => throw new NotImplementedException();
        public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotImplementedException();
        public object? ExecuteScalar() => null;
        public void Prepare() { }
    }

    internal class MinimalFakeCommand : MinimalFakeCommandBase { }

    internal class FakeCmdWithBindByName : MinimalFakeCommandBase
    {
        public bool BindByName { get; set; }
    }

    internal class FakeCmdWithLongFetch : MinimalFakeCommandBase
    {
        public int InitialLONGFetchSize { get; set; }
    }

    internal class FakeCmdWithFetchSize : MinimalFakeCommandBase
    {
        public long FetchSize { get; set; }
    }

    internal class FakeCmdWithAllOracleProps : MinimalFakeCommandBase
    {
        public bool BindByName { get; set; }
        public int InitialLONGFetchSize { get; set; }
        public long FetchSize { get; set; }
    }

    internal class FakeParameterCollection : System.Collections.ArrayList, IDataParameterCollection
    {
        public bool Contains(string parameterName) => false;
        public int IndexOf(string parameterName) => -1;
        public void RemoveAt(string parameterName) { }
        public object this[string parameterName]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
#endif
