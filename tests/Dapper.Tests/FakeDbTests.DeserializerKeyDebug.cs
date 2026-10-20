#if !NET481
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers DeserializerKey.ToString() debug method (lines 93-111 in SqlMapper.TypeDeserializerCache.cs)
    /// via reflection. Three paths:
    /// - names is not null (copyDown=true): returns string.Join(", ", names)
    /// - reader is not null (copyDown=false): iterates reader.GetName
    /// - both null (copyDown=false, null reader): returns base.ToString()
    /// Also covers DisableCommandBehaviorOptimizations (lines 42-55 in SqlMapper.Settings.cs).
    /// </summary>
    public class FakeDbDeserializerKeyDebugTests
    {
        private static (Type keyType, ConstructorInfo ctor) GetDeserializerKey()
        {
            var assembly = typeof(SqlMapper).Assembly;
            var cacheType = assembly.GetType("Dapper.SqlMapper+TypeDeserializerCache")!;
            var keyType = cacheType.GetNestedType("DeserializerKey", BindingFlags.NonPublic)!;
            var ctor = keyType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            return (keyType, ctor);
        }

        // ── ToString() — names path (copyDown=true) ───────────────────

        [Fact]
        public void DeserializerKey_ToString_NamesPath_ReturnsColumnNames()
        {
            var (_, ctor) = GetDeserializerKey();

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();
            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id, Name FROM T");
            reader.Read();

            // copyDown=true copies names/types from reader into arrays
            var key = ctor.Invoke(new object[] { 42, 0, 2, false, reader, true });
            var str = key!.ToString()!;

            Assert.Contains("Id", str);
            Assert.Contains("Name", str);
        }

        // ── ToString() — reader path (copyDown=false) ─────────────────

        [Fact]
        public void DeserializerKey_ToString_ReaderPath_IteratesReader()
        {
            var (_, ctor) = GetDeserializerKey();

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Col1", 10 } }
            });
            conn.Open();
            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Col1 FROM T");
            reader.Read();

            // copyDown=false stores reader reference
            var key = ctor.Invoke(new object[] { 99, 0, 1, false, reader, false });
            var str = key!.ToString()!;

            Assert.Contains("Col1", str);
        }

        // ── ToString() — base path (names=null, reader=null) ──────────

        [Fact]
        public void DeserializerKey_ToString_BasePath_ReturnsNonNull()
        {
            var (_, ctor) = GetDeserializerKey();

            // copyDown=false with null reader → both names and reader are null
            var key = ctor.Invoke(new object?[] { 0, 0, 0, false, null!, false });
            var str = key!.ToString();

            Assert.NotNull(str); // base.ToString() returns the type name
        }

        // ── DisableCommandBehaviorOptimizations (lines 42-55) ─────────

        [Fact]
        public void Settings_DisableCommandBehaviorOptimizations_SingleResult_ReturnsTrue()
        {
            var method = typeof(SqlMapper.Settings).GetMethod(
                "DisableCommandBehaviorOptimizations",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            // Ensure we're at defaults so the method isn't already a no-op
            SqlMapper.Settings.SetDefaults();
            var originalSingle = SqlMapper.Settings.UseSingleResultOptimization;
            var originalRow = SqlMapper.Settings.UseSingleRowOptimization;

            try
            {
                // Exception message mentioning SingleResult triggers the disable
                var ex = new Exception("CommandBehavior.SingleResult is not supported by this provider");
                var result = (bool)method.Invoke(null, new object[]
                {
                    System.Data.CommandBehavior.SingleResult,
                    ex
                })!;

                Assert.True(result);
                Assert.False(SqlMapper.Settings.UseSingleResultOptimization);
            }
            finally
            {
                SqlMapper.Settings.UseSingleResultOptimization = originalSingle;
                SqlMapper.Settings.UseSingleRowOptimization = originalRow;
            }
        }

        [Fact]
        public void Settings_DisableCommandBehaviorOptimizations_NoMatch_ReturnsFalse()
        {
            var method = typeof(SqlMapper.Settings).GetMethod(
                "DisableCommandBehaviorOptimizations",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            SqlMapper.Settings.SetDefaults();
            var originalSingle = SqlMapper.Settings.UseSingleResultOptimization;
            var originalRow = SqlMapper.Settings.UseSingleRowOptimization;

            try
            {
                // Exception message NOT mentioning SingleResult/SingleRow → returns false
                var ex = new Exception("Some other error");
                var result = (bool)method.Invoke(null, new object[]
                {
                    System.Data.CommandBehavior.SingleResult,
                    ex
                })!;

                Assert.False(result);
            }
            finally
            {
                SqlMapper.Settings.UseSingleResultOptimization = originalSingle;
                SqlMapper.Settings.UseSingleRowOptimization = originalRow;
            }
        }
    }
}
#endif
