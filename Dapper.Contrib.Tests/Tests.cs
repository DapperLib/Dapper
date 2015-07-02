using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;

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

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Table("Stuff")]
    public class Stuff
    {
        [Key]
        public short TheId { get; set; }
        public string Name { get; set; }
        public DateTime? Created { get; set; }
    }

    [Table("Automobiles")]
    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Computed]
        public string Computed { get; set; }
    }

    [Table("Results")]
    public class Result
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
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

        private IDbConnection GetConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlCeConnection("Data Source = " + projFolder + "\\Test.sdf;");
            return connection;
        }

        public void ShortIdentity()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Stuff() { Name = "First item" });
                id.IsEqualTo(1);
                var item = connection.Get<Stuff>(1);
                item.TheId.IsEqualTo((short)1);
            }
        }

        public void NullDateTime()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new Stuff() { Name = "First item" });
                connection.Insert(new Stuff() { Name = "Second item", Created = DateTime.Now });
                var stuff = connection.Query<Stuff>("select * from stuff").ToList();
                stuff.First().Created.IsNull();
                stuff.Last().Created.IsNotNull();

            }
        }

        public void TableName()
        {
            using (var connection = GetOpenConnection())
            {
                // tests against "Automobiles" table (Table attribute)
                connection.Insert(new Car { Name = "Volvo" }).IsEqualTo(1);
                connection.Get<Car>(1).Name.IsEqualTo("Volvo");
                connection.Update(new Car() { Id = 1, Name = "Saab" }).IsEqualTo(true);
                connection.Get<Car>(1).Name.IsEqualTo("Saab");
                connection.Delete(new Car() { Id = 1 }).IsEqualTo(true);
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

        public void TestClosedConnection()
        {
            using (var connection = GetConnection())
            {
                connection.Insert(new User { Name = "Adama", Age = 10 }).IsMoreThan(0);
                var users = connection.GetAll<User>();
                users.Count().IsMoreThan(0);
            }
        }

        public void InsertList()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
            }

        }

        public void UpdateList()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
                foreach (var user in users)
                {
                    user.Name = user.Name + " updated";
                }
                connection.Update(users);
                var name = connection.Query<User>("select * from users").First().Name;
                name.Contains("updated").IsTrue();
            }

        }

        public void DeleteList()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities);

                var usersToDelete = users.Take(10).ToList();
                connection.Delete(usersToDelete);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities - 10);
            }

        }

        public void InsertGetUpdate()
        {
            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();
                connection.Get<User>(3).IsNull();

                //insert with computed attribute that should be ignored
                connection.Insert(new Car { Name = "Volvo", Computed = "this property should be ignored" });

                var id = connection.Insert(new User { Name = "Adam", Age = 10 });

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

        public void InsertWithCustomDbType()
        {
            SqlMapperExtensions.GetDatabaseType = conn => "SQLiteConnection";

            bool sqliteCodeCalled = false;
            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();
                connection.Get<User>(3).IsNull();
                try
                {
                    var id = connection.Insert(new User { Name = "Adam", Age = 10 });
                }
                catch (SqlCeException ex)
                {
                    sqliteCodeCalled = ex.Message.IndexOf("There was an error parsing the query", StringComparison.InvariantCultureIgnoreCase) >= 0;
                }
                catch (Exception)
                {
                }
            }
            SqlMapperExtensions.GetDatabaseType = null;

            if (!sqliteCodeCalled)
            {
                throw new Exception("Was expecting sqlite code to be called");
            }
        }

        public void InsertWithCustomTableNameMapper()
        {

            SqlMapperExtensions.TableNameMapper = (type) =>
            {
                switch (type.Name)
                {
                    case "Person":
                        return "People";
                    default:
                        var tableattr = type.GetCustomAttributes(false).SingleOrDefault(attr => attr.GetType().Name == "TableAttribute") as dynamic;
                        if (tableattr != null)
                            return tableattr.Name;

                        var name = type.Name + "s";
                        if (type.IsInterface && name.StartsWith("I"))
                            name = name.Substring(1);
                        return name;
                }
            };

            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Person() { Name = "Mr Mapper" });
                id.IsEqualTo(1);
                var people = connection.GetAll<Person>();
            }
        }

        public void GetAll()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.GetAll<User>().ToList();
                users.Count.IsEqualTo(numberOfEntities);
                var iusers = connection.GetAll<IUser>().ToList();
                iusers.Count.IsEqualTo(numberOfEntities);
            }

        }

        public void Transactions()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Car { Name = "one car" });   //insert outside transaction

                var tran = connection.BeginTransaction();
                var car = connection.Get<Car>(id, tran);
                var orgName = car.Name;
                car.Name = "Another car";
                connection.Update(car, tran);
                tran.Rollback();

                car = connection.Get<Car>(id);  //updates should have been rolled back
                car.Name.IsEqualTo(orgName);
            }
        }

        public void TransactionScope()
        {
            using (var connection = GetConnection())
            {
                using (var txscope = new TransactionScope())
                {
                    connection.Open();  //connection MUST be opened inside the transactionscope

                    var id = connection.Insert(new Car { Name = "one car" });   //inser car within transaction

                    txscope.Dispose();  //rollback

                    connection.Get<Car>(id).IsNull();   //returns null - car with that id should not exist
                }
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
                    nU.Id = (int)connection.Insert(nU);
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
            var template = builder.AddTemplate("SELECT COUNT(*) FROM Users WHERE Age = @age", new { age = 5 });

            if (template.RawSql == null) throw new Exception("RawSql null");
            if (template.Parameters == null) throw new Exception("Parameters null");

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();
                connection.Insert(new User { Age = 5, Name = "Testy McTestington" });

                if (connection.Query<int>(template.RawSql, template.Parameters).Single() != 1)
                    throw new Exception("Query failed");
            }
        }

        public void InsertFieldWithReservedName()
        {
            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();
                var id = connection.Insert(new Result() { Name = "Adam", Order = 1 });

                var result = connection.Get<Result>(id);
                result.Order.IsEqualTo(1);
            }

        }

        public void DeleteAll()
        {
            using (var connection = GetOpenConnection())
            {
                var id1 = connection.Insert(new User() { Name = "Alice", Age = 32 });
                var id2 = connection.Insert(new User() { Name = "Bob", Age = 33 });
                connection.DeleteAll<User>().IsTrue();
                connection.Get<User>(id1).IsNull();
                connection.Get<User>(id2).IsNull();
            }
        }

    }
}

