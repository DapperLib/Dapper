using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace Dapper.Tests
{
    public class CallbackTests : TestBase
    {
        [Fact]
        [Trait("Category", "aaa")]
        public void TestExecuteCallbackCommand()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            connection.Execute(commandDef);

            Assert.Equal(5, r.Value);
        }

        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestExecuteCallbackCommandAsync()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            await connection.ExecuteAsync(commandDef);

            Assert.Equal(5, r.Value);
        }

        [Fact]
        [Trait("Category", "aaa")]
        public void TestExecuteScalerCallbackCommand()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            connection.ExecuteScalar(commandDef);

            Assert.Equal(5, r.Value);
        }

        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestExecuteScalerCallbackCommandAsync()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            await connection.ExecuteScalarAsync(commandDef);

            Assert.Equal(5, r.Value);
        }

        [Fact]
        [Trait("Category", "aaa")]
        public void TestExecuteMultipleCallbackCommand()
        {
            connection.Execute("create table #tt(i int)");
            try
            {
                //Callback increases the value by one
                var callback = new Action<IDbCommand>(cmd =>
                {
                    var p = (SqlParameter)cmd.Parameters[0];
                    p.Value = ((int)p.Value) + 1;
                });

                var command = new CommandDefinition("insert #tt (i) values(@a)", new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } }, beforeExecute: callback);
                int tally = connection.Execute(command);
                int sum = connection.Query<int>("select sum(i) from #tt").First();
                Assert.Equal(4, tally);
                Assert.Equal(14, sum);

            }
            finally
            {
                connection.Execute("drop table #tt");
            }
        }

        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestExecuteMultipleCallbackCommandAsync()
        {
            connection.Execute("create table #tt(i int)");
            try
            {
                //Callback increases the value by one
                var callback = new Action<IDbCommand>(cmd =>
                {
                    var p = (SqlParameter)cmd.Parameters[0];
                    p.Value = ((int)p.Value) + 1;
                });

                var command = new CommandDefinition("insert #tt (i) values(@a)", new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } }, beforeExecute: callback);
                int tally = await connection.ExecuteAsync(command);
                int sum = connection.Query<int>("select sum(i) from #tt").First();
                Assert.Equal(4, tally);
                Assert.Equal(14, sum);

            }
            finally
            {
                connection.Execute("drop table #tt");
            }
        }

        [Fact]
        [Trait("Category", "aaa")]
        public void TestExecuteReaderCallback()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            connection.ExecuteReader(commandDef);
            Assert.Equal(5, r.Value);

        }
        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestExecuteReaderCallbackAsync()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            await connection.ExecuteReaderAsync(commandDef);
            Assert.Equal(5, r.Value);

        }

        [Fact]
        [Trait("Category", "aaa")]
        public void TestQueryCallback()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            connection.Query<dynamic>(commandDef);
            Assert.Equal(5, r.Value);

        }

        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestQueryCallbackAsync()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            await connection.QueryAsync<dynamic>(commandDef);
            Assert.Equal(5, r.Value);

        }
        [Fact]
        [Trait("Category", "aaa")]
        public void TestQueryMultipleCallback()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            connection.QueryMultiple(commandDef);
            Assert.Equal(5, r.Value);

        }

        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestQueryMultipleCallbackAsync()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            await  connection.QueryMultipleAsync(commandDef);
            Assert.Equal(5, r.Value);

        }

        [Fact]
        [Trait("Category", "aaa")]
        public void TestQueryFirstCallback()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            connection.QueryFirstOrDefault<dynamic>(commandDef);
            Assert.Equal(5, r.Value);


        }

        [Fact]
        [Trait("Category", "aaa")]
        public async Task TestQueryFirstCallbackAsync()
        {
            IDbDataParameter r = null;

            var callback = new Action<IDbCommand>(cmd =>
            {
                r = cmd.CreateParameter();
                r.ParameterName = "@R";
                r.Direction = ParameterDirection.Output;
                r.DbType = DbType.Int32;
                cmd.Parameters.Add(r);
            });

            var commandDef = new CommandDefinition("select @R = Id from (select 5 as id) a", beforeExecute: callback);
            await connection.QueryFirstOrDefaultAsync<dynamic>(commandDef);
            Assert.Equal(5, r.Value);


        }

    }
}
