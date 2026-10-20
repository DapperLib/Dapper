#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using pengdows.crud.enums;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbErrorTests
    {
        [Fact]
        public void QueryFirst_ThrowsInvalidOperationException_OnEmptyResult()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            Assert.Throws<InvalidOperationException>(() =>
                conn.QueryFirst<User>("SELECT Id, Name FROM Users WHERE 1=0"));
        }

        [Fact]
        public void QuerySingle_ThrowsInvalidOperationException_OnMultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            Assert.Throws<InvalidOperationException>(() =>
                conn.QuerySingle<User>("SELECT Id, Name FROM Users"));
        }

        [Fact]
        public void ConnectionFailOnOpen_ThrowsWhenOpen()
        {
            var factory = new fakeDbFactory(SupportedDatabase.Sqlite);
            var conn = (fakeDbConnection)factory.CreateConnection();
            conn.SetFailOnOpen(true, false);

            Assert.ThrowsAny<Exception>(() => conn.Open());
        }

        [Fact]
        public void ConnectionFailOnOpen_WithCustomException_ThrowsThatException()
        {
            var factory = fakeDbFactory.CreateFailingFactory(
                SupportedDatabase.Sqlite,
                ConnectionFailureMode.FailOnOpen,
                new TimeoutException("db timeout"),
                null);
            var conn = factory.CreateConnection();

            Assert.Throws<TimeoutException>(() => conn.Open());
        }

        [Fact]
        public void ConnectionFailAfterCount_FailsAfterNthOpen()
        {
            var factory = fakeDbFactory.CreateFailingFactory(
                SupportedDatabase.Sqlite,
                ConnectionFailureMode.FailAfterCount,
                new InvalidOperationException("too many opens"),
                2);
            var conn = (fakeDbConnection)factory.CreateConnection();

            conn.Open(); conn.Close(); // 1st – ok
            conn.Open(); conn.Close(); // 2nd – ok

            Assert.ThrowsAny<Exception>(() => conn.Open()); // 3rd – fails
        }

        [Fact]
        public void ResetFailureConditions_AllowsConnectionToWork()
        {
            var factory = new fakeDbFactory(SupportedDatabase.Sqlite);
            var conn = (fakeDbConnection)factory.CreateConnection();
            conn.SetFailOnOpen(true, false);
            conn.ResetFailureConditions();

            conn.Open();
            Assert.Equal(ConnectionState.Open, conn.State);
            conn.Close();
        }
    }
}
#endif
