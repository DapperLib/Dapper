using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public class SqliteProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => SqliteFactory.Instance;
        public override string GetConnectionString() => "Data Source=:memory:";
    }

    public abstract class SqliteTypeTestBase : TestBase<SqliteProvider>
    {
        protected SqliteConnection GetSQLiteConnection(bool open = true)
             => (SqliteConnection)(open ? Provider.GetOpenConnection() : Provider.GetClosedConnection());

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactSqliteAttribute : FactAttribute
        {
            public override string? Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string? unavailable;

            static FactSqliteAttribute()
            {
                try
                {
                    using var _ = DatabaseProvider<SqliteProvider>.Instance.GetOpenConnection();
                }
                catch (Exception ex)
                {
                    unavailable = $"Sqlite is unavailable: {ex.Message}";
                }
            }
        }
    }

    [Collection(NonParallelDefinition.Name)]
    public class SqliteTypeHandlerTests : SqliteTypeTestBase
    {
#if NET6_0_OR_GREATER
        private sealed class Issue2266DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
        {
            public int ParseCalls { get; private set; }
            public int SetValueCalls { get; private set; }

            public override DateOnly Parse(object value)
            {
                ParseCalls++;
                return DateOnly.Parse((string)value, CultureInfo.InvariantCulture);
            }

            public override void SetValue(IDbDataParameter parameter, DateOnly value)
            {
                SetValueCalls++;
                parameter.DbType = DbType.String;
                parameter.Value = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        private sealed class Issue2266DateOnlyResult
        {
            public DateOnly Date { get; set; }
            public DateOnly? NullableDate { get; set; }
        }

        [FactSqlite]
        public void Issue2266_TypeHandlerOverridesBuiltInDateOnlyMapping()
        {
            using var connection = GetSQLiteConnection();
            var handler = new Issue2266DateOnlyHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                var expected = new DateOnly(2024, 10, 31);
                var result = connection.QuerySingle<Issue2266DateOnlyResult>(
                    "select @date as Date, @nullableDate as NullableDate",
                    new { date = expected, nullableDate = (DateOnly?)expected });

                Assert.Equal(expected, result.Date);
                Assert.Equal(expected, result.NullableDate);
                Assert.Equal(2, handler.SetValueCalls);
                Assert.Equal(2, handler.ParseCalls);
            }
            finally
            {
                SqlMapper.AddTypeHandler(typeof(DateOnly), null!);
            }
        }
#endif

        [FactSqlite]
        public void Issue466_SqliteHatesOptimizations()
        {
            using var connection = GetSQLiteConnection();

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

        [FactSqlite]
        public void TypeHandlerSetValue_StringParameterSizeIsNotOverwritten_Issue2141()
        {
            using var connection = GetSQLiteConnection();

            SqlMapper.ResetTypeHandlers();
            SqlMapper.RemoveTypeMap(typeof(string));
            var handler = new SizedStringTypeHandler();
            SqlMapper.AddTypeHandler(handler);
            try
            {
                // anonymous object parameters take the IL generator path, where the
                // generated code used to overwrite the Size set by the handler
                connection.Execute("select @p", new { p = "abc" });

                Assert.NotNull(handler.LastParameter);
                Assert.Equal(SizedStringTypeHandler.ExpectedSize, handler.LastParameter.Size);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
                SqlMapper.AddTypeMap(typeof(string), DbType.String);
            }
        }

        private sealed class SizedStringTypeHandler : SqlMapper.TypeHandler<string>
        {
            public const int ExpectedSize = 12345;
            public IDbDataParameter? LastParameter { get; private set; }

            public override void SetValue(IDbDataParameter parameter, string? value)
            {
                parameter.Value = value;
                parameter.Size = ExpectedSize;
                LastParameter = parameter;
            }

            public override string Parse(object value) => (string)value;
        }

        [FactSqlite]
        public async Task Issue466_SqliteHatesOptimizations_Async()
        {
            using var connection = GetSQLiteConnection();

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

    public class SqliteTests : SqliteTypeTestBase
    { 
        [FactSqlite]
        public void DapperEnumValue_Sqlite()
        {
            using var connection = GetSQLiteConnection();
            Common.DapperEnumValue(connection);
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
            using var connection = GetSQLiteConnection();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select @foo";
            const SqliteType type = SqliteType.Integer;
            cmd.Parameters.Add(prefix ? "@foo" : "foo", type).Value = 42;
            var i = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(42, i);
        }

        [FactSqlite]
        public void DateTimeIsParsedWithInvariantCulture()
        {
            connection.Execute("CREATE TABLE [PersonWithDob] ([Id] integer primary key autoincrement, [DoB] DATETIME not null )");

            var localMorning = DateTime.Parse("2019-07-31 01:00:00");

            var culture = Thread.CurrentThread.CurrentCulture;

            try
            {
                connection.Execute("INSERT INTO [PersonWithDob] ([DoB]) VALUES (@DoB)", new PersonWithDob { DoB = localMorning });

                // Before we read the column, use Farsi this is a way to ensure the 
                // InvariantCulture is used as otherwise it would fail because Farsi
                // is not able to parse a DateTime that is formatted with Invariant

                var farsi = System.Globalization.CultureInfo.GetCultureInfo("fa-IR");
                Thread.CurrentThread.CurrentCulture = farsi;
                Thread.CurrentThread.CurrentUICulture = farsi;

                var person = connection.QueryFirst<PersonWithDob>("SELECT * FROM [PersonWithDob]");

                Assert.Equal(localMorning, person.DoB);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                connection.Execute("DROP TABLE [PersonWithDob]");
            }
        }

        private class PersonWithDob
        {
            public int Id { get; set; }
            public DateTime DoB { get; set; }
        }

        [FactSqlite]
        public async Task Issue2240_MultiMapAsync_CacheHitAccounting()
        {
            using var connection = GetSQLiteConnection();
            SqlMapper.PurgeQueryCache();

            var sqlGeneric = "SELECT 1 AS Id, 'Alice' AS Name, 2 AS ExtraId, 'Bob' AS ExtraName WHERE 0 /* issue2240_generic */";
            await connection.QueryAsync<HazNameId, HazNameId, int>(
                sqlGeneric,
                (a, b) => a.Id + b.Id,
                splitOn: "ExtraId").ConfigureAwait(false);

            var genericHits = SqlMapper.GetCachedSQL().Single(x => x.Item2 == sqlGeneric).Item3;
            Assert.Equal(0, genericHits);

            // Second execution of the same query should count as reuse (hit count 1)
            await connection.QueryAsync<HazNameId, HazNameId, int>(
                sqlGeneric,
                (a, b) => a.Id + b.Id,
                splitOn: "ExtraId").ConfigureAwait(false);

            genericHits = SqlMapper.GetCachedSQL().Single(x => x.Item2 == sqlGeneric).Item3;
            Assert.Equal(1, genericHits);

            var sqlTypes = "SELECT 1 AS Id, 'Alice' AS Name, 2 AS ExtraId, 'Bob' AS ExtraName WHERE 0 /* issue2240_types */";
            await connection.QueryAsync<int>(
                sqlTypes,
                new[] { typeof(HazNameId), typeof(HazNameId) },
                objs => ((HazNameId)objs[0]).Id + ((HazNameId)objs[1]).Id,
                splitOn: "ExtraId").ConfigureAwait(false);

            var typesHits = SqlMapper.GetCachedSQL().Single(x => x.Item2 == sqlTypes).Item3;
            Assert.Equal(0, typesHits);

            // Second execution of the same query should count as reuse (hit count 1)
            await connection.QueryAsync<int>(
                sqlTypes,
                new[] { typeof(HazNameId), typeof(HazNameId) },
                objs => ((HazNameId)objs[0]).Id + ((HazNameId)objs[1]).Id,
                splitOn: "ExtraId").ConfigureAwait(false);

            typesHits = SqlMapper.GetCachedSQL().Single(x => x.Item2 == sqlTypes).Item3;
            Assert.Equal(1, typesHits);
        }

        [FactSqlite]
        public async Task QueryUnbufferedAsync_ReadToEnd_DoesNotCancel()
        {
            using var connection = new CancelCountingConnection(GetSQLiteConnection());
            var results = new List<int>();
            await foreach (var value in connection.QueryUnbufferedAsync<int>("select 1 union all select 2"))
            {
                results.Add(value);
            }
            Assert.Equal(new[] { 1, 2 }, results);
            Assert.Equal(0, connection.Cancels);
        }

        [FactSqlite]
        public async Task QueryUnbufferedAsync_StopEarly_Cancels()
        {
            using var connection = new CancelCountingConnection(GetSQLiteConnection());
            await foreach (var value in connection.QueryUnbufferedAsync<int>("select 1 union all select 2"))
            {
                Assert.Equal(1, value);
                break;
            }
            Assert.Equal(1, connection.Cancels);
        }

        [FactSqlite]
        public async Task QueryUnbufferedAsync_NoColumns_DoesNotCancel()
        {
            using var connection = new CancelCountingConnection(GetSQLiteConnection());
            await foreach (var value in connection.QueryUnbufferedAsync<int>("create temp table UnbufferedNoColumns (Id int)"))
            {
                Assert.Fail("no rows expected");
            }
            Assert.Equal(0, connection.Cancels);
        }

        // counts DbCommand.Cancel calls; everything else is passed through
        private sealed class CancelCountingConnection(DbConnection inner) : DbConnection
        {
            public int Cancels;
#pragma warning disable CS8765 // nullability differs between target frameworks
            public override string ConnectionString { get => inner.ConnectionString; set => inner.ConnectionString = value; }
#pragma warning restore CS8765
            public override string Database => inner.Database;
            public override string DataSource => inner.DataSource;
            public override string ServerVersion => inner.ServerVersion;
            public override ConnectionState State => inner.State;
            public override void ChangeDatabase(string databaseName) => inner.ChangeDatabase(databaseName);
            public override void Close() => inner.Close();
            public override void Open() => inner.Open();
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => inner.BeginTransaction(isolationLevel);
            protected override DbCommand CreateDbCommand() => new CountingCommand(this, inner.CreateCommand());
            protected override void Dispose(bool disposing)
            {
                if (disposing) inner.Dispose();
                base.Dispose(disposing);
            }

            private sealed class CountingCommand(CancelCountingConnection owner, DbCommand inner) : DbCommand
            {
#pragma warning disable CS8765 // nullability differs between target frameworks
                public override string CommandText { get => inner.CommandText; set => inner.CommandText = value; }
#pragma warning restore CS8765
                public override int CommandTimeout { get => inner.CommandTimeout; set => inner.CommandTimeout = value; }
                public override CommandType CommandType { get => inner.CommandType; set => inner.CommandType = value; }
                public override bool DesignTimeVisible { get => inner.DesignTimeVisible; set => inner.DesignTimeVisible = value; }
                public override UpdateRowSource UpdatedRowSource { get => inner.UpdatedRowSource; set => inner.UpdatedRowSource = value; }
                protected override DbConnection? DbConnection { get => owner; set { } }
                protected override DbParameterCollection DbParameterCollection => inner.Parameters;
                protected override DbTransaction? DbTransaction { get => inner.Transaction; set => inner.Transaction = value; }
                public override void Cancel()
                {
                    owner.Cancels++;
                    inner.Cancel();
                }
                public override int ExecuteNonQuery() => inner.ExecuteNonQuery();
                public override object? ExecuteScalar() => inner.ExecuteScalar();
                public override void Prepare() => inner.Prepare();
                protected override DbParameter CreateDbParameter() => inner.CreateParameter();
                protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => inner.ExecuteReader(behavior);
                protected override void Dispose(bool disposing)
                {
                    if (disposing) inner.Dispose();
                    base.Dispose(disposing);
                }
            }
        }
    }
}

