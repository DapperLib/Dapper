using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public partial class TestSuite
    {
        [Fact]
        public void ParentChildIdentityAssociations()
        {
            var lookup = new Dictionary<int, Parent>();
            var parents = connection.Query<Parent, Child, Parent>(@"select 1 as [Id], 1 as [Id] union all select 1,2 union all select 2,3 union all select 1,4 union all select 3,5",
                (parent, child) =>
                {
                    Parent found;
                    if (!lookup.TryGetValue(parent.Id, out found))
                    {
                        lookup.Add(parent.Id, found = parent);
                    }
                    found.Children.Add(child);
                    return found;
                }).Distinct().ToDictionary(p => p.Id);
            parents.Count.IsEqualTo(3);
            parents[1].Children.Select(c => c.Id).SequenceEqual(new[] { 1, 2, 4 }).IsTrue();
            parents[2].Children.Select(c => c.Id).SequenceEqual(new[] { 3 }).IsTrue();
            parents[3].Children.Select(c => c.Id).SequenceEqual(new[] { 5 }).IsTrue();
        }

        class Parent
        {
            public int Id { get; set; }
            public readonly List<Child> Children = new List<Child>();
        }
        class Child
        {
            public int Id { get; set; }
        }

        [Fact]
        public void TestMultiMap()
        {
            var createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')
";
            connection.Execute(createSql);
            try
            {
                var sql =
    @"select * from #Posts p 
left join #Users u on u.Id = p.OwnerId 
Order by p.Id";

                var data = connection.Query<Post, User, Post>(sql, (post, user) => { post.Owner = user; return post; }).ToList();
                var p = data.First();

                p.Content.IsEqualTo("Sams Post1");
                p.Id.IsEqualTo(1);
                p.Owner.Name.IsEqualTo("Sam");
                p.Owner.Id.IsEqualTo(99);

                data[2].Owner.IsNull();
            }
            finally
            {
                connection.Execute("drop table #Users drop table #Posts");
            }
        }

        [Fact]
        public void TestMultiMapThreeTypesWithGridReader()
        {
            var createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))
                create table #Comments (Id int, PostId int, CommentData varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')

                insert #Comments values(1, 1, 'Comment 1')";
            connection.Execute(createSql);
            try
            {
                const string sql = @"SELECT p.* FROM #Posts p

select p.*, u.Id, u.Name + '0' Name, c.Id, c.CommentData from #Posts p 
left join #Users u on u.Id = p.OwnerId 
left join #Comments c on c.PostId = p.Id
where p.Id = 1
Order by p.Id";

                var grid = connection.QueryMultiple(sql);

                var post1 = grid.Read<Post>().ToList();

                var post2 = grid.Read<Post, User, Comment, Post>((post, user, comment) => { post.Owner = user; post.Comment = comment; return post; }).SingleOrDefault();

                post2.Comment.Id.IsEqualTo(1);
                post2.Owner.Id.IsEqualTo(99);

            }
            finally
            {
                connection.Execute("drop table #Users drop table #Posts drop table #Comments");
            }
        }

        class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        class Post
        {
            public int Id { get; set; }
            public User Owner { get; set; }
            public string Content { get; set; }
            public Comment Comment { get; set; }
        }
        class Comment
        {
            public int Id { get; set; }
            public string CommentData { get; set; }
        }

        [Fact]
        public void TestMultiMapperIsNotConfusedWithUnorderedCols()
        {
            var result = connection.Query<Foo1, Bar1, Tuple<Foo1, Bar1>>("select 1 as Id, 2 as BarId, 3 as BarId, 'a' as Name", Tuple.Create, splitOn: "BarId").First();

            result.Item1.Id.IsEqualTo(1);
            result.Item1.BarId.IsEqualTo(2);
            result.Item2.BarId.IsEqualTo(3);
            result.Item2.Name.IsEqualTo("a");
        }

        class Foo1
        {
#pragma warning disable 0649
            public int Id;
#pragma warning restore 0649
            public int BarId { get; set; }
        }
        class Bar1
        {
#pragma warning disable 0649
            public int BarId;
#pragma warning restore 0649
            public string Name { get; set; }
        }

        [Fact]
        public void TestMultiMapDynamic()
        {
            var createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')
";
            connection.Execute(createSql);

            var sql =
@"select * from #Posts p 
left join #Users u on u.Id = p.OwnerId 
Order by p.Id";

            var data = connection.Query<dynamic, dynamic, dynamic>(sql, (post, user) => { post.Owner = user; return post; }).ToList();
            var p = data.First();

            // hairy extension method support for dynamics
            ((string)p.Content).IsEqualTo("Sams Post1");
            ((int)p.Id).IsEqualTo(1);
            ((string)p.Owner.Name).IsEqualTo("Sam");
            ((int)p.Owner.Id).IsEqualTo(99);

            ((object)data[2].Owner).IsNull();

            connection.Execute("drop table #Users drop table #Posts");
        }

        [Fact]
        public void TestMultiMapWithSplit() // http://stackoverflow.com/q/6056778/23354
        {
            var sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            var product = connection.Query<Product, Category, Product>(sql, (prod, cat) =>
            {
                prod.Category = cat;
                return prod;
            }).First();
            // assertions
            product.Id.IsEqualTo(1);
            product.Name.IsEqualTo("abc");
            product.Category.Id.IsEqualTo(2);
            product.Category.Name.IsEqualTo("def");
        }

        [Fact]
        public void TestMultiMapWithSplitWithNullValue() // http://stackoverflow.com/q/10744728/449906
        {
            var sql = @"select 1 as id, 'abc' as name, NULL as description, 'def' as name";
            var product = connection.Query<Product, Category, Product>(sql, (prod, cat) =>
            {
                prod.Category = cat;
                return prod;
            }, splitOn: "description").First();
            // assertions
            product.Id.IsEqualTo(1);
            product.Name.IsEqualTo("abc");
            product.Category.IsNull();
        }

        [Fact]
        public void TestMultiMapWithSplitWithNullValueAndSpoofColumn() // http://stackoverflow.com/q/10744728/449906
        {
            var sql = @"select 1 as id, 'abc' as name, 1 as spoof, NULL as description, 'def' as name";
            var product = connection.Query<Product, Category, Product>(sql, (prod, cat) =>
            {
                prod.Category = cat;
                return prod;
            }, splitOn: "spoof").First();
            // assertions
            product.Id.IsEqualTo(1);
            product.Name.IsEqualTo("abc");
            product.Category.IsNotNull();
            product.Category.Id.IsEqualTo(0);
            product.Category.Name.IsEqualTo("def");
            product.Category.Description.IsNull();
        }

        class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Category Category { get; set; }
        }
        class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        [Fact]
        public void TestMultiMappingVariations()
        {
            var sql = @"select 1 as Id, 'a' as Content, 2 as Id, 'b' as Content, 3 as Id, 'c' as Content, 4 as Id, 'd' as Content, 5 as Id, 'e' as Content";

            var order = connection.Query<dynamic, dynamic, dynamic, dynamic>(sql, (o, owner, creator) => { o.Owner = owner; o.Creator = creator; return o; }).First();

            Assert.IsEqualTo(order.Id, 1);
            Assert.IsEqualTo(order.Content, "a");
            Assert.IsEqualTo(order.Owner.Id, 2);
            Assert.IsEqualTo(order.Owner.Content, "b");
            Assert.IsEqualTo(order.Creator.Id, 3);
            Assert.IsEqualTo(order.Creator.Content, "c");

            order = connection.Query<dynamic, dynamic, dynamic, dynamic, dynamic>(sql, (o, owner, creator, address) =>
            {
                o.Owner = owner;
                o.Creator = creator;
                o.Owner.Address = address;
                return o;
            }).First();

            Assert.IsEqualTo(order.Id, 1);
            Assert.IsEqualTo(order.Content, "a");
            Assert.IsEqualTo(order.Owner.Id, 2);
            Assert.IsEqualTo(order.Owner.Content, "b");
            Assert.IsEqualTo(order.Creator.Id, 3);
            Assert.IsEqualTo(order.Creator.Content, "c");
            Assert.IsEqualTo(order.Owner.Address.Id, 4);
            Assert.IsEqualTo(order.Owner.Address.Content, "d");

            order = connection.Query<dynamic, dynamic, dynamic, dynamic, dynamic, dynamic>(sql, (a, b, c, d, e) => { a.B = b; a.C = c; a.C.D = d; a.E = e; return a; }).First();

            Assert.IsEqualTo(order.Id, 1);
            Assert.IsEqualTo(order.Content, "a");
            Assert.IsEqualTo(order.B.Id, 2);
            Assert.IsEqualTo(order.B.Content, "b");
            Assert.IsEqualTo(order.C.Id, 3);
            Assert.IsEqualTo(order.C.Content, "c");
            Assert.IsEqualTo(order.C.D.Id, 4);
            Assert.IsEqualTo(order.C.D.Content, "d");
            Assert.IsEqualTo(order.E.Id, 5);
            Assert.IsEqualTo(order.E.Content, "e");
        }

        class UserWithConstructor
        {
            public UserWithConstructor(int id, string name)
            {
                Ident = id;
                FullName = name;
            }
            public int Ident { get; set; }
            public string FullName { get; set; }
        }

        class PostWithConstructor
        {
            public PostWithConstructor(int id, int ownerid, string content)
            {
                Ident = id;
                FullContent = content;
            }

            public int Ident { get; set; }
            public UserWithConstructor Owner { get; set; }
            public string FullContent { get; set; }
            public Comment Comment { get; set; }
        }

        [Fact]
        public void TestMultiMapWithConstructor()
        {
            var createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')";
            connection.Execute(createSql);
            try
            {
                string sql = @"select * from #Posts p 
                           left join #Users u on u.Id = p.OwnerId 
                           Order by p.Id";
                PostWithConstructor[] data = connection.Query<PostWithConstructor, UserWithConstructor, PostWithConstructor>(sql, (post, user) => { post.Owner = user; return post; }).ToArray();
                var p = data.First();

                p.FullContent.IsEqualTo("Sams Post1");
                p.Ident.IsEqualTo(1);
                p.Owner.FullName.IsEqualTo("Sam");
                p.Owner.Ident.IsEqualTo(99);

                data[2].Owner.IsNull();
            }
            finally
            {
                connection.Execute("drop table #Users drop table #Posts");
            }
        }

        class ReviewBoard
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public User User1 { get; set; }
            public User User2 { get; set; }
            public User User3 { get; set; }
            public User User4 { get; set; }
            public User User5 { get; set; }
            public User User6 { get; set; }
            public User User7 { get; set; }
            public User User8 { get; set; }
            public User User9 { get; set; }
        }

        [Fact]
        public void TestMultiMapArbitraryMaps()
        {
            // please excuse the trite example, but it is easier to follow than a more real-world one
            var createSql = @"
                create table #ReviewBoards (Id int, Name varchar(20), User1Id int, User2Id int, User3Id int, User4Id int, User5Id int, User6Id int, User7Id int, User8Id int, User9Id int)
                create table #Users (Id int, Name varchar(20))

                insert #Users values(1, 'User 1')
                insert #Users values(2, 'User 2')
                insert #Users values(3, 'User 3')
                insert #Users values(4, 'User 4')
                insert #Users values(5, 'User 5')
                insert #Users values(6, 'User 6')
                insert #Users values(7, 'User 7')
                insert #Users values(8, 'User 8')
                insert #Users values(9, 'User 9')

                insert #ReviewBoards values(1, 'Review Board 1', 1, 2, 3, 4, 5, 6, 7, 8, 9)
";
            connection.Execute(createSql);
            try
            {
                var sql = @"
                    select 
                        rb.Id, rb.Name,
                        u1.*, u2.*, u3.*, u4.*, u5.*, u6.*, u7.*, u8.*, u9.*
                    from #ReviewBoards rb
                        inner join #Users u1 on u1.Id = rb.User1Id
                        inner join #Users u2 on u2.Id = rb.User2Id
                        inner join #Users u3 on u3.Id = rb.User3Id
                        inner join #Users u4 on u4.Id = rb.User4Id
                        inner join #Users u5 on u5.Id = rb.User5Id
                        inner join #Users u6 on u6.Id = rb.User6Id
                        inner join #Users u7 on u7.Id = rb.User7Id
                        inner join #Users u8 on u8.Id = rb.User8Id
                        inner join #Users u9 on u9.Id = rb.User9Id
";

                var types = new[] { typeof(ReviewBoard), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User), typeof(User) };

                Func<object[], ReviewBoard> mapper = (objects) =>
                {
                    var board = (ReviewBoard)objects[0];
                    board.User1 = (User)objects[1];
                    board.User2 = (User)objects[2];
                    board.User3 = (User)objects[3];
                    board.User4 = (User)objects[4];
                    board.User5 = (User)objects[5];
                    board.User6 = (User)objects[6];
                    board.User7 = (User)objects[7];
                    board.User8 = (User)objects[8];
                    board.User9 = (User)objects[9];
                    return board;
                };

                var data = connection.Query<ReviewBoard>(sql, types, mapper).ToList();

                var p = data.First();
                p.Id.IsEqualTo(1);
                p.Name.IsEqualTo("Review Board 1");
                p.User1.Id.IsEqualTo(1);
                p.User2.Id.IsEqualTo(2);
                p.User3.Id.IsEqualTo(3);
                p.User4.Id.IsEqualTo(4);
                p.User5.Id.IsEqualTo(5);
                p.User6.Id.IsEqualTo(6);
                p.User7.Id.IsEqualTo(7);
                p.User8.Id.IsEqualTo(8);
                p.User9.Id.IsEqualTo(9);
                p.User1.Name.IsEqualTo("User 1");
                p.User2.Name.IsEqualTo("User 2");
                p.User3.Name.IsEqualTo("User 3");
                p.User4.Name.IsEqualTo("User 4");
                p.User5.Name.IsEqualTo("User 5");
                p.User6.Name.IsEqualTo("User 6");
                p.User7.Name.IsEqualTo("User 7");
                p.User8.Name.IsEqualTo("User 8");
                p.User9.Name.IsEqualTo("User 9");
            }
            finally
            {
                connection.Execute("drop table #Users drop table #ReviewBoards");
            }
        }

        [Fact]
        public void TestMultiMapGridReader()
        {
            var createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')
";
            connection.Execute(createSql);

            var sql =
@"select p.*, u.Id, u.Name + '0' Name from #Posts p 
left join #Users u on u.Id = p.OwnerId 
Order by p.Id

select p.*, u.Id, u.Name + '1' Name from #Posts p 
left join #Users u on u.Id = p.OwnerId 
Order by p.Id
";

            var grid = connection.QueryMultiple(sql);

            for (int i = 0; i < 2; i++)
            {
                var data = grid.Read<Post, User, Post>((post, user) => { post.Owner = user; return post; }).ToList();
                var p = data.First();

                p.Content.IsEqualTo("Sams Post1");
                p.Id.IsEqualTo(1);
                p.Owner.Name.IsEqualTo("Sam" + i);
                p.Owner.Id.IsEqualTo(99);

                data[2].Owner.IsNull();
            }

            connection.Execute("drop table #Users drop table #Posts");
        }

        [Fact]
        public void TestFlexibleMultiMapping()
        {
            var sql =
@"select 
    1 as PersonId, 'bob' as Name, 
    2 as AddressId, 'abc street' as Name, 1 as PersonId,
    3 as Id, 'fred' as Name
    ";
            var personWithAddress = connection.Query<Person, Address, Extra, Tuple<Person, Address, Extra>>
                (sql, Tuple.Create, splitOn: "AddressId,Id").First();

            personWithAddress.Item1.PersonId.IsEqualTo(1);
            personWithAddress.Item1.Name.IsEqualTo("bob");
            personWithAddress.Item2.AddressId.IsEqualTo(2);
            personWithAddress.Item2.Name.IsEqualTo("abc street");
            personWithAddress.Item2.PersonId.IsEqualTo(1);
            personWithAddress.Item3.Id.IsEqualTo(3);
            personWithAddress.Item3.Name.IsEqualTo("fred");
        }

        [Fact]
        public void TestMultiMappingWithSplitOnSpaceBetweenCommas()
        {
            var sql = @"select 
                        1 as PersonId, 'bob' as Name, 
                        2 as AddressId, 'abc street' as Name, 1 as PersonId,
                        3 as Id, 'fred' as Name
                        ";
            var personWithAddress = connection.Query<Person, Address, Extra, Tuple<Person, Address, Extra>>
                (sql, Tuple.Create, splitOn: "AddressId, Id").First();

            personWithAddress.Item1.PersonId.IsEqualTo(1);
            personWithAddress.Item1.Name.IsEqualTo("bob");
            personWithAddress.Item2.AddressId.IsEqualTo(2);
            personWithAddress.Item2.Name.IsEqualTo("abc street");
            personWithAddress.Item2.PersonId.IsEqualTo(1);
            personWithAddress.Item3.Id.IsEqualTo(3);
            personWithAddress.Item3.Name.IsEqualTo("fred");
        }

        class Person
        {
            public int PersonId { get; set; }
            public string Name { get; set; }
            public string Occupation { get; private set; }
            public int NumberOfLegs = 2;
            public Address Address { get; set; }
        }
        class Address
        {
            public int AddressId { get; set; }
            public string Name { get; set; }
            public int PersonId { get; set; }
        }
        class Extra
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void TestMultiMappingWithNonReturnedProperty()
        {
            var sql = @"select 
                            1 as PostId, 'Title' as Title,
                            2 as BlogId, 'Blog' as Title";
            var postWithBlog = connection.Query<Post_DupeProp, Blog_DupeProp, Post_DupeProp>(sql,
                (p, b) =>
                {
                    p.Blog = b;
                    return p;
                }, splitOn: "BlogId").First();

            postWithBlog.PostId.IsEqualTo(1);
            postWithBlog.Title.IsEqualTo("Title");
            postWithBlog.Blog.BlogId.IsEqualTo(2);
            postWithBlog.Blog.Title.IsEqualTo("Blog");
        }

        class Post_DupeProp
        {
            public int PostId { get; set; }
            public string Title { get; set; }
            public int BlogId { get; set; }
            public Blog_DupeProp Blog { get; set; }
        }
        class Blog_DupeProp
        {
            public int BlogId { get; set; }
            public string Title { get; set; }
        }
    }
}
