using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    [Collection("TransactionTests")]
    public sealed class SystemSqlClientTransactionTests : TransactionTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("TransactionTests")]
    public sealed class MicrosoftSqlClientTransactionTests : TransactionTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class TransactionTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestTransactionCommit()
        {
            try
            {
                connection.Execute("create table #TransactionTest ([ID] int, [Value] varchar(32));");

                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute("insert into #TransactionTest ([ID], [Value]) values (1, 'ABC');", transaction: transaction);

                    transaction.Commit();
                }

                Assert.Equal(1, connection.Query<int>("select count(*) from #TransactionTest;").Single());
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }

        [Fact]
        public void TestTransactionRollback()
        {
            connection.Execute("create table #TransactionTest ([ID] int, [Value] varchar(32));");

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute("insert into #TransactionTest ([ID], [Value]) values (1, 'ABC');", transaction: transaction);

                    transaction.Rollback();
                }

                Assert.Equal(0, connection.Query<int>("select count(*) from #TransactionTest;").Single());
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }

        [Fact]
        public void TestCommandWithInheritedTransaction()
        {
            connection.Execute("create table #TransactionTest ([ID] int, [Value] varchar(32));");

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    var transactedConnection = new TransactedConnection(connection, transaction);

                    transactedConnection.Execute("insert into #TransactionTest ([ID], [Value]) values (1, 'ABC');");

                    transaction.Rollback();
                }

                Assert.Equal(0, connection.Query<int>("select count(*) from #TransactionTest;").Single());
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }
    }
}
