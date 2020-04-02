using System;
using System.Data;
using System.Linq;
using Xunit;
using System.Reflection;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientInterfaceTests : InterfaceTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientInterfaceTests : InterfaceTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class InterfaceTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    { 
        [Fact]
        public void TestProxyIsDirtyBehavior()
        {
            var test = ProxyGenerator.GetInterfaceProxy<IUser>();
            Assert.False(((ProxyGenerator.IProxy)test).IsDirty);
            test.Age = 1;
            Assert.True(((ProxyGenerator.IProxy)test).IsDirty);
            Assert.Single(((ProxyGenerator.IProxy)test).DirtyFields);

            test.Age = 1;
            Assert.Single(((ProxyGenerator.IProxy)test).DirtyFields);

            test.Age = 2;
            Assert.Single(((ProxyGenerator.IProxy)test).DirtyFields);

            test.Name = "a";
            Assert.True(((ProxyGenerator.IProxy)test).IsDirty);
            Assert.Equal(2, ((ProxyGenerator.IProxy)test).DirtyFields.Count());

            ((ProxyGenerator.IProxy)test).MarkAsClean();
            Assert.False(((ProxyGenerator.IProxy)test).IsDirty);
            Assert.Empty(((ProxyGenerator.IProxy)test).DirtyFields);
        }

        [Fact]
        public void TestInterfaceReturn()
        {
            connection.Execute("CREATE TABLE #Users (Id INT NOT NULL IDENTITY(1, 1) PRIMARY KEY, Name nvarchar(50) NOT NULL, Age INT)");
            connection.Execute("INSERT #Users (Name, Age) VALUES ('Alice', 10), ('Bob', 20), ('Charlie', 30)");

            var res = connection.Query<IUser>("SELECT * FROM #Users").ToArray();
            Assert.Equal(3, res.Count());
            for (int i = 0; i < 3; i++)
            {
                Assert.Contains(typeof(IUser), res[i].GetType().GetInterfaces());
            }
            connection.Execute("DROP TABLE #Users");
        }

        [Fact]
        public void TestIsDirtyBehavior()
        {
            connection.Execute("CREATE TABLE #Users (Id INT NOT NULL IDENTITY(1, 1) PRIMARY KEY, Name nvarchar(50) NOT NULL, Age INT)");
            connection.Execute("INSERT INTO #Users (Name, Age) VALUES ('Alice', 10), ('Bob', 20), ('Charlie', 30)");

            var res = connection.Query<IUser>("SELECT * FROM #Users").ToArray();
            Assert.Equal(3, res.Count());
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(3, ((ProxyGenerator.IProxy)res[i]).DirtyFields.Count());
                ((ProxyGenerator.IProxy)res[i]).MarkAsClean();
                Assert.Empty(((ProxyGenerator.IProxy)res[i]).DirtyFields);
                Assert.Equal((i + 1) * 10, res[i].Age);
            }
            res[0].Name += "a";
            Assert.Single(((ProxyGenerator.IProxy)res[0]).DirtyFields);
            Assert.Contains("Name", ((ProxyGenerator.IProxy)res[0]).DirtyFields);


            res = connection.Query<IUser>("SELECT Id, Age FROM #Users").ToArray();
            Assert.Equal(3, res.Count());
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(2, ((ProxyGenerator.IProxy)res[i]).DirtyFields.Count());
                Assert.DoesNotContain("Name", ((ProxyGenerator.IProxy)res[i]).DirtyFields);
            }
            connection.Execute("DROP TABLE #Users");
        }

        [Fact]
        public void TestAttributeCopy()
        {
            var res = connection.Query<IUser>("SELECT 1 as id").FirstOrDefault();
            Assert.Single(res.GetType().GetProperty("Id").GetCustomAttributes(true));
            Assert.Equal(2, res.GetType().GetProperty("Name").GetCustomAttributes(true).Count());
            foreach (var a in res.GetType().GetProperty("Name").GetCustomAttributes(true))
            {
                Assert.True(a is Dummy2Attribute || a is DummyAttribute);
                if (a is Dummy2Attribute)
                {
                    Assert.Equal("abc", ((Dummy2Attribute)a).Status);
                }
            }
        }
    }
    public interface IUser
    {
        [Dummy]
        int Id { get; set; }
        [Dummy]
        [Dummy2("abc")]
        string Name { get; set; }
        int? Age { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DummyAttribute : Attribute
    {

    }
    [AttributeUsage(AttributeTargets.Property)]
    public class Dummy2Attribute : Attribute
    {
        public string Status { get; set; }
        public Dummy2Attribute(string status)
        {
            Status = status;
        }
    }
}
