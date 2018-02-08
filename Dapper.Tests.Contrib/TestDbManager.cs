using System;
using System.Collections.Generic;
using System.Data;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using Xunit;

namespace Dapper.Tests.Contrib
{
    enum Gender
    {
        Male = 0,
        Female
    }

    [Table("person")]
    class Human
    {
        public long Id { get; set; }
        protected string Region { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        public bool Enabled { get; set; }
        public Gender Gender { get; set; }
        public DateTime? CreatedDate { get; set; }
    }


    public class TestDbManager
    {
        const string conStr = "server=LocalHost;User Id=root;Pwd=MYPASSWORD;database=test";
        Random rand = new Random();

        [Fact]
        public void TestSaveChanges()
        {
            var mgr = new DbManager(() => { return new MySqlConnection(conStr); });

            var person = new Human()
            {
                Id = 11,
                Name = "TestDbManager",
                Age = rand.Next(1, 100),
                Address = "TestInsertStrongClassInstanceAddress",
                Enabled = true
            };
            mgr.Insert(person);
            mgr.SaveChanges();

            var queryPerson = mgr.Get<Human>(person.Id);
            Assert.Equal(person.Age, queryPerson.Age);
        }


        [Fact]
        public void TestGetMapper()
        {
            var mgr = new DbManager(() => { return new MySqlConnection(conStr); });

            var person = new Human()
            {
                Id = 11,
                Name = "TestDbManager",
                Age = rand.Next(1, 100),
                Address = "TestInsertStrongClassInstanceAddress",
                Enabled = true
            };

            var mapper = mgr.GetMapper<ISpUserMapper>();
            //var mapper2 = new SpUserMapper2(new MySqlConnection(conStr));

            //var arr2 = mapper2.GetUserNames(2);
            int a = mapper.UpdateUsers("MapperUpdated" + rand.Next(1, 100), 2);
            var arr = mapper.GetUserNames(2);
        }
    }


    public interface ISpUserMapper : ISqlOperationMapper
    {
        [QuerySql("select name from person where age>@age")]
        string[] GetUserNames(int age);

        [QuerySql("select name from person where age>@age")]
        IEnumerable<string> GetUserNames2(int age);

        [ExcuteSql("update person set address=@address where age>@age")]
        int UpdateUsers(string address, int age);
    }

    //public class SpUserMapper2 : ISpUserMapper
    //{
    //    private IDbConnection connection;

    //    public SpUserMapper2(IDbConnection connection) => this.connection = connection;

    //    public string[] GetUserNames(int age)
    //    {
    //        var sql = "select * from person where age>@age";
    //        var dic = new Dictionary<string, dynamic>();
    //        dic.Add("age", age);
    //        return connection.Query<string>(sql, dic).ToArray();
    //    }

    //    public IEnumerable<string> GetUserNames2(int age)
    //    {
    //        var sql = "select * from person where age>@age";
    //        var dic = new Dictionary<string, dynamic>();
    //        dic.Add("age", age);
    //        return connection.Query<string>(sql, dic);
    //    }

    //    public int UpdateUsers(string address, int age)
    //    {
    //        var sql = "111";
    //        var dic = new Dictionary<string, dynamic>();
    //        dic.Add("address", address);
    //        dic.Add("age", age);
    //        return connection.Execute(sql, dic);
    //    }
    //}
}
