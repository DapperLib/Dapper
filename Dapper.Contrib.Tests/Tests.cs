using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System;
using Dapper;
using Npgsql;


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

    [Table("Houses")]
    public class Houses
    {
        [Key]
        public int Id { get; set; }
        public int Number { get; set; }
        public string Road { get; set; }
    }


    public abstract class Tests
    {
        protected abstract IDbConnection GetOpenConnection();
        protected abstract string ConnectionString { get; }
        public abstract void SetUpTests();

        public void TableName()
        {
            using (var connection = GetOpenConnection())
            {
                // tests against "Automobiles" table (Table attribute)
                connection.Insert(new Car {Name = "Volvo"});
                connection.Get<Car>(1).Name.IsEqualTo("Volvo");
                connection.Update(new Car {Id = 1, Name = "Saab"}).IsEqualTo(true);
                connection.Get<Car>(1).Name.IsEqualTo("Saab");
                connection.Delete(new Car {Id = 1}).IsEqualTo(true);
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

        public void InsertCheckKey()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Get<IUser>(3).IsNull();
                User user = new User { Name = "Adamb", Age = 10 };
                int id = (int)connection.Insert(user);
                user.Id.IsEqualTo(id);
            }
        }

        public void InsertAndUpdateExplicitKey()
        {
            using (var connection = GetOpenConnection())
            {
                if (SqlMapperExtensions.GetFormatter(connection) is PostgresAdapter)
                {
                    //Create the object using a defined primary key (instead of DEFAULT).
                    connection.Get<Houses>(99).IsNull();
                    Houses user = new Houses {Id = 99, Number = 43, Road = "House St"};
                    bool gotException = false;
                    try
                    {
                        connection.Insert(user); //This should fail, as the primary key will be excluded from the query.
                    }
                    catch (NpgsqlException)
                    {
                        gotException = true;
                    }
                    catch (Exception)
                    {
                    }
                    gotException.IsEqualTo(true);
                    int id = (int)connection.Insert(user, useDefaultForKeyValues: false); //No key returned by db due to lack of identity.
                    user.Id.IsEqualTo(id);

                    //Update the item.
                    user.Number = 44;
                    connection.Update(user);

                    //Ensure that the new item was updated using the primary key.
                    user.Number.IsEqualTo(connection.Query<Houses>("SELECT * FROM houses WHERE Id = :id", new {id = 99}).First().Number);
                }
            }
        }

        public void BuilderSelectClause()
        {
            using (var connection = GetOpenConnection())
            {
                var rand = new Random(8675309);
                var data = new List<User>();
                for (int i = 0; i < 100; i++)
                {
                    var nU = new User { Age = rand.Next(70), Id = i, Name = Guid.NewGuid().ToString() };
                    data.Add(nU);
                    nU.Id = (int)connection.Insert<User>(nU);
                }

                var builder = new SqlBuilder();
                var justId = builder.AddTemplate("SELECT /**select**/ FROM Users");
                var all = builder.AddTemplate("SELECT Name, /**select**/, Age FROM Users");

                builder.Select("Id");

                var ids = connection.Query<int>(justId.RawSql, justId.Parameters);
                var users = connection.Query<User>(all.RawSql, all.Parameters);

                foreach (var u in data)
                {
                    if (!ids.Any(i => u.Id == i)) throw new Exception("Missing ids in select");
                    if (!users.Any(a => a.Id == u.Id && a.Name == u.Name && a.Age == u.Age)) throw new Exception("Missing users in select");
                }
            }
        }

        public void BuilderTemplateWOComposition()
        {
            var builder = new SqlBuilder();
            var template = builder.AddTemplate("SELECT COUNT(*) AS count FROM Users WHERE Age = @age", new {age = 5});

            if (template.RawSql == null) throw new Exception("RawSql null");
            if (template.Parameters == null) throw new Exception("Parameters null");

            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Age = 5, Name = "Testy McTestington" });
                var v = connection.Query(template.RawSql, template.Parameters).First();
                if (Convert.ToInt32(v.count) != 1)
                    throw new Exception("Query failed");
            }
        }
    }
}
