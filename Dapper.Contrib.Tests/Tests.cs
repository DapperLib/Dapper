﻿using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Extensions;


namespace Dapper.Contrib.Tests
{
    public interface IUser
    {
        [Key]
        int Id { get; set; }
        string Name { get; set; }
        int Age { get; set; }
    }

    public class User : IUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Table("Automobiles")]
    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Tests
    {
        private IDbConnection GetOpenConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlConnection("Data Source=vmt-sql;Initial Catalog=dapper;Integrated Security=SSPI;");
            connection.Open();
            return connection;
        }

        public void TableName()
        {
            using (var connection = GetOpenConnection())
            {
                // tests against "Automobiles" table (Table attribute)
                connection.Insert(new Car {Name = "Volvo"});
                connection.Get<Car>(1).Name.IsEqualTo("Volvo");
                connection.Update(new Car() {Id = 1, Name = "Saab"}).IsEqualTo(true);
                connection.Get<Car>(1).Name.IsEqualTo("Saab");
                connection.Delete(new Car() {Id = 1}).IsEqualTo(true);
                connection.Get<Car>(1).IsNull();
            }
        }

        public void TestSimpleGet()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "Adama", Age = 10 });
                var user = connection.Get<User>(id);
                user.Id.IsEqualTo((int)id);
                user.Name.IsEqualTo("Adama");
                connection.Delete(user);
            }
        }

        public void InsertGetUpdate()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Get<IUser>(3).IsNull();

                var id = connection.Insert(new User {Name = "Adam", Age = 10});

                //get a user with "isdirty" tracking
                var user = connection.Get<IUser>(id);
                user.Name.IsEqualTo("Adam");
                connection.Update(user).IsEqualTo(false);    //returns false if not updated, based on tracking
                user.Name = "Bob";
                connection.Update(user).IsEqualTo(true);    //returns true if updated, based on tracking
                user = connection.Get<IUser>(id);
                user.Name.IsEqualTo("Bob");

                //get a user with no tracking
                var notrackedUser = connection.Get<User>(id);
                notrackedUser.Name.IsEqualTo("Bob");
                connection.Update(notrackedUser).IsEqualTo(true);   //returns true, even though user was not changed
                notrackedUser.Name = "Cecil";
                connection.Update(notrackedUser).IsEqualTo(true);
                connection.Get<User>(id).Name.IsEqualTo("Cecil");

                connection.Query<User>("select * from Users").Count().IsEqualTo(1);
                connection.Delete(user).IsEqualTo(true);
                connection.Query<User>("select * from Users").Count().IsEqualTo(0);

                connection.Update(notrackedUser).IsEqualTo(false);   //returns false, user not found
            }
        }
    }
}
