#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers DbWrappedReader.Create null-cmd path (line 88) and null-reader path (lines 91-92),
    /// and IWrappedDataReader explicit interface properties (lines 98, 100).
    /// </summary>
    public class FakeDbWrappedReaderCreateTests
    {
        private static Type GetDbWrappedReaderType()
            => typeof(SqlMapper).Assembly.GetType("Dapper.DbWrappedReader")!;

        // ── Create(null, reader) — returns reader directly (line 88) ──

        [Fact]
        public void DbWrappedReader_Create_NullCmd_ReturnsReaderDirectly()
        {
            var type = GetDbWrappedReaderType();
            var create = type.GetMethod("Create",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(IDbCommand), typeof(DbDataReader) },
                null)!;

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();
            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");

            // null cmd → returns the reader itself, no wrapping
            var result = (DbDataReader)create.Invoke(null, new object?[] { null, reader })!;
            Assert.Same(reader, result);
        }

        // ── Create(cmd, null) — disposes cmd and returns null (lines 91-92) ──

        [Fact]
        public void DbWrappedReader_Create_NullReader_DisposesCmd_ReturnsNull()
        {
            var type = GetDbWrappedReaderType();
            var create = type.GetMethod("Create",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(IDbCommand), typeof(DbDataReader) },
                null)!;

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();
            var cmd = conn.CreateCommand();

            // null reader → cmd.Dispose() is called, returns null
            var result = create.Invoke(null, new object?[] { cmd, null });
            Assert.Null(result);
        }

        // ── IWrappedDataReader.Reader and .Command (lines 98, 100) ────

        [Fact]
        public void DbWrappedReader_IWrappedDataReader_ReaderAndCommand_Accessible()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();
            using var innerReader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            var cmd = conn.CreateCommand();

            var type = GetDbWrappedReaderType();
            var ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(IDbCommand), typeof(DbDataReader) }, null)!;

            var wrapped = (DbDataReader)ctor.Invoke(new object[] { cmd, innerReader });
            try
            {
                var iface = (IWrappedDataReader)wrapped;
                Assert.NotNull(iface.Reader);
                Assert.NotNull(iface.Command);
            }
            finally
            {
                wrapped.Dispose();
            }
        }
    }
}
#endif
