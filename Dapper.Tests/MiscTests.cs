using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Xunit;

#if NETCOREAPP1_0
using System.Collections;
using System.Dynamic;
using System.Data.SqlTypes;
#else // net452
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#endif

#if NETCOREAPP1_0
namespace System
{
    public enum GenericUriParserOptions
    {
        Default
    }

    public class GenericUriParser
    {
        private readonly GenericUriParserOptions options;

        public GenericUriParser(GenericUriParserOptions options)
        {
            this.options = options;
        }
    }
}
#endif

namespace Dapper.Tests
{
    public class MiscTests : TestBase
    {
        [Fact]
        public void TestNullableGuidSupport()
        {
            var guid = connection.Query<Guid?>("select null").First();
            guid.IsNull();

            guid = Guid.NewGuid();
            var guid2 = connection.Query<Guid?>("select @guid", new { guid }).First();
            guid.IsEqualTo(guid2);
        }

        [Fact]
        public void TestNonNullableGuidSupport()
        {
            var guid = Guid.NewGuid();
            var guid2 = connection.Query<Guid?>("select @guid", new { guid }).First();
            Assert.IsTrue(guid == guid2);
        }

        private struct Car
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

        private struct CarWithAllProps
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public Car.TrapEnum Trap { get; set; }
        }

        [Fact]
        public void TestStructs()
        {
            var car = connection.Query<Car>("select 'Ford' Name, 21 Age, 2 Trap").First();

            car.Age.IsEqualTo(21);
            car.Name.IsEqualTo("Ford");
            ((int)car.Trap).IsEqualTo(2);
        }

        [Fact]
        public void TestStructAsParam()
        {
            var car1 = new CarWithAllProps { Name = "Ford", Age = 21, Trap = Car.TrapEnum.B };
            // note Car has Name as a field; parameters only respect properties at the moment
            var car2 = connection.Query<CarWithAllProps>("select @Name Name, @Age Age, @Trap Trap", car1).First();

            car2.Name.IsEqualTo(car1.Name);
            car2.Age.IsEqualTo(car1.Age);
            car2.Trap.IsEqualTo(car1.Trap);
        }

