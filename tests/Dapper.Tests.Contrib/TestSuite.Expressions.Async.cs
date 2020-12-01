using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Xunit;

namespace Dapper.Tests.Contrib
{
    public abstract partial class TestSuite
    {
        [Fact]
        public async Task GetAllAsyncWithOrExpression()
        {
            const int numberOfEntities = 10;

            var users = new List<User>(numberOfEntities);

            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User {Name = "User " + i, Age = i});

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(users).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);

                users = (List<User>) await connection.GetAllAsync<User>(x => x.Age == 5 || x.Age == 6).ConfigureAwait(false);
                Assert.Equal(2, users.Count);
                Assert.NotNull(users.FirstOrDefault(x => x.Age == 5));
                Assert.NotNull(users.FirstOrDefault(x => x.Age == 6));

                var iusers = await connection.GetAllAsync<IUser>(x => x.Age == 5 || x.Age == 6).ConfigureAwait(false);
                Assert.Equal(2, iusers.Count());
                Assert.NotNull(iusers.FirstOrDefault(x => x.Age == 5));
                Assert.NotNull(iusers.FirstOrDefault(x => x.Age == 6));
            }
        }

        [Fact]
        public async Task GetAllAsyncWithAndExpression()
        {
            const int numberOfEntities = 10;

            var users = new List<User>(numberOfEntities);

            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User {Name = "User " + i, Age = i});

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(users).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);

                users = (List<User>) await connection.GetAllAsync<User>(x => x.Age == 5 && x.Id == 6).ConfigureAwait(false);
                Assert.Single(users);
                Assert.NotNull(users.FirstOrDefault(x => x.Age == 5 && x.Id == 6));

                var iusers = await connection.GetAllAsync<IUser>(x => x.Age == 5 && x.Id == 6).ConfigureAwait(false);
                Assert.Single(iusers);
                Assert.NotNull(iusers.FirstOrDefault(x => x.Age == 5 && x.Id == 6));
            }
        }

        [Fact]
        public async Task GetAllAsyncWithStringExpression()
        {
            const int numberOfEntities = 10;

            var users = new List<User>(numberOfEntities);

            for (var i = 0; i < numberOfEntities; i++)
                users.Add(new User {Id = 100 + i, Name = "User " + i, Age = i});

            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var total = await connection.InsertAsync(users).ConfigureAwait(false);
                Assert.Equal(total, numberOfEntities);

                users = (List<User>) await connection.GetAllAsync<User>(x => x.Name == "User 5").ConfigureAwait(false);
                Assert.Single(users);
                Assert.NotNull(users.FirstOrDefault(x => x.Name == "User 5"));

                var iusers = await connection.GetAllAsync<IUser>(x => x.Name == "User 5").ConfigureAwait(false);
                Assert.Single(iusers);
                Assert.NotNull(iusers.FirstOrDefault(x => x.Name == "User 5"));
            }
        }

        [Fact]
        public async Task DeleteAllAsyncWithExpression()
        {
            using (var connection = GetOpenConnection())
            {
                await connection.DeleteAllAsync<User>().ConfigureAwait(false);

                var id1 = await connection.InsertAsync(new User {Name = "Alice", Age = 32}).ConfigureAwait(false);
                var id2 = await connection.InsertAsync(new User {Name = "Bob", Age = 33}).ConfigureAwait(false);

                await connection.DeleteAllAsync<User>(x => x.Name == "Alice").ConfigureAwait(false);

                Assert.Null(await connection.GetAsync<User>(id1).ConfigureAwait(false));
                Assert.NotNull(await connection.GetAsync<User>(id2).ConfigureAwait(false));
            }
        }
    }
}
