using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Dapper.Tests
{
    /// <summary>
    /// If Docker Desktop is installed, run the following command to start a container suitable for the tests.
    /// <code>
    /// docker run -d -p 1433:1433 --name Dapper.Tests.SqlServer -e "ACCEPT_EULA=y" -e "MSSQL_SA_PASSWORD=Pass1234" mcr.microsoft.com/mssql/server:2022-latest
    /// docker exec Dapper.Tests.SqlServer /opt/mssql-tools18/bin/sqlcmd -Q "CREATE DATABASE tests;" -C -U sa -P Pass1234
    /// docker exec Dapper.Tests.SqlServer /opt/mssql-tools18/bin/sqlcmd -Q "CREATE LOGIN test WITH PASSWORD='Pass1234';" -C -d tests -U sa -P Pass1234
    /// docker exec Dapper.Tests.SqlServer /opt/mssql-tools18/bin/sqlcmd -Q "CREATE USER test FOR LOGIN test" -C -d tests -U sa -P Pass1234
    /// docker exec Dapper.Tests.SqlServer /opt/mssql-tools18/bin/sqlcmd -Q "EXEC sp_addrolemember 'db_owner', 'test';" -C -d tests -U sa -P Pass1234
    /// </code>
    /// </summary>
    public sealed class SqlServerProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => Microsoft.Data.SqlClient.SqlClientFactory.Instance;

        public override string GetConnectionString() =>
            GetConnectionString("SqlServerConnectionString",
                "Server=localhost;Database=tests;User ID=test;Password=Pass1234;TrustServerCertificate=true;MultipleActiveResultSets=true");

        public DbConnection GetSqlServerConnection(bool open = true)
        {
            string cs = GetConnectionString();
            var csb = Factory.CreateConnectionStringBuilder()!;
            csb.ConnectionString = cs;
            var conn = Factory.CreateConnection()!;
            conn.ConnectionString = csb.ConnectionString;
            if (open) conn.Open();
            return conn;
        }
    }

    public class SQLServerTests : TestBase<SqlServerProvider>
    {
        [FactSqlServer]
        public async Task Issue2028_MARS_on_MicrosoftDataSqlClient()
        {
            using var conn = Provider.GetSqlServerConnection();


            try
            {
                conn.Execute("drop table Issue2028_Test");
            }
            catch
            {
                /* don't care */
            }

            conn.Execute("create table Issue2028_Test (Id int not null);");

            // insert multiple rows does not throw when using Microsoft.Data.SqlClient and having MultipleActiveRecordsets activated
            var cmd = new CommandDefinition("insert into Issue2028_Test (Id) values (@id)",
                new[] { new { id = 1 }, new { id = 2 } }, flags: CommandFlags.Pipelined);
            await conn.ExecuteAsync(cmd);
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactSqlServerAttribute : FactAttribute
        {
            public override string? Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string? unavailable;

            static FactSqlServerAttribute()
            {
                try
                {
                    using (DatabaseProvider<SqlServerProvider>.Instance.GetSqlServerConnection(true))
                    {
                        /* just trying to see if it works */
                    }
                }
                catch (Exception ex)
                {
                    unavailable = $"SqlServer is unavailable: {ex.Message}";
                }
            }
        }
    }
}
