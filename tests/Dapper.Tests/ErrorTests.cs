using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.Tests;

[Collection("ErrorTests")]
public sealed class SystemSqlClientErrorTests(ITestOutputHelper Log) : ErrorTests<SystemSqlClientProvider>(Log) { }

#if MSSQLCLIENT
[Collection("ErrorTests")]
public sealed class MicrosoftSqlClientErrorTests(ITestOutputHelper Log) : ErrorTests<MicrosoftSqlClientProvider>(Log) { }
#endif
public abstract class ErrorTests<TProvider>(ITestOutputHelper Log) : TestBase<TProvider>
    where TProvider : DatabaseProvider
{
    // from https://github.com/DapperLib/Dapper/issues/2077
    const string SQL = """
        -- MOVE the 'SELECT 7;'-Statement here, to increase the required while(read()) calls by one.

        BEGIN TRANSACTION
        BEGIN TRY
            SELECT 7; --SELECT any value INSIDE TRANSACTION

            --FORCE Error
            DECLARE @intvar INT
            SET @intvar = 'A'
        COMMIT TRANSACTION
        END TRY
        BEGIN CATCH
            ROLLBACK TRANSACTION
            ;THROW 50000, 'ERROR in Transaction', 1;
        END CATCH
        """;

    [Theory]
    [InlineData(CommandBehavior.Default)] // pass
    [InlineData(CommandBehavior.SingleResult, 0, 0)] // fail
    [InlineData(CommandBehavior.SingleResult, 1, 0)] // fail
    [InlineData(CommandBehavior.SingleResult, 2, 0)] // pass - two extra Read helps
    [InlineData(CommandBehavior.SingleResult, 3, 0)] // pass
    [InlineData(CommandBehavior.SingleResult, 0, 10)] // fail - no number of NextResult helps
    [InlineData(CommandBehavior.SingleRow, 0, 0)] // fail
    [InlineData(CommandBehavior.SingleRow, 1, 0)] // fail
    [InlineData(CommandBehavior.SingleRow, 2, 0)] // pass - two extra Read helps
    [InlineData(CommandBehavior.SingleRow, 3, 0)] // pass
    [InlineData(CommandBehavior.SingleRow, 0, 10)] // fail - no number of NextResult helps
    [InlineData(CommandBehavior.SingleRow | CommandBehavior.SingleResult, 0, 0)] // fail
    [InlineData(CommandBehavior.SingleRow | CommandBehavior.SingleResult, 1, 0)] // fail
    [InlineData(CommandBehavior.SingleRow | CommandBehavior.SingleResult, 2, 0)] // pass - two extra Read helps
    [InlineData(CommandBehavior.SingleRow | CommandBehavior.SingleResult, 3, 0)] // pass
    [InlineData(CommandBehavior.SingleRow | CommandBehavior.SingleResult, 0, 10)] // fail - no number of NextResult helps
    public void ManualADONET(CommandBehavior commandBehavior, int extraRowReads = 0, int extraGridReads = 0)
    {
        using var conn = GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SQL;
        using var reader = cmd.ExecuteReader(commandBehavior);

        var ex = AssertType(Assert.ThrowsAny<DbException>(() =>
        {
            int grid = 0;
            do
            {
                int row = 0;
                if (reader.Read())
                {
                    Log.WriteLine($"Read: {true}");
                    int width = reader.FieldCount;
                    do
                    {
                        for (int col = 0; col < width; col++)
                        {
                            var val = reader.GetValue(col);
                            Log.WriteLine($"[{grid}, {row}, {col}] {val}");
                        }
                        row++;
                    }
                    while (reader.Read());

                    for (int i = 0; i < extraRowReads; i++)
                    {
                        Log.WriteLine($"Reading (extra, {i})...");
                        Log.WriteLine($"...{reader.Read()}");
                    }
                }
                grid++;
            }
            while (reader.NextResult());

            for (int i = 0; i < extraGridReads; i++)
            {
                _ = reader.NextResult();
            }
        }));
        Log.WriteLine(ex.Message);

    }

    [Fact]
    public async Task ExceptionTestAsync()
    {
        var ex = AssertType(await Assert.ThrowsAnyAsync<DbException>(async () =>
        {
#if !NET472
            await
#endif
            using var connection = GetClosedConnection();
            _ = await connection.QueryFirstOrDefaultAsync<int>(SQL);
        }));
        Log.WriteLine(ex.Message);
    }
    [Fact]
    public void ExceptionTest()
    {
        var ex = AssertType(Assert.ThrowsAny<DbException>(() =>
        {
            using var connection = GetClosedConnection();
            _ = connection.QueryFirstOrDefault<int>(SQL);
        }));
        Log.WriteLine(ex.Message);
    }
}
