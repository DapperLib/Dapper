using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests.Issues
{
    [Collection("Issues")]
    public sealed class SystemSqlClientIssue1431 : Issue1431<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("Issues")]
    public sealed class MicrosoftSqlClientIssue1431 : Issue1431<MicrosoftSqlClientProvider> { }
#endif
    public abstract class Issue1431<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public async Task CanUseDataTableTVP()
        {
            InitSchema();
            var result = (await GetSomeData(new[] { 1, 2, 3 }));
            Assert.Equal("1,2,3", string.Join(",", result));
        }

        private void InitSchema()
        {
            try { connection.Execute("drop proc Issue1431_GetDataWithTVP"); } catch { }
            try { connection.Execute("drop type Issue1431_IdFilter"); } catch { }
            
            connection.Execute("CREATE TYPE Issue1431_IdFilter AS TABLE (Id int NOT NULL)");
            connection.Execute(@"CREATE PROC Issue1431_GetDataWithTVP (@IdFilter Issue1431_IdFilter READONLY)
AS
BEGIN

    SET NOCOUNT ON;

    SELECT Id
      FROM @IdFilter;
END");
        }
        public async Task<IEnumerable<int>> GetSomeData(IEnumerable<int> idFilter)
        {
            
            var dataTable = GetDataTable(idFilter);

            var dynamicParams = new DynamicParameters(new { IdFilter = dataTable });

            return await connection.QueryAsync<int>("Issue1431_GetDataWithTVP", dynamicParams, commandType: CommandType.StoredProcedure);
        }

        private static DataTable GetDataTable(IEnumerable<int> idFilter)
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));

            if (idFilter == null) return dt;

            foreach (var id in idFilter)
            {
                dt.Rows.Add(id);
            }

            return dt;
        }
    }
}
