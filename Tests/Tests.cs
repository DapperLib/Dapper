//#define POSTGRESQL // uncomment to run postgres tests
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
using System.Dynamic;
using System.ComponentModel;
using Microsoft.CSharp.RuntimeBinder;
using System.Data.Common;
using System.Globalization;
using System.Threading;
#if POSTGRESQL
using Npgsql;
#endif

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


        class MultipleConstructors
        {
            public MultipleConstructors()
            {

            }
            public MultipleConstructors(int a, string b)
            {
                A = a + 1;
                B = b + "!";
            }
            public int A { get; set; }
            public string B { get; set; }
        }

        public void TestMultipleConstructors()
        {
            MultipleConstructors mult = connection.Query<MultipleConstructors>("select 0 A, 'Dapper' b").First();
            mult.A.IsEqualTo(0);
            mult.B.IsEqualTo("Dapper");
        }

        class ConstructorsWithAccessModifiers
        {
            private ConstructorsWithAccessModifiers()
            {
            }
            public ConstructorsWithAccessModifiers(int a, string b)
            {
                A = a + 1;
                B = b + "!";
            }
            public int A { get; set; }
            public string B { get; set; }
        }

        public void TestConstructorsWithAccessModifiers()
        {
            ConstructorsWithAccessModifiers value = connection.Query<ConstructorsWithAccessModifiers>("select 0 A, 'Dapper' b").First();
            value.A.IsEqualTo(1);
            value.B.IsEqualTo("Dapper!");
        }

        class NoDefaultConstructor
        {
            public NoDefaultConstructor(int a1, int? b1, float f1, string s1, Guid G1)
            {
                A = a1;
                B = b1;
                F = f1;
                S = s1;
                G = G1;
            }
            public int A { get; set; }
            public int? B { get; set; }
            public float F { get; set; }
            public string S { get; set; }
            public Guid G { get; set; }
        }

        public void TestNoDefaultConstructor()
        {
            var guid = Guid.NewGuid();
            NoDefaultConstructor nodef = connection.Query<NoDefaultConstructor>("select CAST(NULL AS integer) A1,  CAST(NULL AS integer) b1, CAST(NULL AS real) f1, 'Dapper' s1, G1 = @id", new { id = guid }).First();
            nodef.A.IsEqualTo(0);
            nodef.B.IsEqualTo(null);
            nodef.F.IsEqualTo(0);
            nodef.S.IsEqualTo("Dapper");
            nodef.G.IsEqualTo(guid);
        }

        class NoDefaultConstructorWithChar
        {
            public NoDefaultConstructorWithChar(char c1, char? c2, char? c3)
            {
                Char1 = c1;
                Char2 = c2;
                Char3 = c3;
            }
            public char Char1 { get; set; }
            public char? Char2 { get; set; }
            public char? Char3 { get; set; }
        }

        public void TestNoDefaultConstructorWithChar()
        {
            const char c1 = 'ą';
            const char c3 = 'ó';
            NoDefaultConstructorWithChar nodef = connection.Query<NoDefaultConstructorWithChar>("select @c1 c1, @c2 c2, @c3 c3", new { c1 = c1, c2 = (char?)null, c3 = c3 }).First();
            nodef.Char1.IsEqualTo(c1);
            nodef.Char2.IsEqualTo(null);
            nodef.Char3.IsEqualTo(c3);
        }

        class NoDefaultConstructorWithEnum
        {
            public NoDefaultConstructorWithEnum(ShortEnum e1, ShortEnum? n1, ShortEnum? n2)
            {
                E = e1;
                NE1 = n1;
                NE2 = n2;
            }
            public ShortEnum E { get; set; }
            public ShortEnum? NE1 { get; set; }
            public ShortEnum? NE2 { get; set; }
        }

        public void TestNoDefaultConstructorWithEnum()
        {
            NoDefaultConstructorWithEnum nodef = connection.Query<NoDefaultConstructorWithEnum>("select cast(2 as smallint) E1, cast(5 as smallint) n1, cast(null as smallint) n2").First();
            nodef.E.IsEqualTo(ShortEnum.Two);
            nodef.NE1.IsEqualTo(ShortEnum.Five);
            nodef.NE2.IsEqualTo(null);
        }

        class NoDefaultConstructorWithBinary
        {
            public System.Data.Linq.Binary Value { get; set; }
            public int Ynt { get; set; }
            public NoDefaultConstructorWithBinary(System.Data.Linq.Binary val)
            {
                Value = val;
            }
        }

        public void TestNoDefaultConstructorBinary()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);
            var output = connection.Query<NoDefaultConstructorWithBinary>("select @input as val", new { input }).First().Value;
            output.ToArray().IsSequenceEqualTo(orig);
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
#pragma warning disable 0649
            public string Name;
