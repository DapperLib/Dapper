using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Dapper.Contrib.Extensions;

namespace Dapper.Contrib.Tests
{
    public class TestsAsync
    {
        private IDbConnection GetOpenConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlCeConnection("Data Source = " + projFolder + "\\Test.sdf;");
            connection.Open();
            return connection;
        }

        public async Task TableNameAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                // tests against "Automobiles" table (Table attribute)
                await connection.InsertAsync(new Car { Name = "Volvo" });
                (await connection.GetAsync<Car>(1)).Name.IsEqualTo("Volvo");
                (await connection.UpdateAsync(new Car { Id = 1, Name = "Saab" })).IsEqualTo(true);
                (await connection.GetAsync<Car>(1)).Name.IsEqualTo("Saab");
                (await connection.DeleteAsync(new Car { Id = 1 })).IsEqualTo(true);
                (await connection.GetAsync<Car>(1)).IsNull();
            }
        }

        public async Task TestSimpleGetAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var id = await connection.InsertAsync(new User { Name = "Adama", Age = 10 });
                var user = await connection.GetAsync<User>(id);
                user.Id.IsEqualTo(id);
                user.Name.IsEqualTo("Adama");
                await connection.DeleteAsync(user);
            }
        }

        public async Task InsertGetUpdateAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                (await connection.GetAsync<User>(3)).IsNull();

                var id = await connection.InsertAsync(new User { Name = "Adam", Age = 10 });

                //get a user with "isdirty" tracking
                var user = await connection.GetAsync<IUser>(id);
                user.Name.IsEqualTo("Adam");
                (await connection.UpdateAsync(user)).IsEqualTo(false); //returns false if not updated, based on tracking
                user.Name = "Bob";
                (await connection.UpdateAsync(user)).IsEqualTo(true); //returns true if updated, based on tracking
                user = await connection.GetAsync<IUser>(id);
                user.Name.IsEqualTo("Bob");

                //get a user with no tracking
                var notrackedUser = await connection.GetAsync<User>(id);
                notrackedUser.Name.IsEqualTo("Bob");
                (await connection.UpdateAsync(notrackedUser)).IsEqualTo(true);
                //returns true, even though user was not changed
                notrackedUser.Name = "Cecil";
                (await connection.UpdateAsync(notrackedUser)).IsEqualTo(true);
                (await connection.GetAsync<User>(id)).Name.IsEqualTo("Cecil");

                (await connection.QueryAsync<User>("select * from Users")).Count().IsEqualTo(1);
                (await connection.DeleteAsync(user)).IsEqualTo(true);
                (await connection.QueryAsync<User>("select * from Users")).Count().IsEqualTo(0);

                (await connection.UpdateAsync(notrackedUser)).IsEqualTo(false); //returns false, user not found

                (await connection.InsertAsync(new User { Name = "Adam", Age = 10 }, sqlAdapter: new SqlCeServerAdapter())).IsMoreThan(0);
            }
        }


        public async Task InsertCheckKeyAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                (await connection.GetAsync<IUser>(3)).IsNull();
                var user = new User { Name = "Adamb", Age = 10 };
                var id = await connection.InsertAsync(user);
                user.Id.IsEqualTo(id);
            }
        }

        public async Task BuilderSelectClauseAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var rand = new Random(8675309);
                var data = new List<User>();
                for (var i = 0; i < 100; i++)
                {
                    var nU = new User { Age = rand.Next(70), Id = i, Name = Guid.NewGuid().ToString() };
                    data.Add(nU);
                    nU.Id = await connection.InsertAsync(nU);
                }

                var builder = new SqlBuilder();
                var justId = builder.AddTemplate("SELECT /**select**/ FROM Users");
                var all = builder.AddTemplate("SELECT Name, /**select**/, Age FROM Users");

                builder.Select("Id");

                var ids = await connection.QueryAsync<int>(justId.RawSql, justId.Parameters);
                var users = await connection.QueryAsync<User>(all.RawSql, all.Parameters);

                foreach (var u in data)
                {
                    if (!ids.Any(i => u.Id == i)) throw new Exception("Missing ids in select");
                    if (!users.Any(a => a.Id == u.Id && a.Name == u.Name && a.Age == u.Age))
                        throw new Exception("Missing users in select");
                }
            }
        }

        public async Task BuilderTemplateWOCompositionAsync()
        {
            var builder = new SqlBuilder();
            var template = builder.AddTemplate("SELECT COUNT(*) FROM Users WHERE Age = @age", new { age = 5 });

            if (template.RawSql == null) throw new Exception("RawSql null");
            if (template.Parameters == null) throw new Exception("Parameters null");

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                await connection.InsertAsync(new User { Age = 5, Name = "Testy McTestington" });

                if ((await connection.QueryAsync<int>(template.RawSql, template.Parameters)).Single() != 1)
                    throw new Exception("Query failed");
            }
        }

        public async Task InsertListAsync()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
            }
        }

        public async Task UpdateList()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
                foreach (var user in users)
                {
                    user.Name = user.Name + " updated";
                }
                await connection.UpdateAsync(users);
                var name = connection.Query<User>("select * from users").First().Name;
                name.Contains("updated").IsTrue();
            }

        }

        public async Task DeleteList()
        {
            const int numberOfEntities = 100;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(users);
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities);

                var usersToDelete = users.Take(10).ToList();
                await connection.DeleteAsync(usersToDelete);
                users = connection.Query<User>("select * from users").ToList();
                users.Count.IsEqualTo(numberOfEntities - 10);
            }

        }

        public async Task GetAllAsync()
        {
            const int numberOfEntities = 1000;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(users);
                total.IsEqualTo(numberOfEntities);
                users = (List<User>)await connection.GetAllAsync<User>();
                users.Count.IsEqualTo(numberOfEntities);
                var iusers = await connection.GetAllAsync<IUser>();
                iusers.ToList().Count.IsEqualTo(numberOfEntities);
            }
        }

        public async Task InsertFieldWithReservedNameAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();
                var id = await connection.InsertAsync(new Result { Name = "Adam", Order = 1 });

                var result = await connection.GetAsync<Result>(id);
                result.Order.IsEqualTo(1);
            }
        }

        public async Task DeleteAllAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var id1 = await connection.InsertAsync(new User { Name = "Alice", Age = 32 });
                var id2 = await connection.InsertAsync(new User { Name = "Bob", Age = 33 });
                await connection.DeleteAllAsync<User>();
                (await connection.GetAsync<User>(id1)).IsNull();
                (await connection.GetAsync<User>(id2)).IsNull();
            }
        }
    }
}

