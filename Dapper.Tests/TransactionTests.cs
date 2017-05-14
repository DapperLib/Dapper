﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public class TransactionTests : TestBase
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

                connection.Query<int>("select count(*) from #TransactionTest;").Single().IsEqualTo(1);
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

                connection.Query<int>("select count(*) from #TransactionTest;").Single().IsEqualTo(0);
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

                connection.Query<int>("select count(*) from #TransactionTest;").Single().IsEqualTo(0);
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }
    }
}