#pragma warning restore 0649
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
            try
            {
                var d = connection.Query<Dog>("select * from #dog").Single();
                d.Name.IsEqualTo("Alf");
                d.Age.IsEqualTo(1);
                connection.Execute("alter table #dog drop column Name");
                d = connection.Query<Dog>("select * from #dog").Single();
                d.Name.IsNull();
                d.Age.IsEqualTo(1);
            }
            finally
            {
                connection.Execute("drop table #dog");
            }
        }

        public void TestSchemaChangedMultiMap()
        {
            connection.Execute("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')");
            try
            {
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
            }
            finally
            {
                connection.Execute("drop table #dog");
            }
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

        // http://code.google.com/p/dapper-dot-net/issues/detail?id=70
        // https://connect.microsoft.com/VisualStudio/feedback/details/381934/sqlparameter-dbtype-dbtype-time-sets-the-parameter-to-sqldbtype-datetime-instead-of-sqldbtype-time
        public void TestTimeSpanParam()
        {
            connection.Query<TimeSpan>("select @ts", new { ts = TimeSpan.FromMinutes(42) }).First()
                .IsEqualTo(TimeSpan.FromMinutes(42));
        }

        public void TestStrings()
        {
            connection.Query<string>(@"select 'a' a union select 'b'")
                .IsSequenceEqualTo(new[] { "a", "b" });
        }

        // see http://stackoverflow.com/questions/16726709/string-format-with-sql-wildcard-causing-dapper-query-to-break
        public void CheckComplexConcat()
        {
            string end_wildcard = @"
SELECT * FROM #users16726709
WHERE (first_name LIKE CONCAT(@search_term, '%') OR last_name LIKE CONCAT(@search_term, '%'));";

            string both_wildcards = @"
SELECT * FROM #users16726709
WHERE (first_name LIKE CONCAT('%', @search_term, '%') OR last_name LIKE CONCAT('%', @search_term, '%'));";

            string formatted = @"
SELECT * FROM #users16726709
WHERE (first_name LIKE {0} OR last_name LIKE {0});";

            string use_end_only = @"CONCAT(@search_term, '%')";
            string use_both = @"CONCAT('%', @search_term, '%')";

            // if true, slower query due to not being able to use indices, but will allow searching inside strings 
            bool allow_start_wildcards = false;

            string query = String.Format(formatted, allow_start_wildcards ? use_both : use_end_only);
            string term = "F"; // the term the user searched for

            connection.Execute(@"create table #users16726709 (first_name varchar(200), last_name varchar(200))
insert #users16726709 values ('Fred','Bloggs') insert #users16726709 values ('Tony','Farcus') insert #users16726709 values ('Albert','TenoF')");

            // Using Dapper
            connection.Query(end_wildcard, new { search_term = term }).Count().IsEqualTo(2);
            connection.Query(both_wildcards, new { search_term = term }).Count().IsEqualTo(3);
            connection.Query(query, new { search_term = term }).Count().IsEqualTo(2);

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
        class EnumParamObjectNonNullable
        {
            public EnumParam A { get; set; }
            public EnumParam? B { get; set; }
            public EnumParam? C { get; set; }
        }
        public void TestEnumParamsWithNullable()
        {
            EnumParam a = EnumParam.A;
            EnumParam? b = EnumParam.B, c = null;
            var obj = connection.Query<EnumParamObject>("select @a as A, @b as B, @c as C",
                new { a, b, c }).Single();
            obj.A.IsEqualTo(EnumParam.A);
            obj.B.IsEqualTo(EnumParam.B);
            obj.C.IsEqualTo(null);
        }
        public void TestEnumParamsWithoutNullable()
        {
            EnumParam a = EnumParam.A;
            EnumParam b = EnumParam.B, c = 0;
            var obj = connection.Query<EnumParamObjectNonNullable>("select @a as A, @b as B, @c as C",
                new { a, b, c }).Single();
            obj.A.IsEqualTo(EnumParam.A);
            obj.B.IsEqualTo(EnumParam.B);
            obj.C.IsEqualTo((EnumParam)0);
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
            var dog = connection.Query<Dog>("select '' as Extra, 1 as Age, 0.1 as Name1 , Id = @id", new { id = guid });

            dog.Count()
               .IsEqualTo(1);

            dog.First().Age
                .IsEqualTo(1);

            dog.First().Id
                .IsEqualTo(guid);
        }

        // see http://stackoverflow.com/q/18847510/23354
        public void TestOleDbParameters()
        {
            using (var conn = new System.Data.OleDb.OleDbConnection(Program.OleDbConnectionString))
            {
                var row = conn.Query("select Id = ?, Age = ?",
                    new { foo = 12, bar = 23 } // these names DO NOT MATTER!!!
                ).Single();
                int age = row.Age;
                int id = row.Id;
                age.IsEqualTo(23);
                id.IsEqualTo(12);
            }
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
            try
            {
                int tally = connection.Execute(@"insert #t (i) values(@a)", new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } });
                int sum = connection.Query<int>("select sum(i) from #t").First();
                tally.IsEqualTo(4);
                sum.IsEqualTo(10);
            }
            finally
            {
                connection.Execute("drop table #t");
            }
        }

        class Student
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public void TestExecuteMultipleCommandStrongType()
        {
            connection.Execute("create table #t(Name nvarchar(max), Age int)");
            try
            {
                int tally = connection.Execute(@"insert #t (Name,Age) values(@Name, @Age)", new List<Student> 
            {
                new Student{Age = 1, Name = "sam"},
                new Student{Age = 2, Name = "bob"}
            });
                int sum = connection.Query<int>("select sum(Age) from #t").First();
                tally.IsEqualTo(2);
                sum.IsEqualTo(3);
            }
            finally
            {
                connection.Execute("drop table #t");
            }
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

            private int PrivGet { get { return _priv; } }
        }

        public void TestSetInternal()
        {
            connection.Query<TestObj>("select 10 as [Internal]").First()._internal.IsEqualTo(10);
        }

        public void TestSetPrivate()
        {
            connection.Query<TestObj>("select 10 as [Priv]").First()._priv.IsEqualTo(10);
        }


        public void TestExpandWithNullableFields()
        {
            var row = connection.Query("select null A, 2 B").Single();

            ((int?)row.A)
                .IsNull();

            ((int?)row.B)
                .IsEqualTo(2);
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

            // should not exception, since enumerated
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

        public void TestQueryMultipleBuffered()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select @x; select 4", new { x = 3 }))
            {
                var a = grid.Read<int>();
                var b = grid.Read<int>();
                var c = grid.Read<int>();
                var d = grid.Read<int>();

                a.Single().Equals(1);
                b.Single().Equals(2);
                c.Single().Equals(3);
                d.Single().Equals(4);
            }
        }

        public void TestQueryMultipleNonBufferedIncorrectOrder()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select @x; select 4", new { x = 3 }))
            {
                var a = grid.Read<int>(false);
                try
                {
                    var b = grid.Read<int>(false);
                    throw new InvalidOperationException(); // should have thrown
                }
                catch (InvalidOperationException)
                {
                    // that's expected
                }

            }
        }
        public void TestQueryMultipleNonBufferedCcorrectOrder()
        {
            using (var grid = connection.QueryMultiple("select 1; select 2; select @x; select 4", new { x = 3 }))
            {
                var a = grid.Read<int>(false).Single();
                var b = grid.Read<int>(false).Single();
                var c = grid.Read<int>(false).Single();
                var d = grid.Read<int>(false).Single();

                a.Equals(1);
                b.Equals(2);
                c.Equals(3);
                d.Equals(4);
            }
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
            public string Description { get; set; }
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

        
        public void ExecuteReader()
        {
            var dt = new DataTable();
            dt.Load(connection.ExecuteReader("select 3 as [three], 4 as [four]"));
            dt.Columns.Count.IsEqualTo(2);
            dt.Columns[0].ColumnName.IsEqualTo("three");
            dt.Columns[1].ColumnName.IsEqualTo("four");
            dt.Rows.Count.IsEqualTo(1);
            ((int)dt.Rows[0][0]).IsEqualTo(3);
            ((int)dt.Rows[0][1]).IsEqualTo(4);
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
        public void TestEnumStrings()
        {
            connection.Query<TestEnumClassNoNull>("select 'BLA' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
            connection.Query<TestEnumClassNoNull>("select 'bla' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);

            connection.Query<TestEnumClass>("select 'BLA' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
            connection.Query<TestEnumClass>("select 'bla' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
        }

        public void TestSupportForDynamicParameters()
        {
            var p = new DynamicParameters();
            p.Add("name", "bob");
            p.Add("age", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Query<string>("set @age = 11 select @name", p).First().IsEqualTo("bob");

            p.Get<int>("age").IsEqualTo(11);
        }
        public void TestSupportForExpandoObjectParameters()
        {
            dynamic p = new ExpandoObject();
            p.name = "bob";
            object parameters = p;
            string result = connection.Query<string>("select @name", parameters).First();
            result.IsEqualTo("bob");
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

        public void TestMultiMappingWithSplitOnSpaceBetweenCommas()
        {
            var sql = @"select 
                        1 as PersonId, 'bob' as Name, 
                        2 as AddressId, 'abc street' as Name, 1 as PersonId,
                        3 as Id, 'fred' as Name
                        ";
            var personWithAddress = connection.Query<Person, Address, Extra, Tuple<Person, Address, Extra>>
                (sql, (p, a, e) => Tuple.Create(p, a, e), splitOn: "AddressId, Id").First();

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

        class DynamicParameterWithIntTVP : Dapper.DynamicParameters, Dapper.SqlMapper.IDynamicParameters
        {
            IEnumerable<int> numbers;
            public DynamicParameterWithIntTVP(IEnumerable<int> numbers)
            {
                this.numbers = numbers;
            }

            public new void AddParameters(IDbCommand command, Dapper.SqlMapper.Identity identity)
            {
                base.AddParameters(command, identity);

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

        public void TestTVPWithAdditionalParams()
        {
            try
            {
                connection.Execute("CREATE TYPE int_list_type AS TABLE (n int NOT NULL PRIMARY KEY)");
                connection.Execute("CREATE PROC get_values @ints int_list_type READONLY, @stringParam varchar(20), @dateParam datetime AS select i.*, @stringParam as stringParam, @dateParam as dateParam from @ints i");

                var dynamicParameters = new DynamicParameterWithIntTVP(new int[] { 1, 2, 3 });
                dynamicParameters.AddDynamicParams(new { stringParam = "stringParam", dateParam = new DateTime(2012, 1, 1) });

                var results = connection.Query("get_values", dynamicParameters, commandType: CommandType.StoredProcedure).ToList();
                results.Count.IsEqualTo(3);
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    Assert.IsEqualTo(i + 1, result.n);
                    Assert.IsEqualTo("stringParam", result.stringParam);
                    Assert.IsEqualTo(new DateTime(2012, 1, 1), result.dateParam);
                }

            }
            finally
            {
                try
                {
                    connection.Execute("DROP PROC get_values");
                }
                finally
                {
                    connection.Execute("DROP TYPE int_list_type");
                }
            }
        }

        class IntCustomParam : Dapper.SqlMapper.ICustomQueryParameter
        {
            IEnumerable<int> numbers;
            public IntCustomParam(IEnumerable<int> numbers)
            {
                this.numbers = numbers;
            }

            public void AddParameter(IDbCommand command, string name)
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
                var p = sqlCommand.Parameters.Add(name, SqlDbType.Structured);
                p.Direction = ParameterDirection.Input;
                p.TypeName = "int_list_type";
                p.Value = number_list;
            }
        }

        public void TestTVPWithAnonymousObject()
        {
            try
            {
                connection.Execute("CREATE TYPE int_list_type AS TABLE (n int NOT NULL PRIMARY KEY)");
                connection.Execute("CREATE PROC get_ints @integers int_list_type READONLY AS select * from @integers");

                var nums = connection.Query<int>("get_ints", new { integers = new IntCustomParam(new int[] { 1, 2, 3 }) }, commandType: CommandType.StoredProcedure).ToList();
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
            try
            {
                var sql = @"SELECT p.* FROM #Posts p

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

        public class DbParams : Dapper.SqlMapper.IDynamicParameters, IEnumerable<IDbDataParameter>
        {
            private readonly List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            public IEnumerator<IDbDataParameter> GetEnumerator() { return parameters.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public void Add(IDbDataParameter value)
            {
                parameters.Add(value);
            }
            void Dapper.SqlMapper.IDynamicParameters.AddParameters(IDbCommand command,
                Dapper.SqlMapper.Identity identity)
            {
                foreach (IDbDataParameter parameter in parameters)
                    command.Parameters.Add(parameter);
            }
        }
        public void TestCustomParameters()
        {
            var args = new DbParams {
                new SqlParameter("foo", 123),
                new SqlParameter("bar", "abc")
            };
            var result = connection.Query("select Foo=@foo, Bar=@bar", args).Single();
            int foo = result.Foo;
            string bar = result.Bar;
            foo.IsEqualTo(123);
            bar.IsEqualTo("abc");
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
            try
            {
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
            }
            finally
            {
                connection.Execute("drop table #Users drop table #Posts");
            }
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

        public void TestAppendingADictionary()
        {
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("A", 1);
            dictionary.Add("B", "two");

            DynamicParameters p = new DynamicParameters();
            p.AddDynamicParams(dictionary);

            var result = connection.Query("select @A a, @B b", p).Single();

            ((int)result.a).IsEqualTo(1);
            ((string)result.b).IsEqualTo("two");
        }

        public void TestAppendingAnExpandoObject()
        {
            dynamic expando = new System.Dynamic.ExpandoObject();
            expando.A = 1;
            expando.B = "two";

            DynamicParameters p = new DynamicParameters();
            p.AddDynamicParams(expando);

            var result = connection.Query("select @A a, @B b", p).Single();

            ((int)result.a).IsEqualTo(1);
            ((string)result.b).IsEqualTo("two");
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

        public void TestAppendingAListAsDictionary()
        {
            DynamicParameters p = new DynamicParameters();
            var list = new int[] { 1, 2, 3 };
            var args = new Dictionary<string, object>();
            args.Add("ids", list);
            p.AddDynamicParams(args);

            var result = connection.Query<int>("select * from (select 1 A union all select 2 union all select 3) X where A in @ids", p).ToList();

            result[0].IsEqualTo(1);
            result[1].IsEqualTo(2);
            result[2].IsEqualTo(3);
        }

        public void TestAppendingAListByName()
        {
            DynamicParameters p = new DynamicParameters();
            var list = new int[] { 1, 2, 3 };
            p.Add("ids", list);

            var result = connection.Query<int>("select * from (select 1 A union all select 2 union all select 3) X where A in @ids", p).ToList();

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


        public void WorkDespiteHavingWrongStructColumnTypes()
        {
            var hazInt = connection.Query<CanHazInt>("select cast(1 as bigint) Value").Single();
            hazInt.Value.Equals(1);
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

            connection.Query<ShortEnum>("select cast(42 as smallint)").Single().IsEqualTo((ShortEnum)42);
            connection.Query<ShortEnum?>("select cast(42 as smallint)").Single().IsEqualTo((ShortEnum?)42);
            connection.Query<ShortEnum?>("select cast(null as smallint)").Single().IsEqualTo((ShortEnum?)null);

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
        public void TestInt32Usage()
        {
            connection.Query<int>("select cast(42 as int)").Single().IsEqualTo((int)42);
            connection.Query<int?>("select cast(42 as int)").Single().IsEqualTo((int?)42);
            connection.Query<int?>("select cast(null as int)").Single().IsEqualTo((int?)null);

            connection.Query<IntEnum>("select cast(42 as int)").Single().IsEqualTo((IntEnum)42);
            connection.Query<IntEnum?>("select cast(42 as int)").Single().IsEqualTo((IntEnum?)42);
            connection.Query<IntEnum?>("select cast(null as int)").Single().IsEqualTo((IntEnum?)null);

            var row =
                connection.Query<WithInt32Values>(
                    "select cast(1 as int) as NonNullableInt32, cast(2 as int) as NullableInt32, cast(3 as int) as NonNullableInt32Enum, cast(4 as int) as NullableInt32Enum")
                    .Single();
            row.NonNullableInt32.IsEqualTo((int)1);
            row.NullableInt32.IsEqualTo((int)2);
            row.NonNullableInt32Enum.IsEqualTo(IntEnum.Three);
            row.NullableInt32Enum.IsEqualTo(IntEnum.Four);

            row =
    connection.Query<WithInt32Values>(
        "select cast(5 as int) as NonNullableInt32, cast(null as int) as NullableInt32, cast(6 as int) as NonNullableInt32Enum, cast(null as int) as NullableInt32Enum")
        .Single();
            row.NonNullableInt32.IsEqualTo((int)5);
            row.NullableInt32.IsEqualTo((int?)null);
            row.NonNullableInt32Enum.IsEqualTo(IntEnum.Six);
            row.NullableInt32Enum.IsEqualTo((IntEnum?)null);
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
        public class WithInt32Values
        {
            public int NonNullableInt32 { get; set; }
            public int? NullableInt32 { get; set; }
            public IntEnum NonNullableInt32Enum { get; set; }
            public IntEnum? NullableInt32Enum { get; set; }
        }
        public enum IntEnum : int
        {
            Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6
        }

        public void TestTransactionCommit()
        {
            try
            {
                connection.Execute("create table #TransactionTest ([ID] int, [Value] varchar(32));");

                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute("insert into #TransactionTest ([ID], [Value]) values (1, 'ABC');", transaction: transaction);

                    transaction.Commit();
                }

                connection.Query<int>("select count(*) from #TransactionTest;").Single().IsEqualTo(1);
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }

        public void TestTransactionRollback()
        {
            connection.Execute("create table #TransactionTest ([ID] int, [Value] varchar(32));");

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute("insert into #TransactionTest ([ID], [Value]) values (1, 'ABC');", transaction: transaction);

                    transaction.Rollback();
                }

                connection.Query<int>("select count(*) from #TransactionTest;").Single().IsEqualTo(0);
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }

        public void TestCommandWithInheritedTransaction()
        {
            connection.Execute("create table #TransactionTest ([ID] int, [Value] varchar(32));");

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    var transactedConnection = new TransactedConnection(connection, transaction);

                    transactedConnection.Execute("insert into #TransactionTest ([ID], [Value]) values (1, 'ABC');");

                    transaction.Rollback();
                }

                connection.Query<int>("select count(*) from #TransactionTest;").Single().IsEqualTo(0);
            }
            finally
            {
                connection.Execute("drop table #TransactionTest;");
            }
        }

        public void TestReaderWhenResultsChange()
        {
            try
            {

                connection.Execute("create table #ResultsChange (X int);create table #ResultsChange2 (Y int);insert #ResultsChange (X) values(1);insert #ResultsChange2 (Y) values(1);");

                var obj1 = connection.Query<ResultsChangeType>("select * from #ResultsChange").Single();
                obj1.X.IsEqualTo(1);
                obj1.Y.IsEqualTo(0);
                obj1.Z.IsEqualTo(0);

                var obj2 = connection.Query<ResultsChangeType>("select * from #ResultsChange rc inner join #ResultsChange2 rc2 on rc2.Y=rc.X").Single();
                obj2.X.IsEqualTo(1);
                obj2.Y.IsEqualTo(1);
                obj2.Z.IsEqualTo(0);

                connection.Execute("alter table #ResultsChange add Z int null");
                connection.Execute("update #ResultsChange set Z = 2");

                var obj3 = connection.Query<ResultsChangeType>("select * from #ResultsChange").Single();
                obj3.X.IsEqualTo(1);
                obj3.Y.IsEqualTo(0);
                obj3.Z.IsEqualTo(2);

                var obj4 = connection.Query<ResultsChangeType>("select * from #ResultsChange rc inner join #ResultsChange2 rc2 on rc2.Y=rc.X").Single();
                obj4.X.IsEqualTo(1);
                obj4.Y.IsEqualTo(1);
                obj4.Z.IsEqualTo(2);
            }
            finally
            {
                connection.Execute("drop table #ResultsChange;drop table #ResultsChange2;");
            }
        }
        class ResultsChangeType
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
        }

        public void TestCustomTypeMap()
        {
            // default mapping
            var item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            item.A.IsEqualTo("AVal");
            item.B.IsEqualTo("BVal");

            // custom mapping
            var map = new CustomPropertyTypeMap(typeof(TypeWithMapping),
                (type, columnName) => type.GetProperties().Where(prop => prop.GetCustomAttributes(false).OfType<DescriptionAttribute>().Any(attr => attr.Description == columnName)).FirstOrDefault());
            Dapper.SqlMapper.SetTypeMap(typeof(TypeWithMapping), map);

            item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            item.A.IsEqualTo("BVal");
            item.B.IsEqualTo("AVal");

            // reset to default
            Dapper.SqlMapper.SetTypeMap(typeof(TypeWithMapping), null);
            item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            item.A.IsEqualTo("AVal");
            item.B.IsEqualTo("BVal");
        }

        public class TypeWithMapping
        {
            [Description("B")]
            public string A { get; set; }

            [Description("A")]
            public string B { get; set; }
        }

        public class WrongTypes
        {
            public int A { get; set; }
            public double B { get; set; }
            public long C { get; set; }
            public bool D { get; set; }
        }

        public void TestWrongTypes_WithRightTypes()
        {
            var item = connection.Query<WrongTypes>("select 1 as A, cast(2.0 as float) as B, cast(3 as bigint) as C, cast(1 as bit) as D").Single();
            item.A.Equals(1);
            item.B.Equals(2.0);
            item.C.Equals(3L);
            item.D.Equals(true);
        }

        public void TestWrongTypes_WithWrongTypes()
        {
            var item = connection.Query<WrongTypes>("select cast(1.0 as float) as A, 2 as B, 3 as C, cast(1 as bigint) as D").Single();
            item.A.Equals(1);
            item.B.Equals(2.0);
            item.C.Equals(3L);
            item.D.Equals(true);
        }

        public void Test_AddDynamicParametersRepeatedShouldWork()
        {
            var args = new DynamicParameters();
            args.AddDynamicParams(new { Foo = 123 });
            args.AddDynamicParams(new { Foo = 123 });
            int i = connection.Query<int>("select @Foo", args).Single();
            i.IsEqualTo(123);
        }


        public class ParameterWithIndexer
        {
            public int A { get; set; }
            public virtual string this[string columnName]
            {
                get { return null; }
                set { }
            }
        }

        public void TestParameterWithIndexer()
        {
            connection.Execute(@"create proc #TestProcWithIndexer 
	@A int
as 
begin
	select @A
end");
            var item = connection.Query<int>("#TestProcWithIndexer", new ParameterWithIndexer(), commandType: CommandType.StoredProcedure).Single();
        }

        public void Issue_40_AutomaticBoolConversion()
        {
            var user = connection.Query<Issue40_User>("select UserId=1,Email='abc',Password='changeme',Active=cast(1 as tinyint)").Single();
            user.Active.IsTrue();
            user.UserID.IsEqualTo(1);
            user.Email.IsEqualTo("abc");
            user.Password.IsEqualTo("changeme");
        }

        public class Issue40_User
        {
            public Issue40_User()
            {
                Email = Password = String.Empty;
            }
            public int UserID { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public bool Active { get; set; }
        }

        SqlConnection GetClosedConnection()
        {
            var conn = new SqlConnection(connection.ConnectionString);
            if (conn.State != ConnectionState.Closed) throw new InvalidOperationException("should be closed!");
            return conn;
        }
        public void ExecuteFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                conn.Execute("-- nop");
                conn.State.IsEqualTo(ConnectionState.Closed);
            }
        }
        class Multi1
        {
            public int Id { get; set; }
        }
        class Multi2
        {
            public int Id { get; set; }
        }
        public void QueryMultimapFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                conn.State.IsEqualTo(ConnectionState.Closed);
                var i = conn.Query<Multi1, Multi2, int>("select 2 as [Id], 3 as [Id]", (x, y) => x.Id + y.Id).Single();
                conn.State.IsEqualTo(ConnectionState.Closed);
                i.IsEqualTo(5);
            }
        }
        public void QueryMultiple2FromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                conn.State.IsEqualTo(ConnectionState.Closed);
                using (var multi = conn.QueryMultiple("select 1 select 2 select 3"))
                {
                    multi.Read<int>().Single().IsEqualTo(1);
                    multi.Read<int>().Single().IsEqualTo(2);
                    // not reading 3 is intentional here
                }
                conn.State.IsEqualTo(ConnectionState.Closed);
            }
        }
        public void ExecuteInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                try
                {
                    conn.Execute("nop");
                    false.IsEqualTo(true); // shouldn't have got here
                }
                catch
                {
                    conn.State.IsEqualTo(ConnectionState.Closed);
                }
            }
        }
        public void QueryFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                var i = conn.Query<int>("select 1").Single();
                conn.State.IsEqualTo(ConnectionState.Closed);
                i.IsEqualTo(1);
            }
        }
        public void QueryInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                try
                {
                    conn.Query<int>("select gibberish").Single();
                    false.IsEqualTo(true); // shouldn't have got here
                }
                catch
                {
                    conn.State.IsEqualTo(ConnectionState.Closed);
                }
            }
        }
        public void QueryMultipleFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                using (var multi = conn.QueryMultiple("select 1; select 'abc';"))
                {
                    multi.Read<int>().Single().IsEqualTo(1);
                    multi.Read<string>().Single().IsEqualTo("abc");
                }
                conn.State.IsEqualTo(ConnectionState.Closed);
            }
        }
        public void QueryMultipleInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                try
                {
                    conn.QueryMultiple("select gibberish");
                    false.IsEqualTo(true); // shouldn't have got here
                }
                catch
                {
                    conn.State.IsEqualTo(ConnectionState.Closed);
                }
            }
        }

        public void TestMultiSelectWithSomeEmptyGrids()
        {
            using (var reader = connection.QueryMultiple("select 1; select 2 where 1 = 0; select 3 where 1 = 0; select 4;"))
            {
                var one = reader.Read<int>().ToArray();
                var two = reader.Read<int>().ToArray();
                var three = reader.Read<int>().ToArray();
                var four = reader.Read<int>().ToArray();
                try
                { // only returned four grids; expect a fifth read to fail
                    reader.Read<int>();
                    throw new InvalidOperationException("this should not have worked!");
                }
                catch (ObjectDisposedException ex)
                { // expected; success
                    ex.Message.IsEqualTo("The reader has been disposed; this can happen after all data has been consumed\r\nObject name: 'Dapper.SqlMapper+GridReader'.");
                }

                one.Length.IsEqualTo(1);
                one[0].IsEqualTo(1);
                two.Length.IsEqualTo(0);
                three.Length.IsEqualTo(0);
                four.Length.IsEqualTo(1);
                four[0].IsEqualTo(4);
            }
        }

        public void TestDynamicMutation()
        {
            var obj = connection.Query("select 1 as [a], 2 as [b], 3 as [c]").Single();
            ((int)obj.a).IsEqualTo(1);
            IDictionary<string, object> dict = obj;
            Assert.Equals(3, dict.Count);
            Assert.IsTrue(dict.Remove("a"));
            Assert.IsFalse(dict.Remove("d"));
            Assert.Equals(2, dict.Count);
            dict.Add("d", 4);
            Assert.Equals(3, dict.Count);
            Assert.Equals("b,c,d", string.Join(",", dict.Keys.OrderBy(x => x)));
            Assert.Equals("2,3,4", string.Join(",", dict.OrderBy(x => x.Key).Select(x => x.Value)));

            Assert.Equals(2, (int)obj.b);
            Assert.Equals(3, (int)obj.c);
            Assert.Equals(4, (int)obj.d);
            try
            {
                ((int)obj.a).IsEqualTo(1);
                throw new InvalidOperationException("should have thrown");
            }
            catch (RuntimeBinderException)
            {
                // pass
            }
        }

        public void TestIssue131()
        {
            var results = connection.Query<dynamic, int, dynamic>(
                "SELECT 1 Id, 'Mr' Title, 'John' Surname, 4 AddressCount",
                (person, addressCount) =>
                {
                    return person;
                },
                splitOn: "AddressCount"
            ).FirstOrDefault();

            var asDict = (IDictionary<string, object>)results;

            asDict.ContainsKey("Id").IsEqualTo(true);
            asDict.ContainsKey("Title").IsEqualTo(true);
            asDict.ContainsKey("Surname").IsEqualTo(true);
            asDict.ContainsKey("AddressCount").IsEqualTo(false);
        }
        // see http://stackoverflow.com/questions/16955357/issue-about-dapper
        public void TestSplitWithMissingMembers()
        {
            var result = connection.Query<Topic, Profile, Topic>(
            @"select 123 as ID, 'abc' as Title,
                     cast('01 Feb 2013' as datetime) as CreateDate,
                     'ghi' as Name, 'def' as Phone",
            (T, P) => { T.Author = P; return T; },
            null, null, true, "ID,Name").Single();

            result.ID.Equals(123);
            result.Title.Equals("abc");
            result.CreateDate.Equals(new DateTime(2013, 2, 1));
            result.Name.IsNull();
            result.Content.IsNull();

            result.Author.Phone.Equals("def");
            result.Author.Name.Equals("ghi");
            result.Author.ID.Equals(0);
            result.Author.Address.IsNull();
        }
        public class Profile
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            //public ExtraInfo Extra { get; set; }
        }

        public class Topic
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public DateTime CreateDate { get; set; }
            public string Content { get; set; }
            public int UID { get; set; }
            public int TestColum { get; set; }
            public string Name { get; set; }
            public Profile Author { get; set; }
            //public Attachment Attach { get; set; }
        }

        // see http://stackoverflow.com/questions/13127886/dapper-returns-null-for-singleordefaultdatediff
        public void TestNullFromInt_NoRows()
        {
            var result = connection.Query<int>( // case with rows
             "select DATEDIFF(day, GETUTCDATE(), @date)", new { date = DateTime.UtcNow.AddDays(20) })
             .SingleOrDefault();
            result.IsEqualTo(20);

            result = connection.Query<int>( // case without rows
                "select DATEDIFF(day, GETUTCDATE(), @date) where 1 = 0", new { date = DateTime.UtcNow.AddDays(20) })
                .SingleOrDefault();
            result.IsEqualTo(0); // zero rows; default of int over zero rows is zero


        }

        class TransactedConnection : IDbConnection
        {
            IDbConnection _conn;
            IDbTransaction _tran;

            public TransactedConnection(IDbConnection conn, IDbTransaction tran)
            {
                _conn = conn;
                _tran = tran;
            }

            public string ConnectionString { get { return _conn.ConnectionString; } set { _conn.ConnectionString = value; } }
            public int ConnectionTimeout { get { return _conn.ConnectionTimeout; } }
            public string Database { get { return _conn.Database; } }
            public ConnectionState State { get { return _conn.State; } }

            public IDbTransaction BeginTransaction(IsolationLevel il)
            {
                throw new NotImplementedException();
            }

            public IDbTransaction BeginTransaction()
            {
                return _tran;
            }

            public void ChangeDatabase(string databaseName)
            {
                _conn.ChangeDatabase(databaseName);
            }

            public void Close()
            {
                _conn.Close();
            }

            public IDbCommand CreateCommand()
            {
                // The command inherits the "current" transaction.
                var command = _conn.CreateCommand();
                command.Transaction = _tran;
                return command;
            }

            public void Dispose()
            {
                _conn.Dispose();
            }

            public void Open()
            {
                _conn.Open();
            }
        }

        public void TestDapperTableMetadataRetrieval()
        {
            // Test for a bug found in CS 51509960 where the following sequence would result in an InvalidOperationException being
            // thrown due to an attempt to access a disposed of DataReader:
            //
            // - Perform a dynamic query that yields no results
            // - Add data to the source of that query
            // - Perform a the same query again
            connection.Execute("CREATE TABLE #sut (value varchar(10) NOT NULL PRIMARY KEY)");
            connection.Query("SELECT value FROM #sut").IsSequenceEqualTo(Enumerable.Empty<dynamic>());

            connection.Execute("INSERT INTO #sut (value) VALUES ('test')").IsEqualTo(1);
            var result = connection.Query("SELECT value FROM #sut");

            var first = result.First();
            ((string)first.value).IsEqualTo("test");
        }

        public void TestIssue17648290()
        {
            var p = new DynamicParameters();
            int code = 1, getMessageControlId = 2;
            p.Add("@Code", code);
            p.Add("@MessageControlID", getMessageControlId);
            p.Add("@SuccessCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@ErrorDescription", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
            connection.Execute(@"CREATE PROCEDURE #up_MessageProcessed_get
        @Code varchar(10),
        @MessageControlID varchar(22),
        @SuccessCode int OUTPUT,
        @ErrorDescription varchar(255) OUTPUT
        AS

        BEGIN

        Select 2 as MessageProcessID, 38349348 as StartNum, 3874900 as EndNum, GETDATE() as StartDate, GETDATE() as EndDate
        SET @SuccessCode = 0
        SET @ErrorDescription = 'Completed successfully'
        END");
            var result = connection.Query(sql: "#up_MessageProcessed_get", param: p, commandType: CommandType.StoredProcedure);
            var row = result.Single();
            ((int)row.MessageProcessID).IsEqualTo(2);
            ((int)row.StartNum).IsEqualTo(38349348);
            ((int)row.EndNum).IsEqualTo(3874900);
            DateTime startDate = row.StartDate, endDate = row.EndDate;
            p.Get<int>("SuccessCode").IsEqualTo(0);
            p.Get<string>("ErrorDescription").IsEqualTo("Completed successfully");
        }

        public void TestDoubleDecimalConversions_SO18228523_RightWay()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(1 as float) as A, cast(2 as float) as B, cast(3 as decimal) as C, cast(4 as decimal) as D").Single();
            row.A.Equals(1.0);
            row.B.Equals(2.0);
            row.C.Equals(3.0M);
            row.D.Equals(4.0M);
        }
        public void TestDoubleDecimalConversions_SO18228523_WrongWay()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(1 as decimal) as A, cast(2 as decimal) as B, cast(3 as float) as C, cast(4 as float) as D").Single();
            row.A.Equals(1.0);
            row.B.Equals(2.0);
            row.C.Equals(3.0M);
            row.D.Equals(4.0M);
        }
        public void TestDoubleDecimalConversions_SO18228523_Nulls()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(null as decimal) as A, cast(null as decimal) as B, cast(null as float) as C, cast(null as float) as D").Single();
            row.A.Equals(0.0);
            row.B.IsNull();
            row.C.Equals(0.0M);
            row.D.IsNull();
        }

        public void TestParameterInclusionNotSensitiveToCurrentCulture()
        {
            CultureInfo current = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

                connection.Query<int>("select @pid", new { PId = 1 }).Single();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = current;
            }
        }

        public void TestProcedureWithTimeParameter()
        {
            var p = new DynamicParameters();
            p.Add("a", TimeSpan.FromHours(10), dbType: DbType.Time);

            connection.Execute(@"CREATE PROCEDURE #TestProcWithTimeParameter
    @a TIME
    AS 
    BEGIN
    SELECT @a
    END");
            connection.Query<TimeSpan>("#TestProcWithTimeParameter", p, commandType: CommandType.StoredProcedure).First().IsEqualTo(new TimeSpan(10, 0, 0));
        }

        class HasDoubleDecimal
        {
            public double A { get; set; }
            public double? B { get; set; }
            public decimal C { get; set; }
            public decimal? D { get; set; }
        }

#if POSTGRESQL

        class Cat
        {
            public int Id { get; set; }
            public string Breed { get; set; }
            public string Name { get; set; }
        }

        Cat[] Cats = {
                                new Cat() { Breed = "Abyssinian", Name="KACTUS"},
                                new Cat() { Breed = "Aegean cat", Name="KADAFFI"},
                                new Cat() { Breed = "American Bobtail", Name="KANJI"},
                                new Cat() { Breed = "Balinese", Name="MACARONI"},
                                new Cat() { Breed = "Bombay", Name="MACAULAY"},
                                new Cat() { Breed = "Burmese", Name="MACBETH"},
                                new Cat() { Breed = "Chartreux", Name="MACGYVER"},
                                new Cat() { Breed = "German Rex", Name="MACKENZIE"},
                                new Cat() { Breed = "Javanese", Name="MADISON"},
                                new Cat() { Breed = "Persian", Name="MAGNA"}
                            };

        public void TestPostresqlArrayParameters()
        {
            using (var conn = new NpgsqlConnection("Server=localhost;Port=5432;User Id=dappertest;Password=dapperpass;Database=dappertest;Encoding=UNICODE"))
            {
                conn.Open();
                IDbTransaction transaction = conn.BeginTransaction();
                conn.Execute("create table tcat ( id serial not null, breed character varying(20) not null, name character varying (20) not null);");
                conn.Execute("insert tcat(breed, name) values(:breed, :name) ", Cats);

                var r = conn.Query<Cat>("select * from tcat where id=any(:catids)", new { catids = new[] { 1, 3, 5 } });
                r.Count().IsEqualTo(3);
                r.Count(c => c.Id == 1).IsEqualTo(1);
                r.Count(c => c.Id == 3).IsEqualTo(1);
                r.Count(c => c.Id == 5).IsEqualTo(1);
                transaction.Rollback();
            }
        }
#endif
    }
}
