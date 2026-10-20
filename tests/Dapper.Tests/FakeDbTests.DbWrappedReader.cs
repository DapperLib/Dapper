#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Targeted tests for DbWrappedReader — the DbDataReader wrapper returned by ExecuteReader.
    /// Exercises the many delegate methods to improve coverage.
    /// </summary>
    public class FakeDbDbWrappedReaderTests
    {
        private static fakeDbConnection CreateOpenConnection(IEnumerable<Dictionary<string, object?>> rows)
        {
            var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(rows);
            conn.Open();
            return conn;
        }

        [Fact]
        public void DbWrappedReader_HasRows_IsTrue_WhenDataPresent()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            Assert.True(reader.HasRows);
        }

        [Fact]
        public void DbWrappedReader_IsClosed_False_BeforeClose()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            Assert.False(reader.IsClosed);
        }

        [Fact]
        public void DbWrappedReader_RecordsAffected_ReturnsValue()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            // RecordsAffected is valid for SELECT queries (typically -1 or 0)
            Assert.True(reader.RecordsAffected >= -1);
        }

        [Fact]
        public void DbWrappedReader_Depth_ReturnsValue()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            Assert.True(reader.Depth >= 0);
        }

        [Fact]
        public void DbWrappedReader_VisibleFieldCount_EqualsFieldCount()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT A, B FROM T");
            Assert.Equal(reader.FieldCount, reader.VisibleFieldCount);
        }

        [Fact]
        public void DbWrappedReader_GetBoolean_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Flag", true } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Flag FROM T");
            reader.Read();
            Assert.True(reader.GetBoolean(0));
        }

        [Fact]
        public void DbWrappedReader_GetInt16_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", (short)42 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal((short)42, reader.GetInt16(0));
        }

        [Fact]
        public void DbWrappedReader_GetInt64_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 9999999999L } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(9999999999L, reader.GetInt64(0));
        }

        [Fact]
        public void DbWrappedReader_GetFloat_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 3.14f } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(3.14f, reader.GetFloat(0));
        }

        [Fact]
        public void DbWrappedReader_GetDouble_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 2.718 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(2.718, reader.GetDouble(0));
        }

        [Fact]
        public void DbWrappedReader_GetDecimal_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 1.23m } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(1.23m, reader.GetDecimal(0));
        }

        [Fact]
        public void DbWrappedReader_GetDateTime_Works()
        {
            var dt = new DateTime(2024, 1, 1);
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", dt } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(dt, reader.GetDateTime(0));
        }

        [Fact]
        public void DbWrappedReader_GetGuid_Works()
        {
            var guid = Guid.NewGuid();
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", guid } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(guid, reader.GetGuid(0));
        }

        [Fact]
        public void DbWrappedReader_GetString_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", "hello" } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal("hello", reader.GetString(0));
        }

        [Fact]
        public void DbWrappedReader_GetFieldValue_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 99 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(99, reader.GetFieldValue<int>(0));
        }

        [Fact]
        public async Task DbWrappedReader_GetFieldValueAsync_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 77 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            var val = await reader.GetFieldValueAsync<int>(0);
            Assert.Equal(77, val);
        }

        [Fact]
        public async Task DbWrappedReader_ReadAsync_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } },
                new Dictionary<string, object?> { { "Id", 2 } },
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            Assert.True(await reader.ReadAsync());
            Assert.Equal(1, reader.GetInt32(0));
            Assert.True(await reader.ReadAsync());
            Assert.Equal(2, reader.GetInt32(0));
            Assert.False(await reader.ReadAsync());
        }

        [Fact]
        public async Task DbWrappedReader_IsDBNullAsync_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", DBNull.Value } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT NULL AS Val");
            reader.Read();
            Assert.True(await reader.IsDBNullAsync(0));
        }

        [Fact]
        public async Task DbWrappedReader_NextResultAsync_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            // NextResultAsync returns false when there's only one result set
            var hasMore = await reader.NextResultAsync();
            Assert.False(hasMore);
        }

        [Fact]
        public void DbWrappedReader_GetSchemaTable_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            // May return null or a DataTable — just verify it doesn't throw
            var schema = reader.GetSchemaTable();
            // schema may be null for fakeDb
        }

        [Fact]
        public void DbWrappedReader_GetDataTypeName_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 42 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            var typeName = reader.GetDataTypeName(0);
            Assert.NotNull(typeName);
        }

        [Fact]
        public void DbWrappedReader_GetFieldType_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 42 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            var type = reader.GetFieldType(0);
            Assert.NotNull(type);
        }

        [Fact]
        public void DbWrappedReader_GetValues_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", "hello" } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT A, B FROM T");
            reader.Read();
            var values = new object[2];
            var count = reader.GetValues(values);
            Assert.Equal(2, count);
        }

        [Fact]
        public void DbWrappedReader_Indexer_ByOrdinal_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 55 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal(55, reader[0]);
        }

        [Fact]
        public void DbWrappedReader_Indexer_ByName_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", "test" } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            Assert.Equal("test", reader["Val"]);
        }

        [Fact]
        public async Task DbWrappedReader_CloseAsync_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            await reader.CloseAsync();
        }

        [Fact]
        public async Task DbWrappedReader_DisposeAsync_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            await reader.DisposeAsync();
        }

        [Fact]
        public void DbWrappedReader_NextResult_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Id FROM T");
            // After all rows, NextResult should return false (single result set)
            Assert.False(reader.NextResult());
        }

        [Fact]
        public void DbWrappedReader_GetProviderSpecificFieldType_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 1 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            var type = reader.GetProviderSpecificFieldType(0);
            Assert.NotNull(type);
        }

        [Fact]
        public void DbWrappedReader_GetProviderSpecificValue_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "Val", 123 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT Val FROM T");
            reader.Read();
            var val = reader.GetProviderSpecificValue(0);
            Assert.NotNull(val);
        }

        [Fact]
        public void DbWrappedReader_GetProviderSpecificValues_Works()
        {
            using var conn = CreateOpenConnection(new[]
            {
                new Dictionary<string, object?> { { "A", 1 }, { "B", 2 } }
            });

            using var reader = (DbDataReader)conn.ExecuteReader("SELECT A, B FROM T");
            reader.Read();
            var values = new object[2];
            reader.GetProviderSpecificValues(values);
        }
    }
}
#endif
