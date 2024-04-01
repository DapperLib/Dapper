using System;
using System.Threading.Tasks;
using Xunit;

#if NET6_0_OR_GREATER
namespace Dapper.Tests;

/* we do **NOT** expect this to work against System.Data
[Collection("DateTimeOnlyTests")]
public sealed class SystemSqlClientDateTimeOnlyTests : DateTimeOnlyTests<SystemSqlClientProvider> { }
*/
#if MSSQLCLIENT
[Collection("DateTimeOnlyTests")]
public sealed class MicrosoftSqlClientDateTimeOnlyTests : DateTimeOnlyTests<MicrosoftSqlClientProvider> { }
#endif
public abstract class DateTimeOnlyTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
{
    public class HazDateTimeOnly
    {
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
    }

    [Fact]
    public void TypedInOut()
    {
        var now = DateTime.Now;
        var args = new HazDateTimeOnly
        {
            Date = DateOnly.FromDateTime(now),
            Time = TimeOnly.FromDateTime(now),
        };
        var row = connection.QuerySingle<HazDateTimeOnly>("select @date as [Date], @time as [Time]", args);
        Assert.Equal(args.Date, row.Date);
        Assert.Equal(args.Time, row.Time);
    }

    [Fact]
    public async Task TypedInOutAsync()
    {
        var now = DateTime.Now;
        var args = new HazDateTimeOnly
        {
            Date = DateOnly.FromDateTime(now),
            Time = TimeOnly.FromDateTime(now),
        };
        var row = await connection.QuerySingleAsync<HazDateTimeOnly>("select @date as [Date], @time as [Time]", args);
        Assert.Equal(args.Date, row.Date);
        Assert.Equal(args.Time, row.Time);
    }

    [Fact]
    public void UntypedInOut()
    {
        var now = DateTime.Now;
        var args = new DynamicParameters();
        var date = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);
        args.Add("date", date);
        args.Add("time", time);
        var row = connection.QuerySingle<dynamic>("select @date as [Date], @time as [Time]", args);
        // untyped, observation is that these come back as DateTime and TimeSpan
        Assert.Equal(date, DateOnly.FromDateTime((DateTime)row.Date));
        Assert.Equal(time, TimeOnly.FromTimeSpan((TimeSpan)row.Time));
    }
}
#endif
