#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue1649Tests
    {
        [Fact]
        public void TypeHandlerIsUsedForEachListExpansionParameter()
        {
            var handler = new EncodedValueHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(2);
                conn.Open();

                var count = conn.Execute("select 1 where Id in @ids", new
                {
                    ids = new[] { new EncodedValue(7), new EncodedValue(8) }
                });

                Assert.Equal(2, count);
                Assert.Equal(new[] { "encoded:7", "encoded:8" }, handler.Values);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private readonly struct EncodedValue
        {
            public EncodedValue(int value) => Value = value;
            public int Value { get; }
        }

        private sealed class EncodedValueHandler : SqlMapper.TypeHandler<EncodedValue>
        {
            public List<string> Values { get; } = new();

            public override EncodedValue Parse(object value) => new(Convert.ToInt32(value));

            public override void SetValue(IDbDataParameter parameter, EncodedValue value)
            {
                var encoded = $"encoded:{value.Value}";
                Values.Add(encoded);
                parameter.Value = encoded;
            }
        }
    }
}
#endif
