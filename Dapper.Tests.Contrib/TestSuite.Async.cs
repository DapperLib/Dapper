using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper.Contrib.Extensions;
using FactAttribute = Dapper.Tests.Contrib.SkippableFactAttribute;
using Xunit;

namespace Dapper.Tests.Contrib
{
    public abstract partial class TestSuite
    {
        [Fact]
        public async Task TypeWithGenericParameterCanBeInsertedAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<GenericType<string>>();
                var objectToInsert = new GenericType<string>
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "something"
                };
                await connection.InsertAsync(objectToInsert);

                Assert.Single(connection.GetAll<GenericType<string>>());

                var objectsToInsert = new List<GenericType<string>>
                {
                    new GenericType<string>
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "1",
                    },
                    new GenericType<string>
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "2",
                    }
                };

                await connection.InsertAsync(objectsToInsert);
                var list = connection.GetAll<GenericType<string>>();
                Assert.Equal(3, list.Count());
            }
        }

        [Fact]
        public async Task TypeWithGenericParameterCanBeUpdatedAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var objectToInsert = new GenericType<string>
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "something"
                };
                await connection.InsertAsync(objectToInsert);

                objectToInsert.Name = "somethingelse";
                await connection.UpdateAsync(objectToInsert);

                var updatedObject = connection.Get<GenericType<string>>(objectToInsert.Id);
                Assert.Equal(objectToInsert.Name, updatedObject.Name);
            }
        }

        [Fact]
        public async Task TypeWithGenericParameterCanBeDeletedAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var objectToInsert = new GenericType<string>
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "something"
                };
                await connection.InsertAsync(objectToInsert);

                bool deleted = await connection.DeleteAsync(objectToInsert);
                Assert.True(deleted);
            }
        }

        [Fact] 
        public async Task GetAsyncSucceedsAfterDeleteAsyncWhenExplicitKeyPresent()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAsync(new ObjectX { ObjectXId = Guid.NewGuid().ToString() }).ConfigureAwait(false);
                var retrieved = await connection.GetAsync<ObjectX>(Guid.NewGuid().ToString()).ConfigureAwait(false);
                Assert.Null(retrieved);
            }
        }

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
                var originalxCount = (await connection.QueryAsync<int>("Select Count(*) From ObjectX").ConfigureAwait(false)).First();
                await connection.InsertAsync(o1).ConfigureAwait(false);
                var list1 = (await connection.QueryAsync<ObjectX>("select * from ObjectX").ConfigureAwait(false)).ToList();
                Assert.Equal(list1.Count, originalxCount + 1);
                o1 = await connection.GetAsync<ObjectX>(guid).ConfigureAwait(false);
                Assert.Equal(o1.ObjectXId, guid);
                o1.Name = "Bar";
                await connection.UpdateAsync(o1).ConfigureAwait(false);
                o1 = await connection.GetAsync<ObjectX>(guid).ConfigureAwait(false);
                Assert.Equal("Bar", o1.Name);
                await connection.DeleteAsync(o1).ConfigureAwait(false);
                o1 = await connection.GetAsync<ObjectX>(guid).ConfigureAwait(false);
                Assert.Null(o1);

                const int id = 42;
                var o2 = new ObjectY { ObjectYId = id, Name = "Foo" };
                var originalyCount = connection.Query<int>("Select Count(*) From ObjectY").First();
                await connection.InsertAsync(o2).ConfigureAwait(false);
                var list2 = (await connection.QueryAsync<ObjectY>("select * from ObjectY").ConfigureAwait(false)).ToList();
                Assert.Equal(list2.Count, originalyCount + 1);
                o2 = await connection.GetAsync<ObjectY>(id).ConfigureAwait(false);
                Assert.Equal(o2.ObjectYId, id);
                o2.Name = "Bar";
                await connection.UpdateAsync(o2).ConfigureAwait(false);
                o2 = await connection.GetAsync<ObjectY>(id).ConfigureAwait(false);
                Assert.Equal("Bar", o2.Name);
                await connection.DeleteAsync(o2).ConfigureAwait(false);
                o2 = await connection.GetAsync<ObjectY>(id).ConfigureAwait(false);
                Assert.Null(o2);
            }
        }

        [Fact]
        public async Task TableNameAsync()
        {
            using (var connection = GetOpenConnection())
            {
                // tests against "Automobiles" table (Table attribute)
                var id = await connection.InsertAsync(new Car { Name = "VolvoAsync" }).ConfigureAwait(false);
                var car = await connection.GetAsync<Car>(id).ConfigureAwait(false);
                Assert.NotNull(car);
                Assert.Equal("VolvoAsync", car.Name);
                Assert.True(await connection.UpdateAsync(new Car { Id = id, Name = "SaabAsync" }).ConfigureAwait(false));
                Assert.Equal("SaabAsync", (await connection.GetAsync<Car>(id).ConfigureAwait(false)).Name);
                Assert.True(await connection.DeleteAsync(new Car { Id = id }).ConfigureAwait(false));
                Assert.Null(await connection.GetAsync<Car>(id).ConfigureAwait(false));
            }
        }

        [Fact]
        public async Task TestSimpleGetAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var id = await connection.InsertAsync(new User { Name = "Adama", Age = 10 }).ConfigureAwait(false);
                var user = await connection.GetAsync<User>(id).ConfigureAwait(false);
                Assert.Equal(id, user.Id);
                Assert.Equal("Adama", user.Name);
                await connection.DeleteAsync(user).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task InsertGetUpdateAsync()
        {
            using (var connection = GetOpenConnection())
            {
                Assert.Null(await connection.GetAsync<User>(30).ConfigureAwait(false));

                var originalCount = (await connection.QueryAsync<int>("select Count(*) from Users").ConfigureAwait(false)).First();

                var id = await connection.InsertAsync(new User { Name = "Adam", Age = 10 }).ConfigureAwait(false);

                //get a user with "isdirty" tracking
                var user = await connection.GetAsync<IUser>(id).ConfigureAwait(false);
                Assert.Equal("Adam", user.Name);
                Assert.False(await connection.UpdateAsync(user).ConfigureAwait(false)); //returns false if not updated, based on tracking
                user.Name = "Bob";
                Assert.True(await connection.UpdateAsync(user).ConfigureAwait(false)); //returns true if updated, based on tracking
                user = await connection.GetAsync<IUser>(id).ConfigureAwait(false);
                Assert.Equal("Bob", user.Name);

                //get a user with no tracking
                var notrackedUser = await connection.GetAsync<User>(id).ConfigureAwait(false);
                Assert.Equal("Bob", notrackedUser.Name);
                Assert.True(await connection.UpdateAsync(notrackedUser).ConfigureAwait(false));
                //returns true, even though user was not changed
                notrackedUser.Name = "Cecil";
                Assert.True(await connection.UpdateAsync(notrackedUser).ConfigureAwait(false));
                Assert.Equal("Cecil", (await connection.GetAsync<User>(id).ConfigureAwait(false)).Name);

                Assert.Equal((await connection.QueryAsync<User>("select * from Users").ConfigureAwait(false)).Count(), originalCount + 1);
                Assert.True(await connection.DeleteAsync(user).ConfigureAwait(false));
                Assert.Equal((await connection.QueryAsync<User>("select * from Users").ConfigureAwait(false)).Count(), originalCount);

                Assert.False(await connection.UpdateAsync(notrackedUser).ConfigureAwait(false)); //returns false, user not found

                Assert.True(await connection.InsertAsync(new User { Name = "Adam", Age = 10 }).ConfigureAwait(false) > originalCount + 1);
            }
        }

        [Fact]
        public async Task InsertCheckKeyAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                Assert.Null(await connection.GetAsync<IUser>(3).ConfigureAwait(false));
                var user = new User { Name = "Adamb", Age = 10 };
                var id = await connection.InsertAsync(user).ConfigureAwait(false);
                Assert.Equal(user.Id, id);
            }
        }

        [Fact]
        public async Task BuilderSelectClauseAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var rand = new Random(8675309);
                var data = new List<User>();
                for (var i = 0; i < 100; i++)
                {
                    var nU = new User { Age = rand.Next(70), Id = i, Name = Guid.NewGuid().ToString() };
                    data.Add(nU);
                    nU.Id = await connection.InsertAsync(nU).ConfigureAwait(false);
                }

                var builder = new SqlBuilder();
                var justId = builder.AddTemplate("SELECT /**select**/ FROM Users");
                var all = builder.AddTemplate("SELECT Name, /**select**/, Age FROM Users");

                builder.Select("Id");

                var ids = await connection.QueryAsync<int>(justId.RawSql, justId.Parameters).ConfigureAwait(false);
                var users = await connection.QueryAsync<User>(all.RawSql, all.Parameters).ConfigureAwait(false);

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
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                await connection.InsertAsync(new User { Age = 5, Name = "Testy McTestington" }).ConfigureAwait(false);

                if ((await connection.QueryAsync<int>(template.RawSql, template.Parameters).ConfigureAwait(false)).Single() != 1)
                    throw new Exception("Query failed");
            }
        }

        [Fact]
        public async Task InsertEnumerableAsync()
        {
            await InsertHelperAsync(src => src.AsEnumerable()).ConfigureAwait(false);
        }

        [Fact]
        public async Task InsertArrayAsync()
        {
            await InsertHelperAsync(src => src.ToArray()).ConfigureAwait(false);
        }

        [Fact]
        public async Task InsertListAsync()
        {
            await InsertHelperAsync(src => src.ToList()).ConfigureAwait(false);
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
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(helper(users)).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                Assert.Equal(users.Count, numberOfEntities);
            }
        }

        [Fact]
        public async Task UpdateEnumerableAsync()
        {
            await UpdateHelperAsync(src => src.AsEnumerable()).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateArrayAsync()
        {
            await UpdateHelperAsync(src => src.ToArray()).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateListAsync()
        {
            await UpdateHelperAsync(src => src.ToList()).ConfigureAwait(false);
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
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(helper(users)).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                Assert.Equal(users.Count, numberOfEntities);
                foreach (var user in users)
                {
                    user.Name += " updated";
                }
                await connection.UpdateAsync(helper(users)).ConfigureAwait(false);
                var name = connection.Query<User>("select * from Users").First().Name;
                Assert.Contains("updated", name);
            }
        }

        [Fact]
        public async Task DeleteEnumerableAsync()
        {
            await DeleteHelperAsync(src => src.AsEnumerable()).ConfigureAwait(false);
        }

        [Fact]
        public async Task DeleteArrayAsync()
        {
            await DeleteHelperAsync(src => src.ToArray()).ConfigureAwait(false);
        }

        [Fact]
        public async Task DeleteListAsync()
        {
            await DeleteHelperAsync(src => src.ToList()).ConfigureAwait(false);
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
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(helper(users)).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);
                users = connection.Query<User>("select * from Users").ToList();
                Assert.Equal(users.Count, numberOfEntities);

                var usersToDelete = users.Take(10).ToList();
                await connection.DeleteAsync(helper(usersToDelete)).ConfigureAwait(false);
                users = connection.Query<User>("select * from Users").ToList();
                Assert.Equal(users.Count, numberOfEntities - 10);
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
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(users).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);
                users = (List<User>)await connection.GetAllAsync<User>().ConfigureAwait(false);
                Assert.Equal(users.Count, numberOfEntities);
                var iusers = await connection.GetAllAsync<IUser>().ConfigureAwait(false);
                Assert.Equal(iusers.ToList().Count, numberOfEntities);
            }
        }

        /// <summary>
        /// Test for issue #933
        /// </summary>
        [Fact]
        public async void GetAsyncAndGetAllAsyncWithNullableValues()
        {
            using (var connection = GetOpenConnection())
            {
                var id1 = connection.Insert(new NullableDate { DateValue = new DateTime(2011, 07, 14) });
                var id2 = connection.Insert(new NullableDate { DateValue = null });

                var value1 = await connection.GetAsync<INullableDate>(id1).ConfigureAwait(false);
                Assert.Equal(new DateTime(2011, 07, 14), value1.DateValue.Value);

                var value2 = await connection.GetAsync<INullableDate>(id2).ConfigureAwait(false);
                Assert.True(value2.DateValue == null);

                var value3 = await connection.GetAllAsync<INullableDate>().ConfigureAwait(false);
                var valuesList = value3.ToList();
                Assert.Equal(new DateTime(2011, 07, 14), valuesList[0].DateValue.Value);
                Assert.True(valuesList[1].DateValue == null);
            }
        }

        [Fact]
        public async Task InsertFieldWithReservedNameAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);
                var id = await connection.InsertAsync(new Result { Name = "Adam", Order = 1 }).ConfigureAwait(false);

                var result = await connection.GetAsync<Result>(id).ConfigureAwait(false);
                Assert.Equal(1, result.Order);
            }
        }

        [Fact]
        public async Task DeleteAllAsync()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var id1 = await connection.InsertAsync(new User { Name = "Alice", Age = 32 }).ConfigureAwait(false);
                var id2 = await connection.InsertAsync(new User { Name = "Bob", Age = 33 }).ConfigureAwait(false);
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);
                Assert.Null(await connection.GetAsync<User>(id1).ConfigureAwait(false));
                Assert.Null(await connection.GetAsync<User>(id2).ConfigureAwait(false));
            }
        }
    }
}
