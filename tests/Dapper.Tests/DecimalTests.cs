using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection("DecimalTests")]
    public sealed class SystemSqlClientDecimalTests : DecimalTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("DecimalTests")]
    public sealed class MicrosoftSqlClientDecimalTests : DecimalTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class DecimalTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void Issue261_Decimals()
        {
            var parameters = new DynamicParameters();
            parameters.Add("c", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 10, scale: 5);
            connection.Execute("create proc #Issue261 @c decimal(10,5) OUTPUT as begin set @c=11.884 end");
            connection.Execute("#Issue261", parameters, commandType: CommandType.StoredProcedure);
            var c = parameters.Get<Decimal>("c");
            Assert.Equal(11.884M, c);
        }

        [Fact]
        public void Issue261_Decimals_ADONET_SetViaBaseClass() => Issue261_Decimals_ADONET(true);

        [Fact]
        public void Issue261_Decimals_ADONET_SetViaConcreteClass() => Issue261_Decimals_ADONET(false);

        private void Issue261_Decimals_ADONET(bool setPrecisionScaleViaAbstractApi)
        {
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "create proc #Issue261Direct @c decimal(10,5) OUTPUT as begin set @c=11.884 end";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* we don't care that it already exists */ }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "#Issue261Direct";
                var c = cmd.CreateParameter();
                c.ParameterName = "c";
                c.Direction = ParameterDirection.Output;
                c.Value = DBNull.Value;
                c.DbType = DbType.Decimal;

                if (setPrecisionScaleViaAbstractApi)
                {
                    IDbDataParameter baseParam = c;
                    baseParam.Precision = 10;
                    baseParam.Scale = 5;
                }
                else
                {
                    c.Precision = 10;
                    c.Scale = 5;
                }

                cmd.Parameters.Add(c);
                cmd.ExecuteNonQuery();
                decimal value = (decimal)c.Value;
                Assert.Equal(11.884M, value);
            }
        }

        [Fact]
        public void BasicDecimals()
        {
            var c = connection.Query<decimal>("select @c", new { c = 11.884M }).Single();
            Assert.Equal(11.884M, c);
        }

        [Fact]
        public void TestDoubleDecimalConversions_SO18228523_RightWay()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(1 as float) as A, cast(2 as float) as B, cast(3 as decimal) as C, cast(4 as decimal) as D").Single();
            Assert.Equal(1.0, row.A);
            Assert.Equal(2.0, row.B);
            Assert.Equal(3.0M, row.C);
            Assert.Equal(4.0M, row.D);
        }

        [Fact]
        public void TestDoubleDecimalConversions_SO18228523_WrongWay()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(1 as decimal) as A, cast(2 as decimal) as B, cast(3 as float) as C, cast(4 as float) as D").Single();
            Assert.Equal(1.0, row.A);
            Assert.Equal(2.0, row.B);
            Assert.Equal(3.0M, row.C);
            Assert.Equal(4.0M, row.D);
        }

        [Fact]
        public void TestDoubleDecimalConversions_SO18228523_Nulls()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(null as decimal) as A, cast(null as decimal) as B, cast(null as float) as C, cast(null as float) as D").Single();
            Assert.Equal(0.0, row.A);
            Assert.Null(row.B);
            Assert.Equal(0.0M, row.C);
            Assert.Null(row.D);
        }

        private class HasDoubleDecimal
        {
            public double A { get; set; }
            public double? B { get; set; }
            public decimal C { get; set; }
            public decimal? D { get; set; }
        }
    }
}
