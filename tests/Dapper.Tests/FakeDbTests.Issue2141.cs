#if !NET481
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue2141Tests
    {
        [Fact]
        public void StringTypeHandlerOwnsParameterSize()
        {
            SqlMapper.RemoveTypeMap(typeof(string));
            var handler = new SizedStringHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("select @value", new { value = "hello" });

                Assert.Equal(7, handler.LastParameter!.Size);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
                SqlMapper.AddTypeMap(typeof(string), DbType.String);
            }
        }

        private sealed class SizedStringHandler : SqlMapper.TypeHandler<string>
        {
            public IDbDataParameter? LastParameter { get; private set; }

            public override string Parse(object value) => (string)value;

            public override void SetValue(IDbDataParameter parameter, string? value)
            {
                LastParameter = parameter;
                parameter.DbType = DbType.String;
                parameter.Value = value;
                parameter.Size = 7;
            }
        }
    }
}
#endif
