using System;
using System.Threading.Tasks;
using Xunit;

#if NET6_0_OR_GREATER
namespace Dapper.Tests;

/* we do **NOT** expect this to work against System.Data
[Collection("DateTimeOnlyTests")]
public sealed class SystemSqlClientDateTimeOnlyTests : DateTimeOnlyTests<SystemSqlClientProvider> { }
*/
#if MSSQLCLIENT && DATEONLY
[Collection("DateTimeOnlyTests")]
public sealed class MicrosoftSqlClientDateTimeOnlyTests : DateTimeOnlyTests<MicrosoftSqlClientProvider> { }
#endif
public abstract class DateTimeOnlyTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
{
    public class HazDateTimeOnly
    {
        public string Name { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public DateOnly? NDate { get; set; }
        public TimeOnly? NTime { get; set; }
    }

    [Fact]
    public void TypedInOut()
    {
        var now = DateTime.Now;
        var args = new HazDateTimeOnly
        {
            Name = nameof(TypedInOut),
            Date = DateOnly.FromDateTime(now),
            Time = TimeOnly.FromDateTime(now),
            NDate = DateOnly.FromDateTime(now),
            NTime = TimeOnly.FromDateTime(now),
        };
        var row = connection.QuerySingle<HazDateTimeOnly>("select @name as [Name], @date as [Date], @time as [Time], @ndate as [NDate], @ntime as [NTime]", args);
        Assert.Equal(args.Name, row.Name);
        Assert.Equal(args.Date, row.Date);
        Assert.Equal(args.Time, row.Time);
        Assert.Equal(args.NDate, row.NDate);
        Assert.Equal(args.NTime, row.NTime);
    }

    [Fact]
    public async Task TypedInOutAsync()
    {
        var now = DateTime.Now;
        var args = new HazDateTimeOnly
        {
            Name = nameof(TypedInOutAsync),
            Date = DateOnly.FromDateTime(now),
            Time = TimeOnly.FromDateTime(now),
            NDate = DateOnly.FromDateTime(now),
            NTime = TimeOnly.FromDateTime(now),
        };
        var row = await connection.QuerySingleAsync<HazDateTimeOnly>("select @name as [Name], @date as [Date], @time as [Time], @ndate as [NDate], @ntime as [NTime]", args);
        Assert.Equal(args.Name, row.Name);
        Assert.Equal(args.Date, row.Date);
        Assert.Equal(args.Time, row.Time);
        Assert.Equal(args.NDate, row.NDate);
        Assert.Equal(args.NTime, row.NTime);
    }

    [Fact]
    public void UntypedInOut()
    {
        var now = DateTime.Now;
        var args = new DynamicParameters();
        var name = nameof(UntypedInOut);
        var date = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);
        args.Add("name", name);
        args.Add("date", date);
        args.Add("time", time);
        var row = connection.QuerySingle<dynamic>("select @name as [Name], @date as [Date], @time as [Time]", args);
        Assert.Equal(name, (string)row.Name);
        // untyped, observation is that these come back as DateTime and TimeSpan
        Assert.Equal(date, DateOnly.FromDateTime((DateTime)row.Date));
        Assert.Equal(time, TimeOnly.FromTimeSpan((TimeSpan)row.Time));
    }
}
#endif
