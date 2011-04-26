using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace SqlMapper
{
    static class Assert
    {

        public static void IsEqualTo<T>(this T obj, T other)
        {
            if (!obj.Equals(other))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}", obj, other));
            }
        }

        public static void IsSequenceEqualTo<T>(this IEnumerable<T> obj, IEnumerable<T> other)
        {
            if (!obj.SequenceEqual(other))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}", obj, other));
            }
        }

        public static void IsFalse(this bool b)
        {
            if (b)
            {
                throw new ApplicationException("Expected false");
            }
        }

        public static void IsTrue(this bool b)
        {
            if (b)
            {
                throw new ApplicationException("Expected true");
            }
        }

        public static void IsNull(this object obj)
        {
            if (obj != null)
            {
                throw new ApplicationException("Expected null");
            }
        }

    }

    class Tests
    {
       
        SqlConnection connection = Program.GetOpenConnection();

        public void SelectListInt()
        {
            connection.Query<int>("select 1 union all select 2 union all select 3")
              .IsSequenceEqualTo(new[] { 1, 2, 3 });
        }

        public void PassInIntArray()
        {
            connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[] { 1, 2, 3 }.AsEnumerable() })
             .IsSequenceEqualTo(new[] { 1, 2, 3 });
        }


        public void TestDoubleParam()
        {
            connection.Query<double>("select @d", new { d = 0.1d }).First()
                .IsEqualTo(0.1d);
        }

        public void TestBoolParam()
        {
            connection.Query<bool>("select @b", new { b = false }).First()
                .IsFalse();
        }

        public void TestStrings()
        {
            connection.Query<string>(@"select 'a' a union select 'b'")
                .IsSequenceEqualTo(new[] { "a", "b" });
        }

        public class Dog
        {
            public int? Age { get; set; }
            public Guid Id { get; set; }
            public string Name { get; set; }
            public float? Weight { get; set; }

            public int IgnoredProperty { get { return 1; } }
        }

        public void TestExtraFields()
        {
            var guid = Guid.NewGuid();
            var dog = connection.Query<Dog>("select '' as Extra, 1 as Age, 0.1 as Name1 , Id = @id", new { Id = guid});

            dog.Count()
               .IsEqualTo(1);

            dog.First().Age
                .IsEqualTo(1);

            dog.First().Id
                .IsEqualTo(guid);
        }


        public void TestStrongType()
        {
            var guid = Guid.NewGuid();
            var dog = connection.Query<Dog>("select Age = @Age, Id = @Id", new { Age = (int?)null, Id = guid });
            
            dog.Count()
                .IsEqualTo(1);

            dog.First().Age
                .IsNull();

            dog.First().Id
                .IsEqualTo(guid);
        }

        public void TestSimpleNull()
        {
            connection.Query<DateTime?>("select null").First().IsNull();
        }

        public void TestExpando()
        {
            var rows = connection.Query("select 1 A, 2 B union all select 3, 4").ToList();
            
            ((int)rows[0].A)
                .IsEqualTo(1);

            ((int)rows[0].B)
                .IsEqualTo(2);

            ((int)rows[1].A)
                .IsEqualTo(3);

            ((int)rows[1].B)
                .IsEqualTo(4);
        }

        public void TestStringList()
        {
            connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new {strings = new[] {"a","b","c"}})
                .IsSequenceEqualTo(new[] {"a","b","c"});

             connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new { strings = new string[0] })
	                .IsSequenceEqualTo(new string[0]);
        }

        public void TestExecuteCommand()
        {
            connection.Execute(@"
    set nocount on 
    create table #t(i int) 
    set nocount off 
    insert #t 
    select @a a union all select @b 
    set nocount on 
    drop table #t", new {a=1, b=2 }).IsEqualTo(2);
        }

        public void TestMassiveStrings()
        { 
            var str = new string('X', 20000);
            connection.Query<string>("select @a", new { a = str }).First()
                .IsEqualTo(str);
        }

        class TestObj
        {
            public int _internal;
            internal int Internal { set { _internal = value; } }

            public int _priv;
            internal int Priv { set { _priv = value; } }
        }

        public void TestSetInternal()
        {
            connection.Query<TestObj>("select 10 as [Internal]").First()._internal.IsEqualTo(10);
        }

        public void TestSetPrivate()
        {
            connection.Query<TestObj>("select 10 as [Priv]").First()._priv.IsEqualTo(10);
        }

        public void TestEnumeration()
        {
            var en = connection.Query<int>("select 1 as one", buffered: false);
            bool gotException = false;
            try
            {
                connection.Query<int>("select 1 as one", buffered: false);
            }
            catch (Exception)
            {
                gotException = true;
            }

            var realItems = en.ToList();

            // should not exception, since enumertated
            en = connection.Query<int>("select 1 as one", buffered: false);

            gotException.IsTrue();
        }

        public void TestEnumerationDynamic()
        {
            var en = connection.Query("select 1 as one", buffered: false);
            bool gotException = false;
            try
            {
                connection.Query("select 1 as one", buffered: false);
            }
            catch (Exception)
            {
                gotException = true;
            }

            int i = en.First().one;
            i.IsEqualTo(1);

            // should not exception, since enumertated
            en = connection.Query("select 1 as one",buffered:false);

            gotException.IsTrue();
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
        }
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

            var sql = 
@"select * from #Posts p 
left join #Users u on u.Id = p.OwnerId 
Order by p.Id";

            var data = connection.Query<Post, User>(sql, (post, user) => { post.Owner = user; }).ToList();
            var p = data.First();
           
            p.Content.IsEqualTo("Sams Post1");
            p.Id.IsEqualTo(1);
            p.Owner.Name.IsEqualTo("Sam");
            p.Owner.Id.IsEqualTo(99);

            data[2].Owner.IsNull();

            connection.Execute("drop table #Users drop table #Posts");
        }


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

            var data = connection.Query<dynamic, dynamic>(sql, (post, user) => { post.Owner = user; }).ToList();
            var p = data.First();

            // hairy extension method support for dynamics
            ((string)p.Content).IsEqualTo("Sams Post1");
            ((int)p.Id).IsEqualTo(1);
            ((string)p.Owner.Name).IsEqualTo("Sam");
            ((int)p.Owner.Id).IsEqualTo(99);

            ((object)data[2].Owner).IsNull();

            connection.Execute("drop table #Users drop table #Posts");
        }

        public void TestMultiMappingVariations()
        {
            var sql = "select 1 as Id, 'a' as Content, 2 as Id, 'b' as Content, 3 as Id, 'c' as Content, 4 as Id, 'd' as Content, 5 as Id, 'e' as Content";
            var mapped = connection.Query<dynamic, dynamic, dynamic>(sql, (a, b, c) => { a.B = b; a.C = c; }).First();

            Assert.IsEqualTo(mapped.Id, 1);
            Assert.IsEqualTo(mapped.Content, "a");
            Assert.IsEqualTo(mapped.B.Id, 2);
            Assert.IsEqualTo(mapped.B.Content, "b");
            Assert.IsEqualTo(mapped.C.Id, 3);
            Assert.IsEqualTo(mapped.C.Content, "c");

            mapped = connection.Query<dynamic, dynamic, dynamic, dynamic>(sql, (a, b, c, d) => { a.B = b; a.C = c; a.C.D = d; }).First();

            Assert.IsEqualTo(mapped.Id, 1);
            Assert.IsEqualTo(mapped.Content, "a");
            Assert.IsEqualTo(mapped.B.Id, 2);
            Assert.IsEqualTo(mapped.B.Content, "b");
            Assert.IsEqualTo(mapped.C.Id, 3);
            Assert.IsEqualTo(mapped.C.Content, "c");
            Assert.IsEqualTo(mapped.C.D.Id, 4);
            Assert.IsEqualTo(mapped.C.D.Content, "d");

            mapped = connection.Query<dynamic, dynamic, dynamic, dynamic, dynamic>(sql, (a, b, c, d, e) => { a.B = b; a.C = c; a.C.D = d; a.E = e; }).First();

            Assert.IsEqualTo(mapped.Id, 1);
            Assert.IsEqualTo(mapped.Content, "a");
            Assert.IsEqualTo(mapped.B.Id, 2);
            Assert.IsEqualTo(mapped.B.Content, "b");
            Assert.IsEqualTo(mapped.C.Id, 3);
            Assert.IsEqualTo(mapped.C.Content, "c");
            Assert.IsEqualTo(mapped.C.D.Id, 4);
            Assert.IsEqualTo(mapped.C.D.Content, "d");
            Assert.IsEqualTo(mapped.E.Id, 5);
            Assert.IsEqualTo(mapped.E.Content, "e");

        }

        class InheritanceTest1
        {
            public string Base1 { get; set; }
            public string Base2 { get; private set; }
        }

        class InheritanceTest2 : InheritanceTest1
        {
            public string Derived1 { get; set; }
            public string Derived2 { get; private set; }
        }

        public void TestInheritance()
        {
            // Test that inheritance works.
            var list = connection.Query<InheritanceTest2>("select 'One' as Derived1, 'Two' as Derived2, 'Three' as Base1, 'Four' as Base2");
            list.First().Derived1.IsEqualTo("One");
            list.First().Derived2.IsEqualTo("Two");
            list.First().Base1.IsEqualTo("Three");
            list.First().Base2.IsEqualTo("Four");
        }

    }
}
