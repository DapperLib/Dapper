#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue1241Tests
    {
        [Fact]
        public void RemoveUnusedSkipsExplicitlyAddedCollectionParameters()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var parameters = new DynamicParameters();
            parameters.Add("ids", new[] { 1, 2, 3 });
            parameters.Add("used", 42);

            conn.Execute("select 1 where Value = @used", parameters);

            var command = conn.LastCreatedCommand!;
            Assert.DoesNotContain(command.Parameters.Cast<System.Data.IDataParameter>(),
                p => p.ParameterName.StartsWith("ids", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
#endif
