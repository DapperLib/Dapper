using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class ProcedureTests : TestBase
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
            args.Get<int>("ID").IsEqualTo(7);
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
            args.Get<int>("ID").IsEqualTo(7);
            args.Get<int>("result").IsEqualTo(42);
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
            connection.Execute(@"CREATE PROCEDURE #up_MessageProcessed_get
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
            ((int)row.MessageProcessID).IsEqualTo(2);
            ((int)row.StartNum).IsEqualTo(38349348);
            ((int)row.EndNum).IsEqualTo(3874900);
            DateTime startDate = row.StartDate, endDate = row.EndDate;
            p.Get<int>("SuccessCode").IsEqualTo(0);
            p.Get<string>("ErrorDescription").IsEqualTo("Completed successfully");
        }

        [Fact]
        public void SO24605346_ProcsAndStrings()
        {
            connection.Execute(@"create proc #GetPracticeRebateOrderByInvoiceNumber @TaxInvoiceNumber nvarchar(20) as
                select @TaxInvoiceNumber as [fTaxInvoiceNumber]");
            const string InvoiceNumber = "INV0000000028PPN";
            var result = connection.Query<PracticeRebateOrders>("#GetPracticeRebateOrderByInvoiceNumber", new
            {
                TaxInvoiceNumber = InvoiceNumber
            }, commandType: CommandType.StoredProcedure).FirstOrDefault();

            result.TaxInvoiceNumber.IsEqualTo("INV0000000028PPN");
        }

        private class PracticeRebateOrders
        {
            public string fTaxInvoiceNumber;
#if !NETCOREAPP1_0
            [System.Xml.Serialization.XmlElement(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
#endif
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
            result1.Any().IsFalse();
            result2.Any().IsFalse();
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

            connection.Execute(@"create proc #TestProc 
	@a int,
	@b int output
as 
begin
	set @b = 999
	select 1111
	return @a
end");
            connection.Query<int>("#TestProc", p, commandType: CommandType.StoredProcedure).First().IsEqualTo(1111);

            p.Get<int>("c").IsEqualTo(11);
            p.Get<int>("b").IsEqualTo(999);
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

            results.Count.IsEqualTo(2);
            results.Sort();
            results[0].IsEqualTo("a");
            results[1].IsEqualTo("b");
        }
    }
}
