using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public class SqliteTests : TestBase
    {
        protected static SqliteConnection GetSQLiteConnection(bool open = true)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            if (open) connection.Open();
            return connection;
        }

        [FactSqlite]
        public void DapperEnumValue_Sqlite()
        {
            using (var connection = GetSQLiteConnection())
            {
                Common.DapperEnumValue(connection);
            }
        }

        [Collection(NonParallelDefinition.Name)]
        public class SqliteTypeHandlerTests : TestBase
        {
            [FactSqlite]
            public void Issue466_SqliteHatesOptimizations()
            {
                using (var connection = GetSQLiteConnection())
                {
                    SqlMapper.ResetTypeHandlers();
                    var row = connection.Query<HazNameId>("select 42 as Id").First();
                    Assert.Equal(42, row.Id);
                    row = connection.Query<HazNameId>("select 42 as Id").First();
                    Assert.Equal(42, row.Id);

                    SqlMapper.ResetTypeHandlers();
                    row = connection.QueryFirst<HazNameId>("select 42 as Id");
                    Assert.Equal(42, row.Id);
                    row = connection.QueryFirst<HazNameId>("select 42 as Id");
                    Assert.Equal(42, row.Id);
                }
            }

            [FactSqlite]
            public async Task Issue466_SqliteHatesOptimizations_Async()
            {
                using (var connection = GetSQLiteConnection())
                {
                    SqlMapper.ResetTypeHandlers();
                    var row = (await connection.QueryAsync<HazNameId>("select 42 as Id").ConfigureAwait(false)).First();
                    Assert.Equal(42, row.Id);
                    row = (await connection.QueryAsync<HazNameId>("select 42 as Id").ConfigureAwait(false)).First();
                    Assert.Equal(42, row.Id);

                    SqlMapper.ResetTypeHandlers();
                    row = await connection.QueryFirstAsync<HazNameId>("select 42 as Id").ConfigureAwait(false);
                    Assert.Equal(42, row.Id);
                    row = await connection.QueryFirstAsync<HazNameId>("select 42 as Id").ConfigureAwait(false);
                    Assert.Equal(42, row.Id);
                }
            }
        }

        [FactSqlite]
        public void Isse467_SqliteLikesParametersWithPrefix()
        {
            Isse467_SqliteParameterNaming(true);
        }

        [FactSqlite]
        public void Isse467_SqliteLikesParametersWithoutPrefix()
        { // see issue 375 / 467; note: fixed from RC2 onwards
            Isse467_SqliteParameterNaming(false);
        }

        private void Isse467_SqliteParameterNaming(bool prefix)
        {
            using (var connection = GetSQLiteConnection())
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select @foo";
                const SqliteType type = SqliteType.Integer;
                cmd.Parameters.Add(prefix ? "@foo" : "foo", type).Value = 42;
                var i = Convert.ToInt32(cmd.ExecuteScalar());
                Assert.Equal(42, i);
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactSqliteAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactSqliteAttribute()
            {
                try
                {
                    using (GetSQLiteConnection())
                    {
                    }
                }
                catch (Exception ex)
                {
                    unavailable = $"Sqlite is unavailable: {ex.Message}";
                }
            }
        }
    }
}