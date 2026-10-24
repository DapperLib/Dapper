#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue224Tests
    {
        [Fact]
        public void NullableScalarTypeHandlerDoesNotReceiveDbNull()
        {
            var handler = new MoneyHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { ["Value"] = DBNull.Value } });
                conn.Open();

                var value = conn.QuerySingle<Money?>("select Value from Values");

                Assert.Null(value);
                Assert.False(handler.ParseCalled);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private readonly struct Money
        {
            public Money(int value) => Value = value;
            public int Value { get; }
        }

        private sealed class MoneyHandler : SqlMapper.TypeHandler<Money>
        {
            public bool ParseCalled { get; private set; }

            public override Money Parse(object value)
            {
                ParseCalled = true;
                return new Money(Convert.ToInt32(value));
            }

            public override void SetValue(IDbDataParameter parameter, Money value)
                => parameter.Value = value.Value;
        }
    }
}
#endif
