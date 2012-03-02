using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using System.Data.SqlServerCe;
using System.IO;
using System.Data;
using System.Collections;
using System.Reflection;

namespace SqlMapper
{

    class Tests
    {
        SqlConnection connection = Program.GetOpenConnection();

        public class AbstractInheritance
        {
            public abstract class Order
            {
                internal int Internal { get; set; }
                protected int Protected { get; set; }
                public int Public { get; set; }

                public int ProtectedVal { get { return Protected; } }
            }

            public class ConcreteOrder : Order
            {
                public int Concrete { get; set; }
            }
        }

        class NoDefaultConstructor
        {
            public NoDefaultConstructor(int a, float f)
            {
                A = a;
                F = f;
            }
            public int A { get; set; }
            public float F { get; set; }
        }

        public void TestNoDefaultConstructor()
        {
            NoDefaultConstructor nodef = connection.Query<NoDefaultConstructor>("select 1 A, CAST(NULL AS real) f").First();
            nodef.A.IsEqualTo(1);
            nodef.F.IsEqualTo(0);
        }

        // http://stackoverflow.com/q/8593871
        public void TestAbstractInheritance() 
        {
            var order = connection.Query<AbstractInheritance.ConcreteOrder>("select 1 Internal,2 Protected,3 [Public],4 Concrete").First();

            order.Internal.IsEqualTo(1);
            order.ProtectedVal.IsEqualTo(2);
            order.Public.IsEqualTo(3);
            order.Concrete.IsEqualTo(4);
        }

        public void TestListOfAnsiStrings()
        {
            var results = connection.Query<string>("select * from (select 'a' str union select 'b' union select 'c') X where str in @strings",
                new { strings = new[] { new DbString { IsAnsi = true, Value = "a" }, new DbString { IsAnsi = true, Value = "b" } } }).ToList();

            results[0].IsEqualTo("a");
            results[1].IsEqualTo("b");
        }

        public void TestNullableGuidSupport()
        {
            var guid = connection.Query<Guid?>("select null").First();
            guid.IsNull();

            guid = Guid.NewGuid();
            var guid2 = connection.Query<Guid?>("select @guid", new { guid }).First();
            guid.IsEqualTo(guid2);
        }

        public void TestNonNullableGuidSupport()
        {
            var guid = Guid.NewGuid();
            var guid2 = connection.Query<Guid?>("select @guid", new { guid }).First();
            Assert.IsTrue(guid == guid2);
        }

        struct Car
        {
            public enum TrapEnum : int
            {
                A = 1,
                B = 2
            }
            public string Name;
            public int Age { get; set; }
            public TrapEnum Trap { get; set; }
        
        }

        public void TestStructs()
        {
            var car = connection.Query<Car>("select 'Ford' Name, 21 Age, 2 Trap").First();

            car.Age.IsEqualTo(21);
            car.Name.IsEqualTo("Ford");
            ((int)car.Trap).IsEqualTo(2);
        }

