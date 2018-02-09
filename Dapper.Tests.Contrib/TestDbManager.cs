using System;
using System.Collections.Generic;
using System.Data;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using Xunit;

namespace Dapper.Tests.Contrib
{
    public interface ISpUserMapper : ISqlOperationMapper
    {
        [QuerySql("select name from Users where age>@age")]
        string[] GetUserNames(int age);

        [ExcuteSql("update Users set name=@name where age>@age")]
        int UpdateUsers(string name, int age);
    }

    public abstract partial class TestSuite
    {
        Random rand = new Random();

        [Fact]
        public void TestSaveChanges()
        {
            var mgr = new DbManager(() => { return GetConnection(); });

            var user = new User()
            {
                Name = "TestDbManagerName",
                Age = rand.Next(1, 100)
            };
            mgr.Insert(user);
            mgr.SaveChanges();

            var queryUser = mgr.Get<User>(user.Id);
            Assert.Equal(user.Age, queryUser.Age);
        }


        [Fact]
        public void TestGetMapper()
        {
            var mgr = new DbManager(() => { return GetConnection(); });
            const int numberOfEntities = 10;
            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });
            mgr.Insert(users);
            mgr.SaveChanges();
            var mapper = mgr.GetMapper<ISpUserMapper>();
            var originMames = mapper.GetUserNames(2);
            int count = mapper.UpdateUsers("MapperUpdatedName", 2);
            var names = mapper.GetUserNames(2);
            Assert.Equal(count, names.Length);
            Assert.NotEqual(originMames[0], names[0]);
        }
    }


}
