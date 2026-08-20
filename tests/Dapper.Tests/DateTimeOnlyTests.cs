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
        public string Name { get; set; } = "";
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

    [Fact] // #2072: a datetime column into a DateOnly member - the provider boxes DateTime,
    // so this must convert rather than demand GetFieldValue<DateOnly> of a datetime column
    public void MembersFromDateTimeAndTimeSpanColumns()
    {
        var row = connection.QuerySingle<HazDateTimeOnly>(
            "select 'x' as [Name], cast('2019-10-01' as datetime) as [Date], cast('03:03:03' as time) as [Time], cast('2019-10-02' as datetime) as [NDate], cast('04:04:04' as time) as [NTime]");
        Assert.Equal(new DateOnly(2019, 10, 1), row.Date);
        Assert.Equal(new TimeOnly(3, 3, 3), row.Time);
        Assert.Equal(new DateOnly(2019, 10, 2), row.NDate);
        Assert.Equal(new TimeOnly(4, 4, 4), row.NTime);
    }

    [Fact] // #2227: the scalar form must not silently yield default(T)
    public void ScalarDateOnlyAndTimeOnly()
    {
        Assert.Equal(new DateOnly(2021, 1, 1), connection.QuerySingle<DateOnly>("select cast('2021-01-01' as date)"));
        Assert.Equal(new TimeOnly(3, 3, 3), connection.QuerySingle<TimeOnly>("select cast('03:03:03' as time)"));
        Assert.Equal(new DateOnly(2021, 1, 1), connection.QuerySingle<DateOnly?>("select cast('2021-01-01' as date)"));
    }

    [Fact] // the pre-DateOnly reading of a date column must keep working
    public void DateColumnAsDateTime()
    {
        Assert.Equal(new DateTime(2021, 1, 1), connection.QuerySingle<DateTime>("select cast('2021-01-01' as date)"));
        Assert.Equal(new DateTime(2021, 1, 1), connection.QuerySingle<HazDateTime>("select cast('2021-01-01' as date) as [When]").When);
    }

    public class HazDateTime
    {
        public DateTime When { get; set; }
    }
}
#endif
