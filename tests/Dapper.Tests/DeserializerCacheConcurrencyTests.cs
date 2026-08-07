using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// One SQL string, one connection string, one parameter type - so Dapper resolves a single
    /// <c>Identity</c> and every caller shares one cache slot. Only the <em>value</em> of
    /// <c>@mode</c> changes the result shape, which is what a branching stored procedure does in
    /// production. Each test drives one of the sites that reads the cached deserializer.
    /// </summary>
    [Collection(NonParallelDefinition.Name)]
    public class DeserializerCacheConcurrencyTests
    {
        private const string Sql = "select 1 as Id, case when @mode = 1 then 'abc' else 42 end as Value";

        private const int Threads = 16, Iterations = 50_000;

        public class Row
        {
            public int Id { get; set; }
            public string? Value { get; set; }
        }

        [Fact]
        public void QueryImpl_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix((cnn, mode) => Task.FromResult(cnn.Query<Row>(Sql, new { mode }).Single()));

        [Fact]
        public void ReadRow_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix((cnn, mode) => Task.FromResult(cnn.QuerySingle<Row>(Sql, new { mode })));

        [Fact]
        public void QueryAsync_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix(async (cnn, mode) => (await cnn.QueryAsync<Row>(Sql, new { mode })).Single());

        [Fact]
        public void QueryUnbufferedAsync_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix(async (cnn, mode) =>
            {
                await foreach (var row in cnn.QueryUnbufferedAsync<Row>(Sql, new { mode })) return row;
                throw new InvalidOperationException("no rows");
            });

        [Fact]
        public void GridReaderReadImpl_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix((cnn, mode) =>
            {
                using var grid = cnn.QueryMultiple(Sql, new { mode });
                return Task.FromResult(grid.Read<Row>().Single());
            });

        [Fact]
        public void GridReaderReadRow_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix((cnn, mode) =>
            {
                using var grid = cnn.QueryMultiple(Sql, new { mode });
                return Task.FromResult(grid.ReadSingle<Row>());
            });

        [Fact]
        public void GridReaderReadAsyncImpl_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix(async (cnn, mode) =>
            {
                using var grid = await cnn.QueryMultipleAsync(Sql, new { mode });
                return (await grid.ReadAsync<Row>()).Single();
            });

        [Fact]
        public void GridReaderReadRowAsyncImpl_DoesNotReuseAnotherShapesDeserializer()
            => AssertShapesNeverMix(async (cnn, mode) =>
            {
                using var grid = await cnn.QueryMultipleAsync(Sql, new { mode });
                return await grid.ReadSingleAsync<Row>();
            });

        private static void AssertShapesNeverMix(Func<SqliteConnection, int, Task<Row>> read)
        {
            var failures = new ConcurrentQueue<string>();
            var connections = new List<SqliteConnection>();
            var workers = new List<Task>();

            for (int i = 0; i < Threads; i++)
            {
                int mode = (i % 2) + 1;
                string expected = mode == 1 ? "abc" : "42";
                var connection = OpenConnection();
                connections.Add(connection);
                workers.Add(Task.Factory.StartNew(() =>
                {
                    for (int n = 0; n < Iterations; n++)
                    {
                        try
                        {
                            var row = read(connection, mode).GetAwaiter().GetResult();
                            if (row.Value != expected) failures.Enqueue($"mode {mode}: expected Value={expected}, got {row.Value}");
                        }
                        catch (Exception ex)
                        {
                            failures.Enqueue($"mode {mode}: {ex.GetBaseException().Message}");
                        }
                    }
                }, TaskCreationOptions.LongRunning));
            }

            try
            {
                Task.WaitAll(workers.ToArray());
            }
            finally
            {
                foreach (var connection in connections) connection.Dispose();
            }

            Assert.True(failures.IsEmpty, $"{failures.Count} corrupt read(s); first few:{Environment.NewLine}"
                + string.Join(Environment.NewLine, failures.Take(5)));
        }

        private static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }
    }
}
