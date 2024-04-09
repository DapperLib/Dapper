using System;
using System.Data.Common;
using DuckDB.NET.Data;
using Xunit;

namespace Dapper.Tests
{
    public class DuckDBProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => DuckDBClientFactory.Instance;
        public override string GetConnectionString() => "Data Source=:memory:";
    }

    public abstract class DuckDBTypeTestBase : TestBase<DuckDBProvider>
    {
        protected DuckDBConnection GetDuckDBConnection(bool open = true)
            => (DuckDBConnection)(open ? Provider.GetOpenConnection() : Provider.GetClosedConnection());

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactDuckDBAttribute : FactAttribute
        {
            public override string? Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string? unavailable;

            static FactDuckDBAttribute()
            {
                try
                {
                    using var _ = DatabaseProvider<DuckDBProvider>.Instance.GetOpenConnection();
                }
                catch (Exception ex)
                {
                    unavailable = $"DuckDB is unavailable: {ex.Message}";
                }
            }
        }
    }

    public class DuckDBTests : DuckDBTypeTestBase
    {
        [FactDuckDB]
        public void DuckDBNamedParameter()
        {
            using var connection = GetDuckDBConnection();

            var result = connection.QueryFirst<int>("Select $foo", new {foo = 42});
            Assert.Equal(42, result);
        }

        [FactDuckDB]
        public void DuckDBPositionalParameter()
        {
            using var connection = GetDuckDBConnection();

            var dp = new DynamicParameters();
            dp.Add("?", 42);

            var result = connection.QueryFirst<int>("Select ?", dp);
            Assert.Equal(42, result);
        }
    }
}
