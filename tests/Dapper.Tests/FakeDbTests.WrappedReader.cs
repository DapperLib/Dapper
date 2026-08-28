#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for DisposedReader, WrappedBasicReader, and DbWrappedReader.
    /// These are internal classes exercised indirectly via ExecuteReader.
    /// </summary>
    public class FakeDbWrappedReaderTests
    {
        // ── ExecuteReader returns a reader and delegates correctly ─────

        [Fact]
        public void ExecuteReader_CanReadRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT Id, Name FROM Users");

            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(reader.GetOrdinal("Id")));
            Assert.Equal("Alice", reader.GetString(reader.GetOrdinal("Name")));
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(reader.GetOrdinal("Id")));
            Assert.False(reader.Read());
        }

        [Fact]
        public void ExecuteReader_FieldCount_MatchesColumns()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 }, { "C", 3 } }
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT A, B, C FROM T");
            Assert.Equal(3, reader.FieldCount);
        }

        [Fact]
        public void ExecuteReader_GetName_ReturnsColumnName()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "MyColumn", 42 } }
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT MyColumn FROM T");
            Assert.Equal("MyColumn", reader.GetName(0));
        }

        [Fact]
        public void ExecuteReader_GetOrdinal_ReturnsIndex()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "First", 1 }, { "Second", 2 } }
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT First, Second FROM T");
            Assert.Equal(0, reader.GetOrdinal("First"));
            Assert.Equal(1, reader.GetOrdinal("Second"));
        }

        [Fact]
        public void ExecuteReader_IsDBNull_ReturnsTrueForNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", DBNull.Value } }
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT NULL AS Val");
            reader.Read();
            Assert.True(reader.IsDBNull(0));
        }

        [Fact]
        public void ExecuteReader_GetValue_ReturnsRawValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Val", 99 } }
            });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT 99 AS Val");
            reader.Read();
            Assert.Equal(99, reader.GetValue(0));
        }

        // ── async ExecuteReader ───────────────────────────────────────

        [Fact]
        public async Task ExecuteReaderAsync_CanReadRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Eve" } }
            });
            conn.Open();

            await using var reader = await conn.ExecuteReaderAsync("SELECT Id, Name FROM Users");
            Assert.True(await reader.ReadAsync());
            Assert.Equal(5, reader.GetInt32(reader.GetOrdinal("Id")));
        }

        // ── DisposedReader sentinel ───────────────────────────────────

        [Fact]
        public void DisposedReader_Read_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            Assert.Throws<ObjectDisposedException>(() => reader.Read());
        }

        [Fact]
        public void DisposedReader_GetValue_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            Assert.Throws<ObjectDisposedException>(() => reader.GetValue(0));
        }

        [Fact]
        public void DisposedReader_NextResult_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            Assert.Throws<ObjectDisposedException>(() => reader.NextResult());
        }

        [Fact]
        public void DisposedReader_GetName_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            Assert.Throws<ObjectDisposedException>(() => reader.GetName(0));
        }

        [Fact]
        public void DisposedReader_IsDBNull_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            Assert.Throws<ObjectDisposedException>(() => reader.IsDBNull(0));
        }

        [Fact]
        public void DisposedReader_GetBoolean_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetBoolean(0));

        [Fact]
        public void DisposedReader_GetByte_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetByte(0));

        [Fact]
        public void DisposedReader_GetInt16_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetInt16(0));

        [Fact]
        public void DisposedReader_GetInt32_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetInt32(0));

        [Fact]
        public void DisposedReader_GetInt64_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetInt64(0));

        [Fact]
        public void DisposedReader_GetFloat_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetFloat(0));

        [Fact]
        public void DisposedReader_GetDouble_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetDouble(0));

        [Fact]
        public void DisposedReader_GetDecimal_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetDecimal(0));

        [Fact]
        public void DisposedReader_GetDateTime_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetDateTime(0));

        [Fact]
        public void DisposedReader_GetGuid_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetGuid(0));

        [Fact]
        public void DisposedReader_GetString_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetString(0));

        [Fact]
        public void DisposedReader_GetOrdinal_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetOrdinal("x"));

        [Fact]
        public void DisposedReader_GetDataTypeName_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetDataTypeName(0));

        [Fact]
        public void DisposedReader_GetFieldType_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetFieldType(0));

        [Fact]
        public void DisposedReader_GetValues_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetValues(Array.Empty<object>()));

        [Fact]
        public async Task DisposedReader_ReadAsync_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadAsync(CancellationToken.None));
        }

        [Fact]
        public async Task DisposedReader_GetFieldValueAsync_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.GetFieldValueAsync<int>(0, CancellationToken.None));
        }

        [Fact]
        public async Task DisposedReader_IsDBNullAsync_ThrowsObjectDisposedException()
        {
            var reader = DisposedReader.Instance;
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.IsDBNullAsync(0, CancellationToken.None));
        }

        // ── Cancellation token honored ────────────────────────────────

        [Fact]
        public async Task ExecuteReaderAsync_WithCancelledToken_UsesCommandDefinition()
        {
            // Verify CommandDefinition plumbing works (cancellation checked on connection open)
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 } } });

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Connection is closed — Dapper will try to open it and should respect the token
            await Assert.ThrowsAnyAsync<Exception>(() =>
                conn.ExecuteReaderAsync(new CommandDefinition("SELECT Id FROM T", cancellationToken: cts.Token)));
        }
    }
}
#endif
