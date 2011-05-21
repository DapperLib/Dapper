using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlServerCe;
using System.Diagnostics;
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
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class Tests
    {
        private IDbConnection GetOpenConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlCeConnection("Data Source = " + projFolder + "\\Test.sdf;");
            connection.Open();
            return connection;
        }

        public void Get()
        {
            using (var connection = GetOpenConnection())
            {
                try
                {
                    var user = connection.Get<User>(1);
                    Debug.Fail("Fail, should have thrown exception");
                }
                catch (Exception)
                {
                    Console.WriteLine("ok");
                }
            }

        }

        public void InsertGetUpdate()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User {Name = "Adam", Age = 10});
                id.IsEqualTo(1);
                var user = connection.Get<IUser>(id);
                user.Name.IsEqualTo("Adam");
                connection.Update(user).IsEqualTo(false);    //returns false if not updated, based on tracking
                user.Name = "Bob";
                connection.Update(user).IsEqualTo(true);    //returns true if updated, based on tracking
                user = connection.Get<IUser>(id);
                user.Name.IsEqualTo("Bob");
                connection.Query<User>("select * from Users").Count().Equals(2);
                connection.Delete(user).Equals(true);
                connection.Query<User>("select * from Users").Count().Equals(1);
            }
        }
    }
}
