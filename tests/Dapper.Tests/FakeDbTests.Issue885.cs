#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue885Tests
    {
        [Fact]
        public void AddingTypeHandlerInvalidatesCachedDeserializers()
        {
            SqlMapper.PurgeQueryCache();
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { ["Value"] = 1 } });
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { ["Value"] = 2 } });
            conn.Open();

            try
            {
                Assert.Equal(1, conn.QuerySingle<CachedValue>("select Value from Values").Value);

                SqlMapper.AddTypeHandler(new OffsetIntHandler());

                Assert.Equal(102, conn.QuerySingle<CachedValue>("select Value from Values").Value);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
                SqlMapper.PurgeQueryCache();
            }
        }

        private sealed class CachedValue
        {
            public int Value { get; set; }
        }

        private sealed class OffsetIntHandler : SqlMapper.TypeHandler<int>
        {
            public override int Parse(object value) => Convert.ToInt32(value) + 100;

            public override void SetValue(IDbDataParameter parameter, int value)
                => parameter.Value = value;
        }
    }
}
#endif
