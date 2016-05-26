using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Dapper.Contrib.Extensions;

#if !COREFX
using System.Data.SqlServerCe;
using System.Transactions;
#endif
#if XUNIT2
using FactAttribute = Dapper.Tests.Contrib.SkippableFactAttribute;
#else
using Xunit;
#endif

namespace Dapper.Tests.Contrib
{
    [Table("ObjectX")]
    public class ObjectX
    {
        [ExplicitKey]
        public string ObjectXId { get; set; }
        public string Name { get; set; }
    }

    [Table("ObjectY")]
    public class ObjectY
    {
        [ExplicitKey]
        public int ObjectYId { get; set; }
        public string Name { get; set; }
    }
    
    [Table("ObjectZ")]
    public class ObjectZ
    {
        [ExplicitKey]
        public int Id { get; set; }
        public string Name { get; set; }
    }

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

    public abstract partial class TestSuite
    {
        protected static readonly bool IsAppVeyor = Environment.GetEnvironmentVariable("Appveyor")?.ToUpperInvariant() == "TRUE";

        public abstract IDbConnection GetConnection();

        private IDbConnection GetOpenConnection()
        {
            var connection = GetConnection();
            connection.Open();
            return connection;
        }

        [Fact]
        public void Issue418()
        {
            using (var connection = GetOpenConnection())
            {
                //update first (will fail) then insert
                //added for bug #418
                var updateObject = new ObjectX
                {
                    ObjectXId = Guid.NewGuid().ToString(),
                    Name = "Someone"
                };
                var updates = connection.Update(updateObject);
                updates.IsFalse();

                connection.DeleteAll<ObjectX>();

                var objectXId = Guid.NewGuid().ToString();
                var insertObject = new ObjectX
                {
                    ObjectXId = objectXId,
                    Name = "Someone else"
                };
                connection.Insert(insertObject);
                var list = connection.GetAll<ObjectX>();
                list.Count().IsEqualTo(1);
            }
        }

        /// <summary>
        /// Tests for issue #351 
        /// </summary>
        [Fact]
        public void InsertGetUpdateDeleteWithExplicitKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = Guid.NewGuid().ToString();
                var o1 = new ObjectX { ObjectXId = guid, Name = "Foo" };
                var originalxCount = connection.Query<int>("Select Count(*) From ObjectX").First();
                connection.Insert(o1);
                var list1 = connection.Query<ObjectX>("select * from ObjectX").ToList();
                list1.Count.IsEqualTo(originalxCount + 1);
                o1 = connection.Get<ObjectX>(guid);
                o1.ObjectXId.IsEqualTo(guid);
                o1.Name = "Bar";
                connection.Update(o1);
                o1 = connection.Get<ObjectX>(guid);
                o1.Name.IsEqualTo("Bar");
                connection.Delete(o1);
                o1 = connection.Get<ObjectX>(guid);
                o1.IsNull();