        [Fact]
        public void SelectListInt()
        {
            connection.Query<int>("select 1 union all select 2 union all select 3")
              .IsSequenceEqualTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void SelectBinary()
        {
            connection.Query<byte[]>("select cast(1 as varbinary(4))").First().SequenceEqual(new byte[] { 1 });
        }

        [Fact]
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

        [Fact]
        public void TestSchemaChangedViaFirstOrDefault()
        {
            connection.Execute("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')");
            try
            {
                var d = connection.QueryFirstOrDefault<Dog>("select * from #dog");
                d.Name.IsEqualTo("Alf");
                d.Age.IsEqualTo(1);
                connection.Execute("alter table #dog drop column Name");
                d = connection.QueryFirstOrDefault<Dog>("select * from #dog");
                d.Name.IsNull();
                d.Age.IsEqualTo(1);
            }
            finally
            {
                connection.Execute("drop table #dog");
            }
        }

        [Fact]
        public void Test_Single_First_Default()
        {
            var sql = "select 0 where 1 = 0;"; // no rows
            try { connection.QueryFirst<int>(sql); Assert.Fail("QueryFirst, 0"); } catch (InvalidOperationException ex) { ex.Message.IsEqualTo("Sequence contains no elements"); }
            try { connection.QuerySingle<int>(sql); Assert.Fail("QuerySingle, 0"); } catch (InvalidOperationException ex) { ex.Message.IsEqualTo("Sequence contains no elements"); }
            connection.QueryFirstOrDefault<int>(sql).IsEqualTo(0);
            connection.QuerySingleOrDefault<int>(sql).IsEqualTo(0);

            sql = "select 1;"; // one row
            connection.QueryFirst<int>(sql).IsEqualTo(1);
            connection.QuerySingle<int>(sql).IsEqualTo(1);
            connection.QueryFirstOrDefault<int>(sql).IsEqualTo(1);
            connection.QuerySingleOrDefault<int>(sql).IsEqualTo(1);

            sql = "select 2 union select 3 order by 1;"; // two rows
            connection.QueryFirst<int>(sql).IsEqualTo(2);
            try { connection.QuerySingle<int>(sql); Assert.Fail("QuerySingle, 2"); } catch (InvalidOperationException ex) { ex.Message.IsEqualTo("Sequence contains more than one element"); }
            connection.QueryFirstOrDefault<int>(sql).IsEqualTo(2);
            try { connection.QuerySingleOrDefault<int>(sql); Assert.Fail("QuerySingleOrDefault, 2"); } catch (InvalidOperationException ex) { ex.Message.IsEqualTo("Sequence contains more than one element"); }
        }

        [Fact]
        public void TestStrings()
        {
            connection.Query<string>(@"select 'a' a union select 'b'")
                .IsSequenceEqualTo(new[] { "a", "b" });
        }

        // see https://stackoverflow.com/questions/16726709/string-format-with-sql-wildcard-causing-dapper-query-to-break
        [Fact]
        public void CheckComplexConcat()
        {
            const string end_wildcard = @"
SELECT * FROM #users16726709
WHERE (first_name LIKE CONCAT(@search_term, '%') OR last_name LIKE CONCAT(@search_term, '%'));";

            const string both_wildcards = @"
SELECT * FROM #users16726709
WHERE (first_name LIKE CONCAT('%', @search_term, '%') OR last_name LIKE CONCAT('%', @search_term, '%'));";

            const string formatted = @"
SELECT * FROM #users16726709
WHERE (first_name LIKE {0} OR last_name LIKE {0});";

            const string use_end_only = @"CONCAT(@search_term, '%')";
            const string use_both = @"CONCAT('%', @search_term, '%')";

            // if true, slower query due to not being able to use indices, but will allow searching inside strings 
            const bool allow_start_wildcards = false;

            string query = string.Format(formatted, allow_start_wildcards ? use_both : use_end_only);
            const string term = "F"; // the term the user searched for

            connection.Execute(@"create table #users16726709 (first_name varchar(200), last_name varchar(200))
insert #users16726709 values ('Fred','Bloggs') insert #users16726709 values ('Tony','Farcus') insert #users16726709 values ('Albert','TenoF')");

            // Using Dapper
            connection.Query(end_wildcard, new { search_term = term }).Count().IsEqualTo(2);
            connection.Query(both_wildcards, new { search_term = term }).Count().IsEqualTo(3);
            connection.Query(query, new { search_term = term }).Count().IsEqualTo(2);
        }

        [Fact]
        public void TestExtraFields()
        {
            var guid = Guid.NewGuid();
            var dog = connection.Query<Dog>("select '' as Extra, 1 as Age, 0.1 as Name1 , Id = @id", new { id = guid });

            dog.Count().IsEqualTo(1);
            dog.First().Age.IsEqualTo(1);
            dog.First().Id.IsEqualTo(guid);
        }

        [Fact]
        public void TestStrongType()
        {
            var guid = Guid.NewGuid();
            var dog = connection.Query<Dog>("select Age = @Age, Id = @Id", new { Age = (int?)null, Id = guid });

            dog.Count().IsEqualTo(1);
            dog.First().Age.IsNull();
            dog.First().Id.IsEqualTo(guid);
        }

        [Fact]
        public void TestSimpleNull()
        {
            connection.Query<DateTime?>("select null").First().IsNull();
        }

        [Fact]
        public void TestExpando()
        {
            var rows = connection.Query("select 1 A, 2 B union all select 3, 4").ToList();

            ((int)rows[0].A).IsEqualTo(1);
            ((int)rows[0].B).IsEqualTo(2);
            ((int)rows[1].A).IsEqualTo(3);
            ((int)rows[1].B).IsEqualTo(4);
        }

        [Fact]
        public void TestStringList()
        {
            connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new { strings = new[] { "a", "b", "c" } })
                .IsSequenceEqualTo(new[] { "a", "b", "c" });

            connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new { strings = new string[0] })
                   .IsSequenceEqualTo(new string[0]);
        }

