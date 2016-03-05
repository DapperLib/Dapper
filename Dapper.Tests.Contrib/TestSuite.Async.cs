#if ASYNC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper.Contrib.Extensions;
#if XUNIT2
using FactAttribute = Dapper.Tests.Contrib.SkippableFactAttribute;
#else
using Xunit;
#endif

namespace Dapper.Tests.Contrib
{
    public abstract partial class TestSuite
    {
        /// <summary>
        /// Tests for issue #351 
        /// </summary>
        [Fact]
        public async Task InsertGetUpdateDeleteWithExplicitKeyAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = Guid.NewGuid().ToString();
                var o1 = new ObjectX { ObjectXId = guid, Name = "Foo" };
                var originalxCount = (await connection.QueryAsync<int>("Select Count(*) From ObjectX")).First();
                await connection.InsertAsync(o1);
                var list1 = (await connection.QueryAsync<ObjectX>("select * from ObjectX")).ToList();
                list1.Count.IsEqualTo(originalxCount + 1);
                o1 = await connection.GetAsync<ObjectX>(guid);
                o1.ObjectXId.IsEqualTo(guid);
                o1.Name = "Bar";
                await connection.UpdateAsync(o1);
                o1 = await connection.GetAsync<ObjectX>(guid);
                o1.Name.IsEqualTo("Bar");
                await connection.DeleteAsync(o1);
                o1 = await connection.GetAsync<ObjectX>(guid);
                o1.IsNull();

                const int id = 42;
                var o2 = new ObjectY { ObjectYId = id, Name = "Foo" };
                var originalyCount = connection.Query<int>("Select Count(*) From ObjectY").First();
                await connection.InsertAsync(o2);
                var list2 = (await connection.QueryAsync<ObjectY>("select * from ObjectY")).ToList();
                list2.Count.IsEqualTo(originalyCount+1);
                o2 = await connection.GetAsync<ObjectY>(id);
                o2.ObjectYId.IsEqualTo(id);
                o2.Name = "Bar";
                await connection.UpdateAsync(o2);
                o2 = await connection.GetAsync<ObjectY>(id);
                o2.Name.IsEqualTo("Bar");
                await connection.DeleteAsync(o2);
                o2 = await connection.GetAsync<ObjectY>(id);
                o2.IsNull();
            }
        }

        [Fact]
        public async Task TableNameAsync()
        {
            using (var connection = GetOpenConnection())
            {
                // tests against "Automobiles" table (Table attribute)
                var id = await connection.InsertAsync(new Car { Name = "VolvoAsync" });
                var car = await connection.GetAsync<Car>(id);
                car.IsNotNull();
                car.Name.IsEqualTo("VolvoAsync");
                (await connection.UpdateAsync(new Car { Id = id, Name = "SaabAsync" })).IsEqualTo(true);
                (await connection.GetAsync<Car>(id)).Name.IsEqualTo("SaabAsync");
                (await connection.DeleteAsync(new Car { Id = id })).IsEqualTo(true);
                (await connection.GetAsync<Car>(id)).IsNull();
            }
        }

        [Fact]
        public async Task TestSimpleGetAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var id = await connection.InsertAsync(new User { Name = "Adama", Age = 10 });
                var user = await connection.GetAsync<User>(id);
                user.Id.IsEqualTo(id);
                user.Name.IsEqualTo("Adama");
                await connection.DeleteAsync(user);
            }
        }

        [Fact]
        public async Task InsertGetUpdateAsync()
        {
            using (var connection = GetOpenConnection())
            {
                (await connection.GetAsync<User>(30)).IsNull();

                var originalCount = (await connection.QueryAsync<int>("select Count(*) from Users")).First();

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

                (await connection.QueryAsync<User>("select * from Users")).Count().IsEqualTo(originalCount+1);
                (await connection.DeleteAsync(user)).IsEqualTo(true);
                (await connection.QueryAsync<User>("select * from Users")).Count().IsEqualTo(originalCount);

                (await connection.UpdateAsync(notrackedUser)).IsEqualTo(false); //returns false, user not found

                (await connection.InsertAsync(new User {Name = "Adam", Age = 10})).IsMoreThan(originalCount + 1);
            }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task BuilderTemplateWithoutCompositionAsync()
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

        [Fact]
        public async Task InsertArrayAsync()
        {
            await InsertHelperAsync(src => src.ToArray());
        }

        [Fact]
        public async Task InsertListAsync()
        {
            await InsertHelperAsync(src => src.ToList());
        }

        private async Task InsertHelperAsync<T>(Func<IEnumerable<User>, T> helper)
            where T : class
        {
            const int numberOfEntities = 10;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(helper(users));
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
            }
        }

        [Fact]
        public async Task UpdateArrayAsync()
        {
            await UpdateHelperAsync(src => src.ToArray());
        }

        [Fact]
        public async Task UpdateListAsync()
        {
            await UpdateHelperAsync(src => src.ToList());
        }

        private async Task UpdateHelperAsync<T>(Func<IEnumerable<User>, T> helper)
            where T : class
        {
            const int numberOfEntities = 10;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(helper(users));
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities);
                foreach (var user in users)
                {
                    user.Name = user.Name + " updated";
                }
                await connection.UpdateAsync(helper(users));
                var name = connection.Query<User>("select * from Users").First().Name;
                name.Contains("updated").IsTrue();
            }
        }

        [Fact]
        public async Task DeleteArrayAsync()
        {
            await DeleteHelperAsync(src => src.ToArray());
        }

        [Fact]
        public async Task DeleteListAsync()
        {
            await DeleteHelperAsync(src => src.ToList());
        }

        private async Task DeleteHelperAsync<T>(Func<IEnumerable<User>, T> helper)
            where T : class
        {
            const int numberOfEntities = 10;

            var users = new List<User>();
            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User { Name = "User " + i, Age = i });

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>();

                var total = await connection.InsertAsync(helper(users));
                total.IsEqualTo(numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities);

                var usersToDelete = users.Take(10).ToList();
                await connection.DeleteAsync(helper(usersToDelete));
                users = connection.Query<User>("select * from Users").ToList();
                users.Count.IsEqualTo(numberOfEntities - 10);
            }
        }

        [Fact]
        public async Task GetAllAsync()
        {
            const int numberOfEntities = 10;

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

        [Fact]
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

        [Fact]
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
#endif