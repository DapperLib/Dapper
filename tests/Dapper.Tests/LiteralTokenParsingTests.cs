using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class LiteralTokenParsingTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("select @Value")]
        [InlineData("select {Value}")]
        [InlineData("select { =Value}")]
        [InlineData("select {=}")]
        [InlineData("select {=Value")]
        [InlineData("select {=Value-Name}")]
        [InlineData("select prefix{=Value}")]
        [InlineData("select _{=Value}")]
        [InlineData("select é{=Value}")]
        [InlineData("select 1{=Value}")]
        public void NoLiteralTokens(string? sql)
        {
            Assert.Same(SqlMapper.LiteralToken.None, SqlMapper.GetLiteralTokens(sql!));
        }

        [Theory]
        [InlineData("{=Value}", "Value")]
        [InlineData("select {=Value}", "Value")]
        [InlineData("select ({=Value})", "Value")]
        [InlineData("select {=café}", "café")]
        [InlineData("select {=值}", "值")]
        [InlineData("select {=_Value2}", "_Value2")]
        [InlineData("select {=123}", "123")]
        [InlineData("select {{=Value}}", "Value")]
        [InlineData("select {oops}, {=Value}", "Value")]
        [InlineData("select x{=Ignored}, {=Value}", "Value")]
        public void ParsesLiteralTokens(string sql, string member)
        {
            var token = Assert.Single(SqlMapper.GetLiteralTokens(sql));
            Assert.Equal(member, token.Member);
            Assert.Equal("{=" + member + "}", token.Token);
        }

        [Fact]
        public void DuplicateTokensAreRemovedWithoutMergingCaseDistinctMembers()
        {
            var tokens = SqlMapper.GetLiteralTokens("select {=Value}, {=Other}, {=Value}, {=value}");
            Assert.Equal(new[] { "Value", "Other", "value" }, tokens.Select(token => token.Member));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LongCommandRetainsTokensAtTheEnd(bool includeToken)
        {
            string sql = "select @Value /* " + new string('x', 100000) + " */";
            if (includeToken) sql += " {=Value}";
            var tokens = SqlMapper.GetLiteralTokens(sql);
            if (includeToken) Assert.Equal("Value", Assert.Single(tokens).Member);
            else Assert.Empty(tokens);
        }
    }
}