        public void SelectListInt()
        {
            connection.Query<int>("select 1 union all select 2 union all select 3")
              .IsSequenceEqualTo(new[] { 1, 2, 3 });
        }
        public void SelectBinary()
        {
            connection.Query<byte[]>("select cast(1 as varbinary(4))").First().SequenceEqual(new byte[] { 1 });
        }
        public void PassInIntArray()
        {
            connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[] { 1, 2, 3 }.AsEnumerable() })
             .IsSequenceEqualTo(new[] { 1, 2, 3 });
        }

        public void PassInEmptyIntArray()
        {
            connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[0] })
             .IsSequenceEqualTo(new int[0]);
        }

        public void TestSchemaChanged()
        {
            connection.Execute("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')");
            var d = connection.Query<Dog>("select * from #dog").Single();
            d.Name.IsEqualTo("Alf");
            d.Age.IsEqualTo(1);
            connection.Execute("alter table #dog drop column Name");
            d = connection.Query<Dog>("select * from #dog").Single();
            d.Name.IsNull();
            d.Age.IsEqualTo(1);
            connection.Execute("drop table #dog");
        }

        public void TestSchemaChangedMultiMap()
        {
            connection.Execute("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')");
            var tuple = connection.Query<Dog, Dog, Tuple<Dog, Dog>>("select * from #dog d1 join #dog d2 on 1=1", (d1, d2) => Tuple.Create(d1, d2), splitOn: "Age").Single();

            tuple.Item1.Name.IsEqualTo("Alf");
            tuple.Item1.Age.IsEqualTo(1);
            tuple.Item2.Name.IsEqualTo("Alf");
            tuple.Item2.Age.IsEqualTo(1);

            connection.Execute("alter table #dog drop column Name");
            tuple = connection.Query<Dog, Dog, Tuple<Dog, Dog>>("select * from #dog d1 join #dog d2 on 1=1", (d1, d2) => Tuple.Create(d1, d2), splitOn: "Age").Single();

            tuple.Item1.Name.IsNull();
            tuple.Item1.Age.IsEqualTo(1);
            tuple.Item2.Name.IsNull();
            tuple.Item2.Age.IsEqualTo(1);

            connection.Execute("drop table #dog");
        }

        public void TestReadMultipleIntegersWithSplitOnAny()
        {
            connection.Query<int, int, int, Tuple<int, int, int>>(
                "select 1,2,3 union all select 4,5,6", Tuple.Create, splitOn: "*")
             .IsSequenceEqualTo(new[] { Tuple.Create(1, 2, 3), Tuple.Create(4, 5, 6) });
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

        enum EnumParam : short
        {
            None, A, B
        }
        class EnumParamObject
        {
            public EnumParam A { get; set; }
            public EnumParam? B { get; set; }
            public EnumParam? C { get; set; }
        }
        public void TestEnumParams()
        {
            EnumParam a = EnumParam.A;
            EnumParam? b = EnumParam.B, c = null;
            var obj = connection.Query<EnumParamObject>("select @a as A, @b as B, @c as C",
                new { a, b, c }).Single();
            obj.A.IsEqualTo(EnumParam.A);
            obj.B.IsEqualTo(EnumParam.B);
            obj.C.IsEqualTo(null);
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
            var dog = connection.Query<Dog>("select '' as Extra, 1 as Age, 0.1 as Name1 , Id = @id", new { Id = guid });

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
            connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new { strings = new[] { "a", "b", "c" } })
                .IsSequenceEqualTo(new[] { "a", "b", "c" });

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
    drop table #t", new { a = 1, b = 2 }).IsEqualTo(2);
        }
        public void TestExecuteCommandWithHybridParameters()
        {
            var p = new DynamicParameters(new { a = 1, b = 2 });
            p.Add("c", dbType: DbType.Int32, direction: ParameterDirection.Output);
            connection.Execute(@"set @c = @a + @b", p);
            p.Get<int>("@c").IsEqualTo(3);
        }
        public void TestExecuteMultipleCommand()
        {
            connection.Execute("create table #t(i int)");
            int tally = connection.Execute(@"insert #t (i) values(@a)", new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } });
            int sum = connection.Query<int>("select sum(i) from #t drop table #t").First();
            tally.IsEqualTo(4);
            sum.IsEqualTo(10);
        }

        class Student
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public void TestExecuteMultipleCommandStrongType()
        {
            connection.Execute("create table #t(Name nvarchar(max), Age int)");
            int tally = connection.Execute(@"insert #t (Name,Age) values(@Name, @Age)", new List<Student> 
            {
                new Student{Age = 1, Name = "sam"},
                new Student{Age = 2, Name = "bob"}
            });
            int sum = connection.Query<int>("select sum(Age) from #t drop table #t").First();
            tally.IsEqualTo(2);
            sum.IsEqualTo(3);
        }

        public void TestExecuteMultipleCommandObjectArray()
        {
            connection.Execute("create table #t(i int)");
            int tally = connection.Execute(@"insert #t (i) values(@a)", new object[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } });
            int sum = connection.Query<int>("select sum(i) from #t drop table #t").First();
            tally.IsEqualTo(4);
            sum.IsEqualTo(10);
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
            private int Priv { set { _priv = value; } }
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
            var en = connection.Query<int>("select 1 as one union all select 2 as one", buffered: false);
            var i = en.GetEnumerator();
            i.MoveNext();

            bool gotException = false;
            try
            {
                var x = connection.Query<int>("select 1 as one", buffered: false).First();
            }
            catch (Exception)
            {
                gotException = true;
            }

            while (i.MoveNext())
            { }

            // should not exception, since enumertated
            en = connection.Query<int>("select 1 as one", buffered: false);

            gotException.IsTrue();
        }

        public void TestEnumerationDynamic()
        {
            var en = connection.Query("select 1 as one union all select 2 as one", buffered: false);
            var i = en.GetEnumerator();
            i.MoveNext();

            bool gotException = false;
            try
            {
                var x = connection.Query("select 1 as one", buffered: false).First();
            }
            catch (Exception)
            {
                gotException = true;
            }

            while (i.MoveNext())
            { }

            // should not exception, since enumertated
            en = connection.Query("select 1 as one", buffered: false);

            gotException.IsTrue();
        }

        public void TestNakedBigInt()
        {
            long foo = 12345;
            var result = connection.Query<long>("select @foo", new { foo }).Single();
            foo.IsEqualTo(result);
        }

        public void TestBigIntMember()
        {
            long foo = 12345;
            var result = connection.Query<WithBigInt>(@"
declare @bar table(Value bigint)
insert @bar values (@foo)
select * from @bar", new { foo }).Single();
            result.Value.IsEqualTo(foo);
        }
        class WithBigInt
        {
            public long Value { get; set; }
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

            var data = connection.Query<Post, User, Post>(sql, (post, user) => { post.Owner = user; return post; }).ToList();
            var p = data.First();

            p.Content.IsEqualTo("Sams Post1");
            p.Id.IsEqualTo(1);
            p.Owner.Name.IsEqualTo("Sam");
            p.Owner.Id.IsEqualTo(99);

            data[2].Owner.IsNull();

            connection.Execute("drop table #Users drop table #Posts");
        }



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
        }
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
        public void TestFieldsAndPrivates()
        {
            var data = connection.Query<TestFieldCaseAndPrivatesEntity>(
                @"select a=1,b=2,c=3,d=4,f='5'").Single();


            data.a.IsEqualTo(1);
            data.GetB().IsEqualTo(2);
            data.c.IsEqualTo(3);
            data.GetD().IsEqualTo(4);
            data.e.IsEqualTo(5);


        }

        private class TestFieldCaseAndPrivatesEntity
        {
            public int a { get; set; }
            private int b { get; set; }
            public int GetB() { return b; }
            public int c = 0;
            private int d = 0;
            public int GetD() { return d; }
            public int e { get; set; }
            private string f
            {
                get { return e.ToString(); }
                set { e = int.Parse(value); }
            }
        }

        public void TestMultiReaderBasic()
        {
            var sql = @"select 1 as Id union all select 2 as Id     select 'abc' as name   select 1 as Id union all select 2 as Id";
            int i, j;
            string s;
            using (var multi = connection.QueryMultiple(sql))
            {
                i = multi.Read<int>().First();
                s = multi.Read<string>().Single();
                j = multi.Read<int>().Sum();
            }
            Assert.IsEqualTo(i, 1);
            Assert.IsEqualTo(s, "abc");
            Assert.IsEqualTo(j, 3);
        }
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


        public class PostCE
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }

            public AuthorCE Author { get; set; }
        }

        public class AuthorCE
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        public void MultiRSSqlCE()
        {
            if (File.Exists("Test.sdf"))
                File.Delete("Test.sdf");

            var cnnStr = "Data Source = Test.sdf;";
            var engine = new SqlCeEngine(cnnStr);
            engine.CreateDatabase();

            using (var cnn = new SqlCeConnection(cnnStr))
            {
                cnn.Open();

                cnn.Execute("create table Posts (ID int, Title nvarchar(50), Body nvarchar(50), AuthorID int)");
                cnn.Execute("create table Authors (ID int, Name nvarchar(50))");

                cnn.Execute("insert Posts values (1,'title','body',1)");
                cnn.Execute("insert Posts values(2,'title2','body2',null)");
                cnn.Execute("insert Authors values(1,'sam')");

                var data = cnn.Query<PostCE, AuthorCE, PostCE>(@"select * from Posts p left join Authors a on a.ID = p.AuthorID", (post, author) => { post.Author = author; return post; }).ToList();
                var firstPost = data.First();
                firstPost.Title.IsEqualTo("title");
                firstPost.Author.Name.IsEqualTo("sam");
                data[1].Author.IsNull();
                cnn.Close();
            }
        }

        enum TestEnum : byte
        {
            Bla = 1
        }
        class TestEnumClass
        {
            public TestEnum? EnumEnum { get; set; }
        }
        class TestEnumClassNoNull
        {
            public TestEnum EnumEnum { get; set; }
        }
        public void TestEnumWeirdness()
        {
            connection.Query<TestEnumClass>("select null as [EnumEnum]").First().EnumEnum.IsEqualTo(null);
            connection.Query<TestEnumClass>("select cast(1 as tinyint) as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
        }
        void Foo()
        {
            string s = "Bla";
            var obj = new TestEnumClassNoNull();
            obj.EnumEnum = (TestEnum)Enum.Parse(typeof(TestEnum), s, true);
        }
        public void TestEnumStrings()
        {
            connection.Query<TestEnumClassNoNull>("select 'BLA' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
            connection.Query<TestEnumClassNoNull>("select 'bla' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);

            connection.Query<TestEnumClass>("select 'BLA' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
            connection.Query<TestEnumClass>("select 'bla' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
        }

        public void TestSupportForParamDictionary()
        {
            var p = new DynamicParameters();
            p.Add("name", "bob");
            p.Add("age", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Query<string>("set @age = 11 select @name", p).First().IsEqualTo("bob");

            p.Get<int>("age").IsEqualTo(11);
        }


        public void TestProcSupport()
        {
            var p = new DynamicParameters();
            p.Add("a", 11);
            p.Add("b", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("c", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            connection.Execute(@"create proc #TestProc 
	@a int,
	@b int output
as 
begin
	set @b = 999
	select 1111
	return @a
end");
            connection.Query<int>("#TestProc", p, commandType: CommandType.StoredProcedure).First().IsEqualTo(1111);

            p.Get<int>("c").IsEqualTo(11);
            p.Get<int>("b").IsEqualTo(999);

        }

        public void TestDbString()
        {
            var obj = connection.Query("select datalength(@a) as a, datalength(@b) as b, datalength(@c) as c, datalength(@d) as d, datalength(@e) as e, datalength(@f) as f",
                new
                {
                    a = new DbString { Value = "abcde", IsFixedLength = true, Length = 10, IsAnsi = true },
                    b = new DbString { Value = "abcde", IsFixedLength = true, Length = 10, IsAnsi = false },
                    c = new DbString { Value = "abcde", IsFixedLength = false, Length = 10, IsAnsi = true },
                    d = new DbString { Value = "abcde", IsFixedLength = false, Length = 10, IsAnsi = false },
                    e = new DbString { Value = "abcde", IsAnsi = true },
                    f = new DbString { Value = "abcde", IsAnsi = false },
                }).First();
            ((int)obj.a).IsEqualTo(10);
            ((int)obj.b).IsEqualTo(20);
            ((int)obj.c).IsEqualTo(5);
            ((int)obj.d).IsEqualTo(10);
            ((int)obj.e).IsEqualTo(5);
            ((int)obj.f).IsEqualTo(10);
        }

        class Person
        {
            public int PersonId { get; set; }
            public string Name { get; set; }
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

        public void TestFlexibleMultiMapping()
        {
            var sql =
@"select 
    1 as PersonId, 'bob' as Name, 
    2 as AddressId, 'abc street' as Name, 1 as PersonId,
    3 as Id, 'fred' as Name
    ";
            var personWithAddress = connection.Query<Person, Address, Extra, Tuple<Person, Address, Extra>>
                (sql, (p, a, e) => Tuple.Create(p, a, e), splitOn: "AddressId,Id").First();

            personWithAddress.Item1.PersonId.IsEqualTo(1);
            personWithAddress.Item1.Name.IsEqualTo("bob");
            personWithAddress.Item2.AddressId.IsEqualTo(2);
            personWithAddress.Item2.Name.IsEqualTo("abc street");
            personWithAddress.Item2.PersonId.IsEqualTo(1);
            personWithAddress.Item3.Id.IsEqualTo(3);
            personWithAddress.Item3.Name.IsEqualTo("fred");

        }


        public void TestFastExpandoSupportsIDictionary()
        {
            var row = connection.Query("select 1 A, 'two' B").First() as IDictionary<string, object>;
            row["A"].IsEqualTo(1);
            row["B"].IsEqualTo("two");
        }


        class PrivateDan
        {
            public int Shadow { get; set; }
            private string ShadowInDB
            {
                set
                {
                    Shadow = value == "one" ? 1 : 0;
                }
            }
        }
        public void TestDapperSetsPrivates()
        {
            connection.Query<PrivateDan>("select 'one' ShadowInDB").First().Shadow.IsEqualTo(1);
        }


        class IntDynamicParam : Dapper.SqlMapper.IDynamicParameters
        {
            IEnumerable<int> numbers;
            public IntDynamicParam(IEnumerable<int> numbers)
            {
                this.numbers = numbers;
            }

            public void AddParameters(IDbCommand command, Dapper.SqlMapper.Identity identity)
            {
                var sqlCommand = (SqlCommand)command;
                sqlCommand.CommandType = CommandType.StoredProcedure;

                List<Microsoft.SqlServer.Server.SqlDataRecord> number_list = new List<Microsoft.SqlServer.Server.SqlDataRecord>();

                // Create an SqlMetaData object that describes our table type.
                Microsoft.SqlServer.Server.SqlMetaData[] tvp_definition = { new Microsoft.SqlServer.Server.SqlMetaData("n", SqlDbType.Int) };

                foreach (int n in numbers)
                {
                    // Create a new record, using the metadata array above.
                    Microsoft.SqlServer.Server.SqlDataRecord rec = new Microsoft.SqlServer.Server.SqlDataRecord(tvp_definition);
                    rec.SetInt32(0, n);    // Set the value.
                    number_list.Add(rec);      // Add it to the list.
                }

                // Add the table parameter.
                var p = sqlCommand.Parameters.Add("ints", SqlDbType.Structured);
                p.Direction = ParameterDirection.Input;
                p.TypeName = "int_list_type";
                p.Value = number_list;

            }
        }

        // SQL Server specific test to demonstrate TVP 
        public void TestTVP()
        {
            try
            {
                connection.Execute("CREATE TYPE int_list_type AS TABLE (n int NOT NULL PRIMARY KEY)");
                connection.Execute("CREATE PROC get_ints @ints int_list_type READONLY AS select * from @ints");

                var nums = connection.Query<int>("get_ints", new IntDynamicParam(new int[] { 1, 2, 3 })).ToList();
                nums[0].IsEqualTo(1);
                nums[1].IsEqualTo(2);
                nums[2].IsEqualTo(3);
                nums.Count.IsEqualTo(3);

            }
            finally
            {
                try
                {
                    connection.Execute("DROP PROC get_ints");
                }
                finally
                {
                    connection.Execute("DROP TYPE int_list_type");
                }
            }
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
            parents.Count().IsEqualTo(3);
            parents[1].Children.Select(c => c.Id).SequenceEqual(new[] { 1, 2, 4 }).IsTrue();
            parents[2].Children.Select(c => c.Id).SequenceEqual(new[] { 3 }).IsTrue();
            parents[3].Children.Select(c => c.Id).SequenceEqual(new[] { 5 }).IsTrue();
        }


        /* TODO:
         * 
        public void TestMagicParam()
        {
            // magic params allow you to pass in single params without using an anon class
            // this test fails for now, but I would like to support a single param by parsing the sql with regex and remapping. 

            var first = connection.Query("select @a as a", 1).First();
            Assert.IsEqualTo(first.a, 1);
        }
         * */

        class WithBizarreData
        {
            public GenericUriParser Foo { get; set; }
            public int Bar { get; set; }
        }
        public void TestUnexpectedDataMessage()
        {
            string msg = null;
            try
            {
                connection.Query<int>("select count(1) where 1 = @Foo", new WithBizarreData { Foo = new GenericUriParser(GenericUriParserOptions.Default), Bar = 23 }).First();

            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            msg.IsEqualTo("The member Foo of type System.GenericUriParser cannot be used as a parameter value");
        }
        public void TestUnexpectedButFilteredDataMessage()
        {
            int i = connection.Query<int>("select @Bar", new WithBizarreData { Foo = new GenericUriParser(GenericUriParserOptions.Default), Bar = 23 }).Single();

            i.IsEqualTo(23);
        }

        class WithCharValue
        {
            public char Value { get; set; }
            public char? ValueNullable { get; set; }
        }
        public void TestCharInputAndOutput()
        {
            const char test = '〠';
            char c = connection.Query<char>("select @c", new { c = test }).Single();

            c.IsEqualTo(test);

            var obj = connection.Query<WithCharValue>("select @Value as Value", new WithCharValue { Value = c }).Single();

            obj.Value.IsEqualTo(test);
        }
        public void TestNullableCharInputAndOutputNonNull()
        {
            char? test = '〠';
            char? c = connection.Query<char?>("select @c", new { c = test }).Single();

            c.IsEqualTo(test);

            var obj = connection.Query<WithCharValue>("select @ValueNullable as ValueNullable", new WithCharValue { ValueNullable = c }).Single();

            obj.ValueNullable.IsEqualTo(test);
        }
        public void TestNullableCharInputAndOutputNull()
        {
            char? test = null;
            char? c = connection.Query<char?>("select @c", new { c = test }).Single();

            c.IsEqualTo(test);

            var obj = connection.Query<WithCharValue>("select @ValueNullable as ValueNullable", new WithCharValue { ValueNullable = c }).Single();

            obj.ValueNullable.IsEqualTo(test);
        }
        public void TestInvalidSplitCausesNiceError()
        {
            try
            {
                connection.Query<User, User, User>("select 1 A, 2 B, 3 C", (x, y) => x);
            }
            catch (ArgumentException)
            {
                // expecting an app exception due to multi mapping being bodged 
            }

            try
            {
                connection.Query<dynamic, dynamic, dynamic>("select 1 A, 2 B, 3 C", (x, y) => x);
            }
            catch (ArgumentException)
            {
                // expecting an app exception due to multi mapping being bodged 
            }
        }



        class Comment
        {
            public int Id { get; set; }
            public string CommentData { get; set; }
        }


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

            var sql = @"SELECT p.* FROM #Posts p

select p.*, u.Id, u.Name + '0' Name, c.Id, c.CommentData from #Posts p 
left join #Users u on u.Id = p.OwnerId 
left join #Comments c on c.postId = p.Id
where p.Id = 1
Order by p.Id";

            var grid = connection.QueryMultiple(sql);

            var post1 = grid.Read<Post>().ToList();

            var post2 = grid.Read<Post, User, Comment, Post>((post, user, comment) => { post.Owner = user; post.Comment = comment; return post; }).SingleOrDefault();

            post2.Comment.Id.IsEqualTo(1);
            post2.Owner.Id.IsEqualTo(99);


            connection.Execute("drop table #Users drop table #Posts drop table #Comments");
        }

        public void TestReadDynamicWithGridReader()
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

            var sql = @"SELECT * FROM #Users ORDER BY Id
                        SELECT * FROM #Posts ORDER BY Id DESC";

            var grid = connection.QueryMultiple(sql);

            var users = grid.Read().ToList();
            var posts = grid.Read().ToList();

            users.Count.IsEqualTo(2);
            posts.Count.IsEqualTo(3);

            ((int)users.First().Id).IsEqualTo(2);
            ((int)posts.First().Id).IsEqualTo(3);

            connection.Execute("drop table #Users drop table #Posts");
        }

        public void TestDynamicParamNullSupport()
        {
            var p = new DynamicParameters();

            p.Add("@b", dbType: DbType.Int32, direction: ParameterDirection.Output);
            connection.Execute("select @b = null", p);

            p.Get<int?>("@b").IsNull();
        }
        class Foo1
        {
            public int Id;
            public int BarId { get; set; }
        }
        class Bar1
        {
            public int BarId;
            public string Name { get; set; }
        }
        public void TestMultiMapperIsNotConfusedWithUnorderedCols()
        {
            var result = connection.Query<Foo1, Bar1, Tuple<Foo1, Bar1>>("select 1 as Id, 2 as BarId, 3 as BarId, 'a' as Name", (f, b) => Tuple.Create(f, b), splitOn: "BarId").First();

            result.Item1.Id.IsEqualTo(1);
            result.Item1.BarId.IsEqualTo(2);
            result.Item2.BarId.IsEqualTo(3);
            result.Item2.Name.IsEqualTo("a");
        }
        public void TestLinqBinaryToClass()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);

            var output = connection.Query<WithBinary>("select @input as [Value]", new { input }).First().Value;

            output.ToArray().IsSequenceEqualTo(orig);
        }

        public void TestLinqBinaryRaw()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);

            var output = connection.Query<System.Data.Linq.Binary>("select @input as [Value]", new { input }).First();

            output.ToArray().IsSequenceEqualTo(orig);
        }

        class WithBinary
        {
            public System.Data.Linq.Binary Value { get; set; }
        }


        class WithPrivateConstructor
        {
            public int Foo { get; set; }
            private WithPrivateConstructor() { }
        }

        public void TestWithNonPublicConstructor()
        {
            var output = connection.Query<WithPrivateConstructor>("select 1 as Foo").First();
            output.Foo.IsEqualTo(1);
        }

        public void TestAppendingAnonClasses()
        {
            DynamicParameters p = new DynamicParameters();
            p.AddDynamicParams(new { A = 1, B = 2 });
            p.AddDynamicParams(new { C = 3, D = 4 });

            var result = connection.Query("select @A a,@B b,@C c,@D d", p).Single();

            ((int)result.a).IsEqualTo(1);
            ((int)result.b).IsEqualTo(2);
            ((int)result.c).IsEqualTo(3);
            ((int)result.d).IsEqualTo(4);
        }

        public void TestAppendingAList()
        {
            DynamicParameters p = new DynamicParameters();
            var list = new int[] { 1, 2, 3 };
            p.AddDynamicParams(new { list });

            var result = connection.Query<int>("select * from (select 1 A union all select 2 union all select 3) X where A in @list", p).ToList();

            result[0].IsEqualTo(1);
            result[1].IsEqualTo(2);
            result[2].IsEqualTo(3);
        }

        public void TestUniqueIdentifier()
        {
            var guid = Guid.NewGuid();
            var result = connection.Query<Guid>("declare @foo uniqueidentifier set @foo = @guid select @foo", new { guid }).Single();
            result.IsEqualTo(guid);
        }
        public void TestNullableUniqueIdentifierNonNull()
        {
            Guid? guid = Guid.NewGuid();
            var result = connection.Query<Guid?>("declare @foo uniqueidentifier set @foo = @guid select @foo", new { guid }).Single();
            result.IsEqualTo(guid);
        }
        public void TestNullableUniqueIdentifierNull()
        {
            Guid? guid = null;
            var result = connection.Query<Guid?>("declare @foo uniqueidentifier set @foo = @guid select @foo", new { guid }).Single();
            result.IsEqualTo(guid);
        }


        public void TestFailInASaneWayWithWrongStructColumnTypes()
        {
            try
            {
                connection.Query<CanHazInt>("select cast(1 as bigint) Value").Single();
                throw new Exception("Should not have got here");
            } catch(DataException ex)
            {
                ex.Message.IsEqualTo("Error parsing column 0 (Value=1 - Int64)");
            }
        }


        public void TestProcWithOutParameter()
        {
            connection.Execute(
                @"CREATE PROCEDURE #TestProcWithOutParameter
        @ID int output,
        @Foo varchar(100),
        @Bar int
        AS
        SET @ID = @Bar + LEN(@Foo)");
            var obj = new
            {
                ID = 0,
                Foo = "abc",
                Bar = 4
            };
            var args = new DynamicParameters(obj);
            args.Add("ID", 0, direction: ParameterDirection.Output);
            connection.Execute("#TestProcWithOutParameter", args, commandType: CommandType.StoredProcedure);
            args.Get<int>("ID").IsEqualTo(7);
        }
        public void TestProcWithOutAndReturnParameter()
        {
            connection.Execute(
                @"CREATE PROCEDURE #TestProcWithOutAndReturnParameter
        @ID int output,
        @Foo varchar(100),
        @Bar int
        AS
        SET @ID = @Bar + LEN(@Foo)
        RETURN 42");
            var obj = new
            {
                ID = 0,
                Foo = "abc",
                Bar = 4
            };
            var args = new DynamicParameters(obj);
            args.Add("ID", 0, direction: ParameterDirection.Output);
            args.Add("result", 0, direction: ParameterDirection.ReturnValue);
            connection.Execute("#TestProcWithOutAndReturnParameter", args, commandType: CommandType.StoredProcedure);
            args.Get<int>("ID").IsEqualTo(7);
            args.Get<int>("result").IsEqualTo(42);
        }
        struct CanHazInt
        {
            public int Value { get; set; }
        }
        public void TestInt16Usage()
        {
            connection.Query<short>("select cast(42 as smallint)").Single().IsEqualTo((short)42);
            connection.Query<short?>("select cast(42 as smallint)").Single().IsEqualTo((short?)42);
            connection.Query<short?>("select cast(null as smallint)").Single().IsEqualTo((short?)null);

            // hmmm.... these don't work currently... adding TODO
            //connection.Query<ShortEnum>("select cast(42 as smallint)").Single().IsEqualTo((ShortEnum)42);
            //connection.Query<ShortEnum?>("select cast(42 as smallint)").Single().IsEqualTo((ShortEnum?)42);
            //connection.Query<ShortEnum?>("select cast(null as smallint)").Single().IsEqualTo((ShortEnum?)null);

            var row =
                connection.Query<WithInt16Values>(
                    "select cast(1 as smallint) as NonNullableInt16, cast(2 as smallint) as NullableInt16, cast(3 as smallint) as NonNullableInt16Enum, cast(4 as smallint) as NullableInt16Enum")
                    .Single();
            row.NonNullableInt16.IsEqualTo((short)1);
            row.NullableInt16.IsEqualTo((short)2);
            row.NonNullableInt16Enum.IsEqualTo(ShortEnum.Three);
            row.NullableInt16Enum.IsEqualTo(ShortEnum.Four);

            row =
    connection.Query<WithInt16Values>(
        "select cast(5 as smallint) as NonNullableInt16, cast(null as smallint) as NullableInt16, cast(6 as smallint) as NonNullableInt16Enum, cast(null as smallint) as NullableInt16Enum")
        .Single();
            row.NonNullableInt16.IsEqualTo((short)5);
            row.NullableInt16.IsEqualTo((short?)null);
            row.NonNullableInt16Enum.IsEqualTo(ShortEnum.Six);
            row.NullableInt16Enum.IsEqualTo((ShortEnum?)null);
        }


        public class WithInt16Values
        {
            public short NonNullableInt16 { get; set; }
            public short? NullableInt16 { get; set; }
            public ShortEnum NonNullableInt16Enum { get; set; }
            public ShortEnum? NullableInt16Enum { get; set; }

        }
        public enum ShortEnum : short
        {
            Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6
        }
    }
}