        [Fact]
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

        [Fact]
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

        private class Student
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Fact]
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

        [Fact]
        public void TestExecuteMultipleCommandObjectArray()
        {
            connection.Execute("create table #t(i int)");
            int tally = connection.Execute(@"insert #t (i) values(@a)", new object[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } });
            int sum = connection.Query<int>("select sum(i) from #t drop table #t").First();
            tally.IsEqualTo(4);
            sum.IsEqualTo(10);
        }

        private class TestObj
        {
            public int _internal;
            internal int Internal
            {
                set { _internal = value; }
            }

            public int _priv;
            private int Priv
            {
                set { _priv = value; }
            }

            private int PrivGet => _priv;
        }

        [Fact]
        public void TestSetInternal()
        {
            connection.Query<TestObj>("select 10 as [Internal]").First()._internal.IsEqualTo(10);
        }

        [Fact]
        public void TestSetPrivate()
        {
            connection.Query<TestObj>("select 10 as [Priv]").First()._priv.IsEqualTo(10);
        }

        [Fact]
        public void TestExpandWithNullableFields()
        {
            var row = connection.Query("select null A, 2 B").Single();
            ((int?)row.A).IsNull();
            ((int?)row.B).IsEqualTo(2);
        }

        [Fact]
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
            {
            }

            // should not exception, since enumerated
            en = connection.Query<int>("select 1 as one", buffered: false);

            gotException.IsTrue();
        }

        [Fact]
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
            {
            }

            // should not exception, since enumertated
            en = connection.Query("select 1 as one", buffered: false);

            gotException.IsTrue();
        }

        [Fact]
        public void TestNakedBigInt()
        {
            const long foo = 12345;
            var result = connection.Query<long>("select @foo", new { foo }).Single();
            foo.IsEqualTo(result);
        }

        [Fact]
        public void TestBigIntMember()
        {
            const long foo = 12345;
            var result = connection.Query<WithBigInt>(@"
declare @bar table(Value bigint)
insert @bar values (@foo)
select * from @bar", new { foo }).Single();
            result.Value.IsEqualTo(foo);
        }

        private class WithBigInt
        {
            public long Value { get; set; }
        }

        [Fact]
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
#pragma warning disable IDE1006 // Naming Styles
            public int a { get; set; }
            private int b { get; set; }
            public int GetB() { return b; }
            public int c = 0;
#pragma warning disable RCS1169 // Mark field as read-only.
            private int d = 0;
#pragma warning restore RCS1169 // Mark field as read-only.
            public int GetD() { return d; }
            public int e { get; set; }
            private string f
            {
                get { return e.ToString(); }
                set { e = int.Parse(value); }
            }
#pragma warning restore IDE1006 // Naming Styles
        }

        private class InheritanceTest1
        {
            public string Base1 { get; set; }
            public string Base2 { get; private set; }
        }

        private class InheritanceTest2 : InheritanceTest1
        {
            public string Derived1 { get; set; }
            public string Derived2 { get; private set; }
        }

        [Fact]
        public void TestInheritance()
        {
            // Test that inheritance works.
            var list = connection.Query<InheritanceTest2>("select 'One' as Derived1, 'Two' as Derived2, 'Three' as Base1, 'Four' as Base2");
            list.First().Derived1.IsEqualTo("One");
            list.First().Derived2.IsEqualTo("Two");
            list.First().Base1.IsEqualTo("Three");
            list.First().Base2.IsEqualTo("Four");
        }

