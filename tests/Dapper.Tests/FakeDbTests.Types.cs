#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests type coercion paths inside Dapper's mapping engine.
    /// Exercising a wide variety of CLR types ensures the type-switch
    /// branches in SqlMapper are hit.
    /// </summary>
    public class FakeDbTypeCoercionTests
    {
        // ── primitive numeric types ─────────────────────────────────────

        [Fact]
        public void Query_MapsInt32Column()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 42 } } });
            conn.Open();
            Assert.Equal(42, conn.QueryFirst<int>("SELECT 42"));
        }

        [Fact]
        public void Query_MapsInt64Column()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 9_000_000_000L } } });
            conn.Open();
            Assert.Equal(9_000_000_000L, conn.QueryFirst<long>("SELECT 9000000000"));
        }

        [Fact]
        public void Query_MapsInt16Column()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", (short)32767 } } });
            conn.Open();
            Assert.Equal((short)32767, conn.QueryFirst<short>("SELECT 32767"));
        }

        [Fact]
        public void Query_MapsByteColumn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", (byte)255 } } });
            conn.Open();
            Assert.Equal((byte)255, conn.QueryFirst<byte>("SELECT 255"));
        }

        [Fact]
        public void Query_MapsBoolColumn_True()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", true } } });
            conn.Open();
            Assert.True(conn.QueryFirst<bool>("SELECT 1"));
        }

        [Fact]
        public void Query_MapsBoolColumn_False()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", false } } });
            conn.Open();
            Assert.False(conn.QueryFirst<bool>("SELECT 0"));
        }

        [Fact]
        public void Query_MapsFloatColumn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 3.14f } } });
            conn.Open();
            Assert.Equal(3.14f, conn.QueryFirst<float>("SELECT 3.14"), 4);
        }

        [Fact]
        public void Query_MapsDoubleColumn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 2.718281828 } } });
            conn.Open();
            Assert.Equal(2.718281828, conn.QueryFirst<double>("SELECT 2.718"), 6);
        }

        [Fact]
        public void Query_MapsDecimalColumn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 99.99m } } });
            conn.Open();
            Assert.Equal(99.99m, conn.QueryFirst<decimal>("SELECT 99.99"));
        }

        // ── string / char ───────────────────────────────────────────────

        [Fact]
        public void Query_MapsStringColumn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", "hello" } } });
            conn.Open();
            Assert.Equal("hello", conn.QueryFirst<string>("SELECT 'hello'"));
        }

        [Fact]
        public void Query_MapsCharColumn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 'A' } } });
            conn.Open();
            Assert.Equal('A', conn.QueryFirst<char>("SELECT 'A'"));
        }

        // ── date / time ─────────────────────────────────────────────────

        [Fact]
        public void Query_MapsDateTimeColumn()
        {
            var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", dt } } });
            conn.Open();
            Assert.Equal(dt, conn.QueryFirst<DateTime>("SELECT GETDATE()"));
        }

        [Fact]
        public void Query_MapsDateTimeOffsetColumn()
        {
            var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", dto } } });
            conn.Open();
            Assert.Equal(dto, conn.QueryFirst<DateTimeOffset>("SELECT SYSDATETIMEOFFSET()"));
        }

        [Fact]
        public void Query_MapsTimeSpanColumn()
        {
            var ts = TimeSpan.FromHours(2.5);
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", ts } } });
            conn.Open();
            Assert.Equal(ts, conn.QueryFirst<TimeSpan>("SELECT '02:30:00'"));
        }

        // ── Guid ────────────────────────────────────────────────────────

        [Fact]
        public void Query_MapsGuidColumn()
        {
            var g = Guid.NewGuid();
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", g } } });
            conn.Open();
            Assert.Equal(g, conn.QueryFirst<Guid>("SELECT NEWID()"));
        }

        // ── nullable types ──────────────────────────────────────────────

        [Fact]
        public void Query_MapsNullableInt_WithValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 7 } } });
            conn.Open();
            Assert.Equal(7, conn.QueryFirst<int?>("SELECT 7"));
        }

        [Fact]
        public void Query_MapsNullableInt_WithNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", DBNull.Value } } });
            conn.Open();
            Assert.Null(conn.QueryFirst<int?>("SELECT NULL"));
        }

        [Fact]
        public void Query_MapsNullableBool_WithNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", DBNull.Value } } });
            conn.Open();
            Assert.Null(conn.QueryFirst<bool?>("SELECT NULL"));
        }

        [Fact]
        public void Query_MapsNullableDateTime_WithValue()
        {
            var dt = new DateTime(2025, 1, 1);
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", dt } } });
            conn.Open();
            Assert.Equal(dt, conn.QueryFirst<DateTime?>("SELECT GETDATE()"));
        }

        // ── multi-column POCO ────────────────────────────────────────────

        private class AllTypesRow
        {
            public int IntCol { get; set; }
            public long LongCol { get; set; }
            public bool BoolCol { get; set; }
            public double DoubleCol { get; set; }
            public decimal DecimalCol { get; set; }
            public string? StringCol { get; set; }
            public DateTime DateCol { get; set; }
            public Guid GuidCol { get; set; }
        }

        [Fact]
        public void Query_MapsAllColumnTypesToPoco()
        {
            var g = Guid.NewGuid();
            var dt = new DateTime(2024, 1, 1);
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?>
                {
                    { "IntCol", 1 },
                    { "LongCol", 2L },
                    { "BoolCol", true },
                    { "DoubleCol", 3.14 },
                    { "DecimalCol", 99.99m },
                    { "StringCol", "test" },
                    { "DateCol", dt },
                    { "GuidCol", g },
                }
            });
            conn.Open();

            var row = conn.QueryFirst<AllTypesRow>("SELECT ...");

            Assert.Equal(1, row.IntCol);
            Assert.Equal(2L, row.LongCol);
            Assert.True(row.BoolCol);
            Assert.Equal(3.14, row.DoubleCol, 10);
            Assert.Equal(99.99m, row.DecimalCol);
            Assert.Equal("test", row.StringCol);
            Assert.Equal(dt, row.DateCol);
            Assert.Equal(g, row.GuidCol);
        }

        // ── enum mapping ─────────────────────────────────────────────────

        private enum Status { Active = 1, Inactive = 2 }

        private class StatusRow { public Status Status { get; set; } }

        [Fact]
        public void Query_MapsIntColumnToEnum()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Status", 1 } } });
            conn.Open();

            var result = conn.QueryFirst<StatusRow>("SELECT 1 AS Status");
            Assert.Equal(Status.Active, result.Status);
        }

        [Fact]
        public void Query_MapsEnumDirectly()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Val", 2 } } });
            conn.Open();

            var result = conn.QueryFirst<Status>("SELECT 2");
            Assert.Equal(Status.Inactive, result);
        }

        // ── value type as scalar ─────────────────────────────────────────

        [Fact]
        public void ExecuteScalar_MapsToInt()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(10);
            conn.Open();
            Assert.Equal(10, conn.ExecuteScalar<int>("SELECT 10"));
        }

        [Fact]
        public void ExecuteScalar_MapsToGuid()
        {
            var g = Guid.NewGuid();
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(g);
            conn.Open();
            Assert.Equal(g, conn.ExecuteScalar<Guid>("SELECT NEWID()"));
        }

        [Fact]
        public void ExecuteScalar_MapsToNullableInt_Null()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(null);
            conn.Open();
            Assert.Null(conn.ExecuteScalar<int?>("SELECT NULL"));
        }

        // ── unbuffered query ──────────────────────────────────────────────

        [Fact]
        public void Query_Unbuffered_ReturnsLazySequence()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            var result = conn.Query<User>("SELECT Id, Name FROM Users", buffered: false).ToList();
            Assert.Equal(2, result.Count);
        }

        // ── IEnumerable parameter expansion ──────────────────────────────

        [Fact]
        public void Query_WithListParameter_ExpandsToIn()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } },
            });
            conn.Open();

            var ids = new[] { 1, 3 };
            var result = conn.Query<User>("SELECT Id, Name FROM Users WHERE Id IN @ids", new { ids })
                             .ToList();

            Assert.Equal(2, result.Count);
        }

        // ── CommandDefinition overload ────────────────────────────────────

        [Fact]
        public void Query_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 5 }, { "Name", "CD" } } });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM Users WHERE Id = @id",
                parameters: new { id = 5 });
            var result = conn.Query<User>(cmd).ToList();

            Assert.Single(result);
            Assert.Equal(5, result[0].Id);
        }

        [Fact]
        public void ExecuteScalar_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(7);
            conn.Open();

            var cmd = new CommandDefinition("SELECT COUNT(*) FROM Users");
            Assert.Equal(7, conn.ExecuteScalar<int>(cmd));
        }

        [Fact]
        public void Execute_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(2);
            conn.Open();

            var cmd = new CommandDefinition("DELETE FROM Users WHERE Active = 0");
            Assert.Equal(2, conn.Execute(cmd));
        }
    }
}
#endif
