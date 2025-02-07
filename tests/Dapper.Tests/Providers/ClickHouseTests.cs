#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using ClickHouse.Client.ADO;
using Xunit;
using Xunit.Sdk;

namespace Dapper.Tests.Providers
{
    public class ClickHouseProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory { get; } = new ClickHouseConnectionFactory();

        public override string GetConnectionString() =>
            GetConnectionString("ClickHouseConnectionString", "Server=localhost;Port=8123;Username=default");
    }

    public class ClickHouseTests : TestBase<ClickHouseProvider>
    {
        private ClickHouseConnection CreateConnection() => (ClickHouseConnection)Provider.GetOpenConnection();

        public static IEnumerable<object[]> SelectTestCases
        {
            get
            {
                yield return new object[] { "SELECT toInt16(-16)", (short)-16 };
                yield return new object[] { "SELECT toUInt16(16)", (ushort)16 };
                yield return new object[] { "SELECT toInt32(-32)", -32 };
                yield return new object[] { "SELECT toFloat64(64)", 64.0 };
                yield return new object[] { "SELECT 'hello'", "hello" };
                yield return new object[] { "SELECT array('hello', 'world')", new[] { "hello", "world" } };
            }
        }

        [TheoryClickHouse]
        [MemberData(nameof(SelectTestCases))]
        public void ShouldSelect(string sql, object expected) => Assert.Equal(expected, connection.ExecuteScalar(sql));

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactClickHouseAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactClickHouseAttribute()
            {
                try
                {
                    using (DatabaseProvider<ClickHouseProvider>.Instance.GetOpenConnection()) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"ClickHouse is unavailable: {ex.Message}";
                }
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class TheoryClickHouseAttribute : TheoryAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static TheoryClickHouseAttribute()
            {
                try
                {
                    using (DatabaseProvider<ClickHouseProvider>.Instance.GetOpenConnection()) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"ClickHouse is unavailable: {ex.Message}";
                }
            }
        }
    }
}

#endif