#if !NETCOREAPP1_0
        [Fact]
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
#endif

        [Fact]
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

        [Fact]
        public void TestDefaultDbStringDbType()
        {
            var origDefaultStringDbType = DbString.IsAnsiDefault;
            try
            {
                DbString.IsAnsiDefault = true;
                var a = new DbString { Value = "abcde" };
                var b = new DbString { Value = "abcde", IsAnsi = false };
                a.IsAnsi.IsTrue();
                b.IsAnsi.IsFalse();
            }
            finally
            {
                DbString.IsAnsiDefault = origDefaultStringDbType;
            }
        }

        [Fact]
        public void TestFastExpandoSupportsIDictionary()
        {
            var row = connection.Query("select 1 A, 'two' B").First() as IDictionary<string, object>;
            row["A"].IsEqualTo(1);
            row["B"].IsEqualTo("two");
        }

        [Fact]
        public void TestDapperSetsPrivates()
        {
            connection.Query<PrivateDan>("select 'one' ShadowInDB").First().Shadow.IsEqualTo(1);

            connection.QueryFirstOrDefault<PrivateDan>("select 'one' ShadowInDB").Shadow.IsEqualTo(1);
        }

        private class PrivateDan
        {
            public int Shadow { get; set; }
            private string ShadowInDB
            {
                set { Shadow = value == "one" ? 1 : 0; }
            }
        }

        [Fact]
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

        [Fact]
        public void TestUnexpectedButFilteredDataMessage()
        {
            int i = connection.Query<int>("select @Bar", new WithBizarreData { Foo = new GenericUriParser(GenericUriParserOptions.Default), Bar = 23 }).Single();

            i.IsEqualTo(23);
        }

        private class WithBizarreData
        {
            public GenericUriParser Foo { get; set; }
            public int Bar { get; set; }
        }

        private class WithCharValue
        {
            public char Value { get; set; }
            public char? ValueNullable { get; set; }
        }

        [Fact]
        public void TestCharInputAndOutput()
        {
            const char test = '〠';
            char c = connection.Query<char>("select @c", new { c = test }).Single();

            c.IsEqualTo(test);

            var obj = connection.Query<WithCharValue>("select @Value as Value", new WithCharValue { Value = c }).Single();

            obj.Value.IsEqualTo(test);
        }

        [Fact]
        public void TestNullableCharInputAndOutputNonNull()
        {
            char? test = '〠';
            char? c = connection.Query<char?>("select @c", new { c = test }).Single();

            c.IsEqualTo(test);

            var obj = connection.Query<WithCharValue>("select @ValueNullable as ValueNullable", new WithCharValue { ValueNullable = c }).Single();

            obj.ValueNullable.IsEqualTo(test);
        }

        [Fact]
        public void TestNullableCharInputAndOutputNull()
        {
            char? test = null;
            char? c = connection.Query<char?>("select @c", new { c = test }).Single();

            c.IsEqualTo(test);

            var obj = connection.Query<WithCharValue>("select @ValueNullable as ValueNullable", new WithCharValue { ValueNullable = c }).Single();

            obj.ValueNullable.IsEqualTo(test);
        }

        [Fact]
        public void WorkDespiteHavingWrongStructColumnTypes()
        {
            var hazInt = connection.Query<CanHazInt>("select cast(1 as bigint) Value").Single();
            hazInt.Value.Equals(1);
        }

        private struct CanHazInt
        {
            public int Value { get; set; }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
                Email = Password = string.Empty;
            }

            public int UserID { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public bool Active { get; set; }
        }

        [Fact]
        public void ExecuteFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                conn.Execute("-- nop");
                conn.State.IsEqualTo(ConnectionState.Closed);
            }
        }

        [Fact]
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

        [Fact]
        public void QueryFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                var i = conn.Query<int>("select 1").Single();
                conn.State.IsEqualTo(ConnectionState.Closed);
                i.IsEqualTo(1);
            }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TestIssue131()
        {
            var results = connection.Query<dynamic, int, dynamic>(
                "SELECT 1 Id, 'Mr' Title, 'John' Surname, 4 AddressCount",
                (person, addressCount) => person,
                splitOn: "AddressCount"
            ).FirstOrDefault();

            var asDict = (IDictionary<string, object>)results;

            asDict.ContainsKey("Id").IsEqualTo(true);
            asDict.ContainsKey("Title").IsEqualTo(true);
            asDict.ContainsKey("Surname").IsEqualTo(true);
            asDict.ContainsKey("AddressCount").IsEqualTo(false);
        }

        // see https://stackoverflow.com/questions/13127886/dapper-returns-null-for-singleordefaultdatediff
        [Fact]
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

        [Fact]
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

        [Fact]
        public void DbStringAnsi()
        {
            var a = connection.Query<int>("select datalength(@x)",
                new { x = new DbString { Value = "abc", IsAnsi = true } }).Single();
            var b = connection.Query<int>("select datalength(@x)",
                new { x = new DbString { Value = "abc", IsAnsi = false } }).Single();
            a.IsEqualTo(3);
            b.IsEqualTo(6);
        }

        private class HasInt32
        {
            public int Value { get; set; }
        }

        // https://stackoverflow.com/q/23696254/23354
        [Fact]
        public void DownwardIntegerConversion()
        {
            const string sql = "select cast(42 as bigint) as Value";
            int i = connection.Query<HasInt32>(sql).Single().Value;
            Assert.IsEqualTo(42, i);

            i = connection.Query<int>(sql).Single();
            Assert.IsEqualTo(42, i);
        }

        [Fact]
        public void TypeBasedViaDynamic()
        {
            Type type = Common.GetSomeType();

            dynamic template = Activator.CreateInstance(type);
            dynamic actual = CheetViaDynamic(template, "select @A as [A], @B as [B]", new { A = 123, B = "abc" });
            ((object)actual).GetType().IsEqualTo(type);
            int a = actual.A;
            string b = actual.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");
        }

        [Fact]
        public void TypeBasedViaType()
        {
            Type type = Common.GetSomeType();

            dynamic actual = connection.Query(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).FirstOrDefault();
            ((object)actual).GetType().IsEqualTo(type);
            int a = actual.A;
            string b = actual.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");
        }

        private T CheetViaDynamic<T>(T template, string query, object args)
        {
            return connection.Query<T>(query, args).SingleOrDefault();
        }

        [Fact]
        public void Issue22_ExecuteScalar()
        {
            int i = connection.ExecuteScalar<int>("select 123");
            i.IsEqualTo(123);

            i = connection.ExecuteScalar<int>("select cast(123 as bigint)");
            i.IsEqualTo(123);

            long j = connection.ExecuteScalar<long>("select 123");
            j.IsEqualTo(123L);

            j = connection.ExecuteScalar<long>("select cast(123 as bigint)");
            j.IsEqualTo(123L);

            int? k = connection.ExecuteScalar<int?>("select @i", new { i = default(int?) });
            k.IsNull();
        }

        [Fact]
        public void Issue142_FailsNamedStatus()
        {
            var row1 = connection.Query<Issue142_Status>("select @Status as [Status]", new { Status = StatusType.Started }).Single();
            row1.Status.IsEqualTo(StatusType.Started);

            var row2 = connection.Query<Issue142_StatusType>("select @Status as [Status]", new { Status = Status.Started }).Single();
            row2.Status.IsEqualTo(Status.Started);
        }

        public class Issue142_Status
        {
            public StatusType Status { get; set; }
        }

        public class Issue142_StatusType
        {
            public Status Status { get; set; }
        }

        public enum StatusType : byte
        {
            NotStarted = 1, Started = 2, Finished = 3
        }

        public enum Status : byte
        {
            NotStarted = 1, Started = 2, Finished = 3
        }

        [Fact]
        public void Issue178_SqlServer()
        {
            const string sql = @"select count(*) from Issue178";
            try { connection.Execute("drop table Issue178"); }
            catch { /* don't care */ }
            try { connection.Execute("create table Issue178(id int not null)"); }
            catch { /* don't care */ }
            // raw ADO.net
            var sqlCmd = new SqlCommand(sql, connection);
            using (IDataReader reader1 = sqlCmd.ExecuteReader())
            {
                Assert.IsTrue(reader1.Read());
                reader1.GetInt32(0).IsEqualTo(0);
                Assert.IsFalse(reader1.Read());
                Assert.IsFalse(reader1.NextResult());
            }

            // dapper
            using (var reader2 = connection.ExecuteReader(sql))
            {
                Assert.IsTrue(reader2.Read());
                reader2.GetInt32(0).IsEqualTo(0);
                Assert.IsFalse(reader2.Read());
                Assert.IsFalse(reader2.NextResult());
            }
        }

        [Fact]
        public void QueryBasicWithoutQuery()
        {
            int? i = connection.Query<int?>("print 'not a query'").FirstOrDefault();
            i.IsNull();
        }

        [Fact]
        public void QueryComplexWithoutQuery()
        {
            var obj = connection.Query<Foo1>("print 'not a query'").FirstOrDefault();
            obj.IsNull();
        }

        [FactLongRunning]
        public void Issue263_Timeout()
        {
            var watch = Stopwatch.StartNew();
            var i = connection.Query<int>("waitfor delay '00:01:00'; select 42;", commandTimeout: 300, buffered: false).Single();
            watch.Stop();
            i.IsEqualTo(42);
            var minutes = watch.ElapsedMilliseconds / 1000 / 60;
            Assert.IsTrue(minutes >= 0.95 && minutes <= 1.05);
        }

        [Fact]
        public void SO30435185_InvalidTypeOwner()
        {
            try
            {
                const string sql = @" INSERT INTO #XXX
                        (XXXId, AnotherId, ThirdId, Value, Comment)
                        VALUES
                        (@XXXId, @AnotherId, @ThirdId, @Value, @Comment); select @@rowcount as [Foo]";

                var command = new
                {
                    MyModels = new[]
                    {
                        new {XXXId = 1, AnotherId = 2, ThirdId = 3, Value = "abc", Comment = "def" }
                    }
                };
                var parameters = command
                    .MyModels
                    .Select(model => new
                    {
                        XXXId = model.XXXId,
                        AnotherId = model.AnotherId,
                        ThirdId = model.ThirdId,
                        Value = model.Value,
                        Comment = model.Comment
                    })
                    .ToArray();

                var rowcount = (int)connection.Query(sql, parameters).Single().Foo;
                rowcount.IsEqualTo(1);

                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.IsEqualTo("An enumerable sequence of parameters (arrays, lists, etc) is not allowed in this context");
            }
        }

        [Fact]
        public async void SO35470588_WrongValuePidValue()
        {
            // nuke, rebuild, and populate the table
            try { connection.Execute("drop table TPTable"); } catch { /* don't care */ }
            connection.Execute(@"
create table TPTable (Pid int not null primary key identity(1,1), Value int not null);
insert TPTable (Value) values (2), (568)");

            // fetch the data using the query in the question, then force to a dictionary
            var rows = (await connection.QueryAsync<TPTable>("select * from TPTable").ConfigureAwait(false))
                .ToDictionary(x => x.Pid);

            // check the number of rows
            rows.Count.IsEqualTo(2);

            // check row 1
            var row = rows[1];
            row.Pid.IsEqualTo(1);
            row.Value.IsEqualTo(2);

            // check row 2
            row = rows[2];
            row.Pid.IsEqualTo(2);
            row.Value.IsEqualTo(568);
        }

        public class TPTable
        {
            public int Pid { get; set; }
            public int Value { get; set; }
        }

        [Fact]
        public void GetOnlyProperties()
        {
            var obj = connection.QuerySingle<HazGetOnly>("select 42 as [Id], 'def' as [Name];");
            obj.Id.IsEqualTo(42);
            obj.Name.IsEqualTo("def");
        }

        private class HazGetOnly
        {
            public int Id { get; }
            public string Name { get; } = "abc";
        }
    }
}
