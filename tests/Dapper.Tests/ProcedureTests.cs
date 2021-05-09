using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    [Collection("ProcedureTests")]
    public sealed class SystemSqlClientProcedureTests : ProcedureTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("ProcedureTests")]
    public sealed class MicrosoftSqlClientProcedureTests : ProcedureTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class ProcedureTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestProcWithOutParameter()
        {
            connection.Execute(
            @"CREATE PROCEDURE #TestProcWithOutParameter
                @ID int output,
                @Foo varchar(100),
                @Bar int
            AS
                SET @ID = @Bar + LEN(@Foo)");
            var obj = new
            {
                ID = 0,
                Foo = "abc",
                Bar = 4
            };
            var args = new DynamicParameters(obj);
            args.Add("ID", 0, direction: ParameterDirection.Output);
            connection.Execute("#TestProcWithOutParameter", args, commandType: CommandType.StoredProcedure);
            Assert.Equal(7, args.Get<int>("ID"));
        }

        [Fact]
        public void TestProcWithOutAndReturnParameter()
        {
            connection.Execute(
            @"CREATE PROCEDURE #TestProcWithOutAndReturnParameter
                @ID int output,
                @Foo varchar(100),
                @Bar int
            AS
                SET @ID = @Bar + LEN(@Foo)
                RETURN 42");
            var obj = new
            {
                ID = 0,
                Foo = "abc",
                Bar = 4
            };
            var args = new DynamicParameters(obj);
            args.Add("ID", 0, direction: ParameterDirection.Output);
            args.Add("result", 0, direction: ParameterDirection.ReturnValue);
            connection.Execute("#TestProcWithOutAndReturnParameter", args, commandType: CommandType.StoredProcedure);
            Assert.Equal(7, args.Get<int>("ID"));
            Assert.Equal(42, args.Get<int>("result"));
        }

        [Fact]
        public void TestIssue17648290()
        {
            var p = new DynamicParameters();
            const int code = 1, getMessageControlId = 2;
            p.Add("@Code", code);
            p.Add("@MessageControlID", getMessageControlId);
            p.Add("@SuccessCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@ErrorDescription", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
            connection.Execute(
            @"CREATE PROCEDURE #up_MessageProcessed_get
                @Code varchar(10),
                @MessageControlID varchar(22),
                @SuccessCode int OUTPUT,
                @ErrorDescription varchar(255) OUTPUT
            AS
            BEGIN
                Select 2 as MessageProcessID, 38349348 as StartNum, 3874900 as EndNum, GETDATE() as StartDate, GETDATE() as EndDate
                SET @SuccessCode = 0
                SET @ErrorDescription = 'Completed successfully'
            END");
            var result = connection.Query(sql: "#up_MessageProcessed_get", param: p, commandType: CommandType.StoredProcedure);
            var row = result.Single();
            Assert.Equal(2, (int)row.MessageProcessID);
            Assert.Equal(38349348, (int)row.StartNum);
            Assert.Equal(3874900, (int)row.EndNum);
            DateTime startDate = row.StartDate, endDate = row.EndDate;
            Assert.Equal(0, p.Get<int>("SuccessCode"));
            Assert.Equal("Completed successfully", p.Get<string>("ErrorDescription"));
        }

        [Fact]
        public void SO24605346_ProcsAndStrings()
        {
            connection.Execute(
            @"create proc #GetPracticeRebateOrderByInvoiceNumber 
                @TaxInvoiceNumber nvarchar(20) 
            as
                select @TaxInvoiceNumber as [fTaxInvoiceNumber]");
            const string InvoiceNumber = "INV0000000028PPN";
            var result = connection.Query<PracticeRebateOrders>("#GetPracticeRebateOrderByInvoiceNumber", new
            {
                TaxInvoiceNumber = InvoiceNumber
            }, commandType: CommandType.StoredProcedure).FirstOrDefault();

            Assert.Equal("INV0000000028PPN", result.TaxInvoiceNumber);
        }

        private class PracticeRebateOrders
        {
            public string fTaxInvoiceNumber;
            [System.Xml.Serialization.XmlElement(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
            public string TaxInvoiceNumber
            {
                get { return fTaxInvoiceNumber; }
                set { fTaxInvoiceNumber = value; }
            }
        }

        [Fact]
        public void Issue327_ReadEmptyProcedureResults()
        {
            // Actually testing for not erroring here on the mapping having no rows to map on in Read<T>();
            connection.Execute(@"
            CREATE PROCEDURE #TestEmptyResults
            AS
                SELECT Top 0 1 Id, 'Bob' Name;
                SELECT Top 0 'Billy Goat' Creature, 'Unicorn' SpiritAnimal, 'Rainbow' Location;");
            var query = connection.QueryMultiple("#TestEmptyResults", commandType: CommandType.StoredProcedure);
            var result1 = query.Read<Issue327_Person>();
            var result2 = query.Read<Issue327_Magic>();
            Assert.False(result1.Any());
            Assert.False(result2.Any());
        }

        private class Issue327_Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class Issue327_Magic
        {
            public string Creature { get; set; }
            public string SpiritAnimal { get; set; }
            public string Location { get; set; }
        }

        [Fact]
        public void TestProcSupport()
        {
            var p = new DynamicParameters();
            p.Add("a", 11);
            p.Add("b", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("c", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            connection.Execute(@"
            create proc #TestProc 
	            @a int,
	            @b int output
            as 
            begin
	            set @b = 999
	            select 1111
	            return @a
            end");
            Assert.Equal(1111, connection.Query<int>("#TestProc", p, commandType: CommandType.StoredProcedure).First());

            Assert.Equal(11, p.Get<int>("c"));
            Assert.Equal(999, p.Get<int>("b"));
        }

        // https://stackoverflow.com/q/8593871
        [Fact]
        public void TestListOfAnsiStrings()
        {
            var results = connection.Query<string>("select * from (select 'a' str union select 'b' union select 'c') X where str in @strings",
                new
                {
                    strings = new[] {
                    new DbString { IsAnsi = true, Value = "a" },
                    new DbString { IsAnsi = true, Value = "b" }
                }
                }).ToList();

            Assert.Equal(2, results.Count);
            results.Sort();
            Assert.Equal("a", results[0]);
            Assert.Equal("b", results[1]);
        }

        [Fact]
        public void TestDateTime2PrecisionPreservedInDynamicParameters()
        {
            const string tempSPName = "#" + nameof(TestDateTime2PrecisionPreservedInDynamicParameters);

            DateTime datetimeDefault = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime datetime2 = datetimeDefault.AddTicks(1); // Add 100 ns

            Assert.True(datetimeDefault < datetime2);

            connection.Execute(
            $@"create proc {tempSPName} 
	            @a datetime2,
	            @b datetime2 output
            as 
            begin
	            set @b = @a
	            select DATEADD(ns, -100, @b)
            end");

            var p = new DynamicParameters();
            // Note: parameters declared as DateTime2
            p.Add("a", datetime2, dbType: DbType.DateTime2, direction: ParameterDirection.Input);
            p.Add("b", dbType: DbType.DateTime2, direction: ParameterDirection.Output);

            DateTime fromSelect = connection.Query<DateTime>(tempSPName, p, commandType: CommandType.StoredProcedure).First();

            Assert.Equal(datetimeDefault, fromSelect);

            Assert.Equal(datetime2, p.Get<DateTime>("b"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(DbType.DateTime)]
        public void TestDateTime2LosePrecisionInDynamicParameters(DbType? dbType)
        {
            const string tempSPName = "#" + nameof(TestDateTime2LosePrecisionInDynamicParameters);

            DateTime datetimeDefault = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime datetime2 = datetimeDefault.AddTicks(1); // Add 100 ns

            Assert.True(datetimeDefault < datetime2);

            connection.Execute(
            $@"create proc {tempSPName}
	            @a datetime2,
	            @b datetime2 output
            as 
            begin
	            set @b = DATEADD(ns, 100, @a)
	            select @b
            end");

            var p = new DynamicParameters();
            // Note: input parameter declared as DateTime (or implicitly as this) but SP has DateTime2
            p.Add("a", datetime2, dbType: dbType, direction: ParameterDirection.Input);
            p.Add("b", dbType: DbType.DateTime, direction: ParameterDirection.Output);

            DateTime fromSelect = connection.Query<DateTime>(tempSPName, p, commandType: CommandType.StoredProcedure).First();

            // @a truncates to datetimeDefault when passed into SP by DynamicParameters, add 100ns and it comes out as DateTime2
            Assert.Equal(datetime2, fromSelect);

            // @b gets set to datetime2 value but is truncated back to DbType.DateTime by DynamicParameter's Output declaration
            Assert.Equal(datetimeDefault, p.Get<DateTime>("b"));
        }


        [Fact]
        public async Task Issue591_NoResultsAsync()
        {
            const string tempSPName = "#" + nameof(Issue591_NoResultsAsync);

            var result = await connection.QueryAsync(
            $@"create proc {tempSPName}
            as 
            begin
                -- basically a failed if statement, so the select is not happening and the stored proc return nothing
                if 1=0
                begin
                    select 1 as Num
                end
            end
            
            exec {tempSPName}");

            Assert.Empty(result);
        }
    }
}
