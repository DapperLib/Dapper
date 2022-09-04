using System;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Dapper.Tests
{
    public class TypeMapPr471
    {
        private readonly ITestOutputHelper output;

        public TypeMapPr471(ITestOutputHelper output)
        {
            this.output = output;
        }
        public class DateTimeToTimestampHandler : SqlMapper.TypeHandler<DateTimeOffset?>
        {
            private readonly ITestOutputHelper _output;
            public DateTimeToTimestampHandler(ITestOutputHelper outputHelper)
            {
                _output = outputHelper;
            }
            public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
            {
                _output.WriteLine($"SetValue was hit, input value is {value}");
             
                parameter.Value = value?.ToUnixTimeSeconds();
                parameter.DbType = DbType.Int64;
            }
            public override DateTimeOffset? Parse(object value)
            {
                _output.WriteLine($"Parse was hit, input value is {value} - converted to {DateTimeOffset.FromUnixTimeSeconds((long)value)}");
                return DateTimeOffset.FromUnixTimeSeconds((long)value);
            }
        }

        [Fact]
        public void TestMapDateTimeOffsetToInt()
        {
            DateTimeOffset? myTimestamp = DateTimeOffset.UtcNow;
            Dapper.SqlMapper.AddTypeHandler(new DateTimeToTimestampHandler(output));

            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            SQLitePCL.raw.sqlite3_trace(connection.Handle, (_, statement) => output.WriteLine($"Sent to SQLite: {statement}"), null);

            output.WriteLine("SQLite version is " + connection.ExecuteScalar("SELECT sqlite_version()"));
            connection.Execute("CREATE TABLE BugReport (ThisIsAnIntColumn INTEGER) STRICT");
            connection.Execute("INSERT INTO BugReport Values (1653915600)");
            var firstSelect = connection.Query<DateTimeOffset?>("SELECT * FROM BugReport");


            output.WriteLine($"Mapped result is {firstSelect.First()}");
            try
            {
                connection.Execute("INSERT INTO BugReport VALUES (@MyTimestamp)",
                    new { MyTimestamp = DateTimeOffset.UtcNow });
            }

            catch (Exception e)
            {
                throw new XunitException($"Didn't insert a datetime {e.Message}");
            }
            
        }
    }
}
