#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbTypeHandlerTests
    {
        private class GuidStringHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value)
                => parameter.Value = value.ToString("D");

            public override Guid Parse(object value)
                => Guid.Parse(value.ToString()!);
        }

        [Fact]
        public void TypeHandler_Parse_IsInvokedWhenReadingColumn()
        {
            SqlMapper.AddTypeHandler(new GuidStringHandler());
            try
            {
                var id = Guid.NewGuid();
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "GuidId", id.ToString("D") } }
                });
                conn.Open();

                var result = conn.QueryFirst<GuidRow>("SELECT GuidId FROM Items");

                Assert.Equal(id, result.GuidId);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private class GuidRow
        {
            public Guid GuidId { get; set; }
        }

        [Fact]
        public void TypeHandler_SetValue_IsInvokedWhenPassingParameter()
        {
            var handler = new TrackingHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("INSERT INTO Items (Val) VALUES (@val)",
                    new { val = new TrackedType { Value = "x" } });

                Assert.True(handler.SetValueCalled);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        private class TrackedType { public string? Value { get; set; } }

        private class TrackingHandler : SqlMapper.TypeHandler<TrackedType>
        {
            public bool SetValueCalled { get; private set; }

            public override void SetValue(IDbDataParameter parameter, TrackedType value)
            {
                SetValueCalled = true;
                parameter.Value = value.Value;
            }

            public override TrackedType Parse(object value)
                => new TrackedType { Value = value?.ToString() };
        }
    }
}
#endif
