#if !NET481
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue621Tests
    {
        [Fact]
        public void StaticPropertiesAreIgnoredWhenCreatingParameters()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var parameters = new Parameters { InstanceValue = "instance" };
            Parameters.StaticValue = "static";

            var result = conn.Execute("select @InstanceValue, @StaticValue", parameters);

            Assert.Equal(1, result);
        }

        private sealed class Parameters
        {
            public string? InstanceValue { get; set; }
            public static string? StaticValue { get; set; }
        }
    }
}
#endif