                const int id = 42;
                var o2 = new ObjectY { ObjectYId = id, Name = "Foo" };
                var originalyCount = connection.Query<int>("Select Count(*) From ObjectY").First();
                connection.Insert(o2);
                var list2 = connection.Query<ObjectY>("select * from ObjectY").ToList();
                list2.Count.IsEqualTo(originalyCount + 1);
                o2 = connection.Get<ObjectY>(id);
                o2.ObjectYId.IsEqualTo(id);
                o2.Name = "Bar";
                connection.Update(o2);
                o2 = connection.Get<ObjectY>(id);
                o2.Name.IsEqualTo("Bar");
                connection.Delete(o2);
                o2 = connection.Get<ObjectY>(id);
                o2.IsNull();
            }
        }
        
        [Fact]
        public void GetAllWithExplicitKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = Guid.NewGuid().ToString();
                var o1 = new ObjectX { ObjectXId = guid, Name = "Foo" };
                connection.Insert(o1);

                var objectXs = connection.GetAll<ObjectX>().ToList();
                objectXs.Count.IsMoreThan(0);
                objectXs.Count(x => x.ObjectXId== guid).IsEqualTo(1);
            }
        }

        [Fact]
        public void InsertGetUpdateDeleteWithExplicitKeyNamedId()
        {
            using (var connection = GetOpenConnection())
            {
                const int id = 42;
                var o2 = new ObjectZ { Id = id, Name = "Foo" };
                connection.Insert(o2);
                var list2 = connection.Query<ObjectZ>("select * from ObjectZ").ToList();
                list2.Count.IsEqualTo(1);
                o2 = connection.Get<ObjectZ>(id);
                o2.Id.IsEqualTo(id);
                //o2.Name = "Bar";
                //connection.Update(o2);
                //o2 = connection.Get<ObjectY>(id);
                //o2.Name.IsEqualTo("Bar");
                //connection.Delete(o2);
                //o2 = connection.Get<ObjectY>(id);
                //o2.IsNull();
            }
        }
        
        [Fact]
        public void ShortIdentity()
        {
            using (var connection = GetOpenConnection())
            {
                const string name = "First item";
                var id = connection.Insert(new Stuff { Name = name });
                id.IsMoreThan(0); // 1-n are valid here, due to parallel tests
                var item = connection.Get<Stuff>(id);
                item.TheId.IsEqualTo((short)id);
                item.Name.IsEqualTo(name);
            }
        }

        [Fact]
        public void NullDateTime()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new Stuff { Name = "First item" });
                connection.Insert(new Stuff { Name = "Second item", Created = DateTime.Now });
                var stuff = connection.Query<Stuff>("select * from Stuff").ToList();
                stuff.First().Created.IsNull();
                stuff.Last().Created.IsNotNull();

            }
        }

        [Fact]
        public void TableName()
        {
            using (var connection = GetOpenConnection())
            {
                // tests against "Automobiles" table (Table attribute)
                var id = connection.Insert(new Car { Name = "Volvo" });
                var car = connection.Get<Car>(id);
                car.IsNotNull();
                car.Name.IsEqualTo("Volvo");
                connection.Get<Car>(id).Name.IsEqualTo("Volvo");
                connection.Update(new Car { Id = (int)id, Name = "Saab" }).IsEqualTo(true);
                connection.Get<Car>(id).Name.IsEqualTo("Saab");
                connection.Delete(new Car { Id = (int)id }).IsEqualTo(true);
                connection.Get<Car>(id).IsNull();
            }
        }

        [Fact]
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

        [Fact]
        public void TestClosedConnection()
        {
            using (var connection = GetConnection())
            {
                connection.Insert(new User { Name = "Adama", Age = 10 }).IsMoreThan(0);
                var users = connection.GetAll<User>();
                users.Count().IsMoreThan(0);
            }
        }

        [Fact]
        public void InsertArray()
        {
            InsertHelper(src => src.ToArray());
        }

        [Fact]
        public void InsertList()
        {
            InsertHelper(src => src.ToList());
        }

        private void InsertHelper<T>(Func<IEnumerable<User>, T> helper)
            where T : class
        {
            const int numberOfEntities = 10;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(helper(users));
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
            }
        }

        [Fact]
        public void UpdateArray()
        {
            UpdateHelper(src => src.ToArray());
        }

        [Fact]
        public void UpdateList()
        {
            UpdateHelper(src => src.ToList());
        }

        private void UpdateHelper<T>(Func<IEnumerable<User>, T> helper)
            where T : class
        {
            const int numberOfEntities = 10;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(helper(users));
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
                foreach (var user in users)
                {
                    user.Name = user.Name + " updated";
                }
                connection.Update(helper(users));
                var name = connection.Query<User>("select * from Users").First().Name;
                name.Contains("updated").IsTrue();
            }
        }

        [Fact]
        public void DeleteArray()
        {
            DeleteHelper(src => src.ToArray());
        }

        [Fact]
        public void DeleteList()
        {
            DeleteHelper(src => src.ToList());
        }

        private void DeleteHelper<T>(Func<IEnumerable<User>, T> helper)
            where T : class
        {
            const int numberOfEntities = 10;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                connection.DeleteAll<User>();

                var total = connection.Insert(helper(users));
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities);

                var usersToDelete = users.Take(10).ToList();
                connection.Delete(helper(usersToDelete));
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities - 10);
            }
        }

        [Fact]
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

#if !COREFX
        [Fact(Skip = "Not parallel friendly - thinking about how to test this")]
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
                    connection.Insert(new User { Name = "Adam", Age = 10 });
                }
                catch (SqlCeException ex)
                {
                    sqliteCodeCalled = ex.Message.IndexOf("There was an error parsing the query", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                // ReSharper disable once EmptyGeneralCatchClause
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
#endif

        [Fact]
        public void InsertWithCustomTableNameMapper()
        {
            SqlMapperExtensions.TableNameMapper = type =>
            {
                switch (type.Name())
                {
                    case "Person":
                        return "People";
                    default:
                        var tableattr = type.GetCustomAttributes(false).SingleOrDefault(attr => attr.GetType().Name == "TableAttribute") as dynamic;
                        if (tableattr != null)
                            return tableattr.Name;

                        var name = type.Name + "s";
                        if (type.IsInterface() && name.StartsWith("I"))
                            name = name.Substring(1);
                        return name;
                }
            };

            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Person { Name = "Mr Mapper" });
                id.IsEqualTo(1);
                connection.GetAll<Person>();
            }
        }

        [Fact]
        public void GetAll()
        {
            const int numberOfEntities = 10;

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
                for (var i = 0; i < numberOfEntities; i++)
                    iusers[i].Age.IsEqualTo(i);
            }

        }

        [Fact]
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

#if !COREFX
        [Fact]
        public void TransactionScope()
        {
            using (var txscope = new TransactionScope())
            {
                using (var connection = GetOpenConnection())
                {
                    var id = connection.Insert(new Car { Name = "one car" });   //inser car within transaction

                    txscope.Dispose();  //rollback

                    connection.Get<Car>(id).IsNull();   //returns null - car with that id should not exist
                }
            }
        }
#endif

        [Fact]
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

        [Fact]
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

        [Fact]
        public void BuilderTemplateWithoutComposition()
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

        [Fact]
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

        [Fact]
        public void DeleteAll()
        {
            using (var connection = GetOpenConnection())
            {
                var id1 = connection.Insert(new User { Name = "Alice", Age = 32 });
                var id2 = connection.Insert(new User { Name = "Bob", Age = 33 });
                connection.DeleteAll<User>().IsTrue();
                connection.Get<User>(id1).IsNull();
                connection.Get<User>(id2).IsNull();
            }
        }
    }
}

