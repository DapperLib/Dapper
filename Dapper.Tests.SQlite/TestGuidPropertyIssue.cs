using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.Tests.SQlite
{

    /// <summary>
    ///  Test the handling of classes with guid type properties and persisting them to a sqlite database.
    /// </summary>
    public class TestGuidPropertyIssue
    {
        private const string TEST_STRING = "Test Name";
        private const string CREATE_STATEMENT = @"CREATE TABLE IF NOT EXISTS SomeTable (Id UNIQUEIDENTIFIER, Name NVARCHAR(255))";
        private const string INSERT_STATEMENT = @"INSERT INTO SomeTable (Id, Name) VALUES(@Id, @Name)";
        private const string SELECT_STATEMENT = @"SELECT * FROM SomeTable";

        private static SqliteConnection GetSQLiteConnection(bool open = true)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            if (open) connection.Open();
            return connection;
        }

        public TestGuidPropertyIssue()
        {
            // Add the new Guid handler for all the tests.
            SqlMapper.AddTypeHandler(new GuidHandler());
        }

        [Fact]
        public void Test_select_of_class_with_guid_property_and_default_constructor()
        {
            using (var connection = GetSQLiteConnection())
            {
                // Arrange.
                var seed = Guid.NewGuid();

                var expectedRes = new ClassWithGuidPropertyWithDefaultConstructor()
                {
                    Id = seed,
                    Name = TEST_STRING
                };

                connection.Execute(CREATE_STATEMENT);
                connection.Execute(INSERT_STATEMENT, expectedRes);

                // Act.
                var res = connection.Query<ClassWithGuidPropertyWithDefaultConstructor>(SELECT_STATEMENT).FirstOrDefault();

                // Assert.
                Assert.Equal(expectedRes, res);
            }
        }

        [Fact]
        public void Test_select_of_class_with_guid_property_and_no_default_constructor()
        {
            using (var connection = GetSQLiteConnection())
            {
                // Arrange.
                var seed = Guid.NewGuid();

                var expectedRes = new ClassWithGuidPropertyWithNoDefaultConstructor( seed, TEST_STRING);

                connection.Execute(CREATE_STATEMENT);
                connection.Execute(INSERT_STATEMENT, expectedRes);

                // Act.
                var res = connection.Query<ClassWithGuidPropertyWithNoDefaultConstructor>(SELECT_STATEMENT).FirstOrDefault();

                // Assert.
                Assert.Equal(expectedRes, res);
            }
        }

        [Fact]
        public void Test_Insert_Using_Anonymous_Object()
        {
            using (var connection = GetSQLiteConnection())
            {
                // Arrange.
                var seed = Guid.NewGuid();

                var expectedRes = new ClassWithGuidPropertyWithNoDefaultConstructor(seed, TEST_STRING);

                connection.Execute(CREATE_STATEMENT);
                connection.Execute(INSERT_STATEMENT, new
                {
                    Id = seed,
                    Name = "Test Name"
                });

                // Act.
                var res = connection.Query<ClassWithGuidPropertyWithNoDefaultConstructor>(SELECT_STATEMENT).FirstOrDefault();

                // Assert.
                Assert.Equal(expectedRes, res);
            }
        }

    }
}
