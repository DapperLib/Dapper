using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastMember;
using Xunit;
using Xunit.Abstractions;
using static Dapper.SqlMapper;

namespace Dapper.Tests;

[Collection("SingleRowTests")]
public sealed class SystemSqlClientSingleRowTests(ITestOutputHelper log) : SingleRowTests<SystemSqlClientProvider>(log)
{
    protected override async Task InjectDataAsync(DbConnection conn, DbDataReader source)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        using var bcp = new System.Data.SqlClient.SqlBulkCopy((System.Data.SqlClient.SqlConnection)conn);
#pragma warning restore CS0618 // Type or member is obsolete
        bcp.DestinationTableName = "#mydata";
        bcp.EnableStreaming = true;
        await bcp.WriteToServerAsync(source);
    }
}
#if MSSQLCLIENT
[Collection("SingleRowTests")]
public sealed class MicrosoftSqlClientSingleRowTests(ITestOutputHelper log) : SingleRowTests<MicrosoftSqlClientProvider>(log)
{
    protected override async Task InjectDataAsync(DbConnection conn, DbDataReader source)
    {
        using var bcp = new Microsoft.Data.SqlClient.SqlBulkCopy((Microsoft.Data.SqlClient.SqlConnection)conn);
        bcp.DestinationTableName = "#mydata";
        bcp.EnableStreaming = true;
        await bcp.WriteToServerAsync(source);
    }
}
#endif
public abstract class SingleRowTests<TProvider>(ITestOutputHelper log) : TestBase<TProvider> where TProvider : DatabaseProvider
{
    protected abstract Task InjectDataAsync(DbConnection connection, DbDataReader source);

    [Fact]
    public async Task QueryFirst_PerformanceAndCorrectness()
    {
        using var conn = GetOpenConnection();
        conn.Execute("create table #mydata(id int not null, name nvarchar(250) not null)");

        var rand = new Random();
        var data = from id in Enumerable.Range(1, 500_000)
                    select new MyRow { Id = rand.Next(), Name = CreateName(rand) };

        Stopwatch watch;
        using (var reader = ObjectReader.Create(data))
        {
            await InjectDataAsync(conn, reader);
            watch = Stopwatch.StartNew();
            var count = await conn.QuerySingleAsync<int>("""select count(1) from #mydata""");
            watch.Stop();
            log.WriteLine($"bulk-insert complete; {count} rows in {watch.ElapsedMilliseconds}ms");
        }

        // just errors
        var ex = Assert.ThrowsAny<DbException>(() => conn.Execute("raiserror('bad things', 16, 1)"));
        log.WriteLine(ex.Message);
        ex = await Assert.ThrowsAnyAsync<DbException>(async () => await conn.ExecuteAsync("raiserror('bad things', 16, 1)"));
        log.WriteLine(ex.Message);

        // just data
        watch = Stopwatch.StartNew();
        var row = conn.QueryFirst<MyRow>("select top 1 * from #mydata");
        watch.Stop();
        log.WriteLine($"sync top 1 read first complete; row {row.Id} in {watch.ElapsedMilliseconds}ms");

        watch = Stopwatch.StartNew();
        row = await conn.QueryFirstAsync<MyRow>("select top 1 * from #mydata");
        watch.Stop();
        log.WriteLine($"async top 1 read first complete; row {row.Id} in {watch.ElapsedMilliseconds}ms");

        watch = Stopwatch.StartNew();
        row = conn.QueryFirst<MyRow>("select * from #mydata");
        watch.Stop();
        log.WriteLine($"sync read first complete; row {row.Id} in {watch.ElapsedMilliseconds}ms");

        watch = Stopwatch.StartNew();
        row = await conn.QueryFirstAsync<MyRow>("select * from #mydata");
        watch.Stop();
        log.WriteLine($"async read first complete; row {row.Id} in {watch.ElapsedMilliseconds}ms");

        // data with trailing errors

        watch = Stopwatch.StartNew();
        ex = Assert.ThrowsAny<DbException>(() => conn.QueryFirst<MyRow>("select * from #mydata; raiserror('bad things', 16, 1)"));
        watch.Stop();
        log.WriteLine($"sync read with error complete in {watch.ElapsedMilliseconds}ms; {ex.Message}");

        watch = Stopwatch.StartNew();
        ex = await Assert.ThrowsAnyAsync<DbException>(async () => await conn.QueryFirstAsync<MyRow>("select * from #mydata; raiserror('bad things', 16, 1)"));
        watch.Stop();
        log.WriteLine($"async read with error complete in {watch.ElapsedMilliseconds}ms; {ex.Message}");

        // unbuffered read with trailing errors - do not expect to see this unless we consume all!

        watch = Stopwatch.StartNew();
        row = conn.Query<MyRow>("select * from #mydata", buffered: false).First();
        watch.Stop();
        log.WriteLine($"sync unbuffered LINQ read first complete; row {row.Id} in {watch.ElapsedMilliseconds}ms");

        watch = Stopwatch.StartNew();
        row = await conn.QueryUnbufferedAsync<MyRow>("select * from #mydata").FirstAsync();
        watch.Stop();
        log.WriteLine($"async unbuffered LINQ read first complete; row {row.Id} in {watch.ElapsedMilliseconds}ms");

        static unsafe string CreateName(Random rand)
        {
            const string Alphabet = "abcdefghijklmnopqrstuvwxyz 0123456789,;-";
            var len = rand.Next(5, 251);
            char* ptr = stackalloc char[len];
            for (int i = 0; i < len; i++)
            {
                ptr[i] = Alphabet[rand.Next(Alphabet.Length)];
            }
            return new string(ptr, 0, len);
        }

    }

    public class MyRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}

internal static class AsyncLinqHelper
{
    public static async ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        await using var iter = source.GetAsyncEnumerator(cancellationToken);
        if (!await iter.MoveNextAsync()) Array.Empty<T>().First(); // for consistent error
        return iter.Current;
    }
}
