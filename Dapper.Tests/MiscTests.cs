using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    [Collection("MiscTests")]
    public sealed class SystemSqlClientMiscTests : MiscTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("MiscTests")]
    public sealed class MicrosoftSqlClientMiscTests : MiscTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class MiscTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestNullableGuidSupport()
        {
            var guid = connection.Query<Guid?>("select null").First();
            Assert.Null(guid);

            guid = Guid.NewGuid();
            var guid2 = connection.Query<Guid?>("select @guid", new { guid }).First();
            Assert.Equal(guid, guid2);
        }

        [Fact]
        public void TestNonNullableGuidSupport()
        {
            var guid = Guid.NewGuid();
            var guid2 = connection.Query<Guid?>("select @guid", new { guid }).First();
            Assert.True(guid == guid2);
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

            Assert.Equal(21, car.Age);
            Assert.Equal("Ford", car.Name);
            Assert.Equal(2, (int)car.Trap);
        }

        [Fact]
        public void TestStructAsParam()
        {
            var car1 = new CarWithAllProps { Name = "Ford", Age = 21, Trap = Car.TrapEnum.B };
            // note Car has Name as a field; parameters only respect properties at the moment
            var car2 = connection.Query<CarWithAllProps>("select @Name Name, @Age Age, @Trap Trap", car1).First();

            Assert.Equal(car2.Name, car1.Name);
            Assert.Equal(car2.Age, car1.Age);
            Assert.Equal(car2.Trap, car1.Trap);
        }

        [Fact]
        public void SelectListInt()
        {
            Assert.Equal(new[] { 1, 2, 3 }, connection.Query<int>("select 1 union all select 2 union all select 3"));
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
                Assert.Equal("Alf", d.Name);
                Assert.Equal(1, d.Age);
                connection.Execute("alter table #dog drop column Name");
                d = connection.Query<Dog>("select * from #dog").Single();
                Assert.Null(d.Name);
                Assert.Equal(1, d.Age);
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
                Assert.Equal("Alf", d.Name);
                Assert.Equal(1, d.Age);
                connection.Execute("alter table #dog drop column Name");
                d = connection.QueryFirstOrDefault<Dog>("select * from #dog");
                Assert.Null(d.Name);
                Assert.Equal(1, d.Age);
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

            var ex = Assert.Throws<InvalidOperationException>(() => connection.QueryFirst<int>(sql));
            Assert.Equal("Sequence contains no elements", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => connection.QuerySingle<int>(sql));
            Assert.Equal("Sequence contains no elements", ex.Message);

            Assert.Equal(0, connection.QueryFirstOrDefault<int>(sql));
            Assert.Equal(0, connection.QuerySingleOrDefault<int>(sql));

            sql = "select 1;"; // one row
            Assert.Equal(1, connection.QueryFirst<int>(sql));
            Assert.Equal(1, connection.QuerySingle<int>(sql));
            Assert.Equal(1, connection.QueryFirstOrDefault<int>(sql));
            Assert.Equal(1, connection.QuerySingleOrDefault<int>(sql));

            sql = "select 2 union select 3 order by 1;"; // two rows
            Assert.Equal(2, connection.QueryFirst<int>(sql));

            ex = Assert.Throws<InvalidOperationException>(() => connection.QuerySingle<int>(sql));
            Assert.Equal("Sequence contains more than one element", ex.Message);

            Assert.Equal(2, connection.QueryFirstOrDefault<int>(sql));

            ex = Assert.Throws<InvalidOperationException>(() => connection.QuerySingleOrDefault<int>(sql));
            Assert.Equal("Sequence contains more than one element", ex.Message);
        }

        [Fact]
        public void TestStrings()
        {
            Assert.Equal(new[] { "a", "b" }, connection.Query<string>("select 'a' a union select 'b'"));
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

            const string use_end_only = "CONCAT(@search_term, '%')";
            const string use_both = "CONCAT('%', @search_term, '%')";

            // if true, slower query due to not being able to use indices, but will allow searching inside strings 
            const bool allow_start_wildcards = false;

            string query = string.Format(formatted, allow_start_wildcards ? use_both : use_end_only);
            const string term = "F"; // the term the user searched for

            connection.Execute(@"create table #users16726709 (first_name varchar(200), last_name varchar(200))
insert #users16726709 values ('Fred','Bloggs') insert #users16726709 values ('Tony','Farcus') insert #users16726709 values ('Albert','TenoF')");

            // Using Dapper
            Assert.Equal(2, connection.Query(end_wildcard, new { search_term = term }).Count());
            Assert.Equal(3, connection.Query(both_wildcards, new { search_term = term }).Count());
            Assert.Equal(2, connection.Query(query, new { search_term = term }).Count());
        }

        [Fact]
        public void TestExtraFields()
        {
            var guid = Guid.NewGuid();
            var dog = connection.Query<Dog>("select '' as Extra, 1 as Age, 0.1 as Name1 , Id = @id", new { id = guid });

            Assert.Single(dog);
            Assert.Equal(1, dog.First().Age);
            Assert.Equal(dog.First().Id, guid);
        }

        [Fact]
        public void TestStrongType()
        {
            var guid = Guid.NewGuid();
            var dog = connection.Query<Dog>("select Age = @Age, Id = @Id", new { Age = (int?)null, Id = guid });

            Assert.Single(dog);
            Assert.Null(dog.First().Age);
            Assert.Equal(dog.First().Id, guid);
        }

        [Fact]
        public void TestSimpleNull()
        {
            Assert.Null(connection.Query<DateTime?>("select null").First());
        }

        [Fact]
        public void TestExpando()
        {
            var rows = connection.Query("select 1 A, 2 B union all select 3, 4").ToList();

            Assert.Equal(1, (int)rows[0].A);
            Assert.Equal(2, (int)rows[0].B);
            Assert.Equal(3, (int)rows[1].A);
            Assert.Equal(4, (int)rows[1].B);
        }

        [Fact]
        public void TestStringList()
        {
            Assert.Equal(
                new[] { "a", "b", "c" },
                connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new { strings = new[] { "a", "b", "c" } })
            );
            Assert.Equal(
                new string[0],
                connection.Query<string>("select * from (select 'a' as x union all select 'b' union all select 'c') as T where x in @strings", new { strings = new string[0] })
            );
        }

        [Fact]
        public void TestExecuteCommand()
        {
            Assert.Equal(2, connection.Execute(@"
    set nocount on 
    create table #t(i int) 
    set nocount off 
    insert #t 
    select @a a union all select @b 
    set nocount on 
    drop table #t", new { a = 1, b = 2 }));
        }

        [Fact]
        public void TestExecuteMultipleCommand()
        {
            connection.Execute("create table #t(i int)");
            try
            {
                int tally = connection.Execute("insert #t (i) values(@a)", new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } });
                int sum = connection.Query<int>("select sum(i) from #t").First();
                Assert.Equal(4, tally);
                Assert.Equal(10, sum);
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
                int tally = connection.Execute("insert #t (Name,Age) values(@Name, @Age)", new List<Student>
            {
                new Student{Age = 1, Name = "sam"},
                new Student{Age = 2, Name = "bob"}
            });
                int sum = connection.Query<int>("select sum(Age) from #t").First();
                Assert.Equal(2, tally);
                Assert.Equal(3, sum);
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
            int tally = connection.Execute("insert #t (i) values(@a)", new object[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } });
            int sum = connection.Query<int>("select sum(i) from #t drop table #t").First();
            Assert.Equal(4, tally);
            Assert.Equal(10, sum);
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
            Assert.Equal(10, connection.Query<TestObj>("select 10 as [Internal]").First()._internal);
        }

        [Fact]
        public void TestSetPrivate()
        {
            Assert.Equal(10, connection.Query<TestObj>("select 10 as [Priv]").First()._priv);
        }

        [Fact]
        public void TestExpandWithNullableFields()
        {
            var row = connection.Query("select null A, 2 B").Single();
            Assert.Null((int?)row.A);
            Assert.Equal(2, (int?)row.B);
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

            Assert.True(gotException);
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

            Assert.True(gotException);
        }

        [Fact]
        public void TestNakedBigInt()
        {
            const long foo = 12345;
            var result = connection.Query<long>("select @foo", new { foo }).Single();
            Assert.Equal(foo, result);
        }

        [Fact]
        public void TestBigIntMember()
        {
            const long foo = 12345;
            var result = connection.Query<WithBigInt>(@"
declare @bar table(Value bigint)
insert @bar values (@foo)
select * from @bar", new { foo }).Single();
            Assert.Equal(result.Value, foo);
        }

        private class WithBigInt
        {
            public long Value { get; set; }
        }

        [Fact]
        public void TestFieldsAndPrivates()
        {
            var data = connection.Query<TestFieldCaseAndPrivatesEntity>(
                "select a=1,b=2,c=3,d=4,f='5'").Single();
            Assert.Equal(1, data.a);
            Assert.Equal(2, data.GetB());
            Assert.Equal(3, data.c);
            Assert.Equal(4, data.GetD());
            Assert.Equal(5, data.e);
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
            Assert.Equal("One", list.First().Derived1);
            Assert.Equal("Two", list.First().Derived2);
            Assert.Equal("Three", list.First().Base1);
            Assert.Equal("Four", list.First().Base2);
        }

        [Fact]
        public void ExecuteReader()
        {
            var dt = new DataTable();
            dt.Load(connection.ExecuteReader("select 3 as [three], 4 as [four]"));
            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal("three", dt.Columns[0].ColumnName);
            Assert.Equal("four", dt.Columns[1].ColumnName);
            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(3, (int)dt.Rows[0][0]);
            Assert.Equal(4, (int)dt.Rows[0][1]);
        }

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
            Assert.Equal(10, (int)obj.a);
            Assert.Equal(20, (int)obj.b);
            Assert.Equal(5, (int)obj.c);
            Assert.Equal(10, (int)obj.d);
            Assert.Equal(5, (int)obj.e);
            Assert.Equal(10, (int)obj.f);
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
                Assert.True(a.IsAnsi);
                Assert.False(b.IsAnsi);
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
            Assert.Equal(1, row["A"]);
            Assert.Equal("two", row["B"]);
        }

        [Fact]
        public void TestDapperSetsPrivates()
        {
            Assert.Equal(1, connection.Query<PrivateDan>("select 'one' ShadowInDB").First().Shadow);

            Assert.Equal(1, connection.QueryFirstOrDefault<PrivateDan>("select 'one' ShadowInDB").Shadow);
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
            Assert.Equal("The member Foo of type System.GenericUriParser cannot be used as a parameter value", msg);
        }

        [Fact]
        public void TestUnexpectedButFilteredDataMessage()
        {
            int i = connection.Query<int>("select @Bar", new WithBizarreData { Foo = new GenericUriParser(GenericUriParserOptions.Default), Bar = 23 }).Single();

            Assert.Equal(23, i);
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

            Assert.Equal(c, test);

            var obj = connection.Query<WithCharValue>("select @Value as Value", new WithCharValue { Value = c }).Single();

            Assert.Equal(obj.Value, test);
        }

        [Fact]
        public void TestNullableCharInputAndOutputNonNull()
        {
            char? test = '〠';
            char? c = connection.Query<char?>("select @c", new { c = test }).Single();

            Assert.Equal(c, test);

            var obj = connection.Query<WithCharValue>("select @ValueNullable as ValueNullable", new WithCharValue { ValueNullable = c }).Single();

            Assert.Equal(obj.ValueNullable, test);
        }

        [Fact]
        public void TestNullableCharInputAndOutputNull()
        {
            char? test = null;
            char? c = connection.Query<char?>("select @c", new { c = test }).Single();

            Assert.Equal(c, test);

            var obj = connection.Query<WithCharValue>("select @ValueNullable as ValueNullable", new WithCharValue { ValueNullable = c }).Single();

            Assert.Equal(obj.ValueNullable, test);
        }

        [Fact]
        public void WorkDespiteHavingWrongStructColumnTypes()
        {
            var hazInt = connection.Query<CanHazInt>("select cast(1 as bigint) Value").Single();
            Assert.Equal(1, hazInt.Value);
        }

        private struct CanHazInt
        {
            public int Value { get; set; }
        }

        [Fact]
        public void TestInt16Usage()
        {
            Assert.Equal(connection.Query<short>("select cast(42 as smallint)").Single(), (short)42);
            Assert.Equal(connection.Query<short?>("select cast(42 as smallint)").Single(), (short?)42);
            Assert.Equal(connection.Query<short?>("select cast(null as smallint)").Single(), (short?)null);

            Assert.Equal(connection.Query<ShortEnum>("select cast(42 as smallint)").Single(), (ShortEnum)42);
            Assert.Equal(connection.Query<ShortEnum?>("select cast(42 as smallint)").Single(), (ShortEnum?)42);
            Assert.Equal(connection.Query<ShortEnum?>("select cast(null as smallint)").Single(), (ShortEnum?)null);

            var row =
                connection.Query<WithInt16Values>(
                    "select cast(1 as smallint) as NonNullableInt16, cast(2 as smallint) as NullableInt16, cast(3 as smallint) as NonNullableInt16Enum, cast(4 as smallint) as NullableInt16Enum")
                    .Single();
            Assert.Equal(row.NonNullableInt16, (short)1);
            Assert.Equal(row.NullableInt16, (short)2);
            Assert.Equal(ShortEnum.Three, row.NonNullableInt16Enum);
            Assert.Equal(ShortEnum.Four, row.NullableInt16Enum);

            row =
    connection.Query<WithInt16Values>(
        "select cast(5 as smallint) as NonNullableInt16, cast(null as smallint) as NullableInt16, cast(6 as smallint) as NonNullableInt16Enum, cast(null as smallint) as NullableInt16Enum")
        .Single();
            Assert.Equal(row.NonNullableInt16, (short)5);
            Assert.Equal(row.NullableInt16, (short?)null);
            Assert.Equal(ShortEnum.Six, row.NonNullableInt16Enum);
            Assert.Equal(row.NullableInt16Enum, (ShortEnum?)null);
        }

        [Fact]
        public void TestInt32Usage()
        {
            Assert.Equal(connection.Query<int>("select cast(42 as int)").Single(), (int)42);
            Assert.Equal(connection.Query<int?>("select cast(42 as int)").Single(), (int?)42);
            Assert.Equal(connection.Query<int?>("select cast(null as int)").Single(), (int?)null);

            Assert.Equal(connection.Query<IntEnum>("select cast(42 as int)").Single(), (IntEnum)42);
            Assert.Equal(connection.Query<IntEnum?>("select cast(42 as int)").Single(), (IntEnum?)42);
            Assert.Equal(connection.Query<IntEnum?>("select cast(null as int)").Single(), (IntEnum?)null);

            var row =
                connection.Query<WithInt32Values>(
                    "select cast(1 as int) as NonNullableInt32, cast(2 as int) as NullableInt32, cast(3 as int) as NonNullableInt32Enum, cast(4 as int) as NullableInt32Enum")
                    .Single();
            Assert.Equal(row.NonNullableInt32, (int)1);
            Assert.Equal(row.NullableInt32, (int)2);
            Assert.Equal(IntEnum.Three, row.NonNullableInt32Enum);
            Assert.Equal(IntEnum.Four, row.NullableInt32Enum);

            row =
    connection.Query<WithInt32Values>(
        "select cast(5 as int) as NonNullableInt32, cast(null as int) as NullableInt32, cast(6 as int) as NonNullableInt32Enum, cast(null as int) as NullableInt32Enum")
        .Single();
            Assert.Equal(row.NonNullableInt32, (int)5);
            Assert.Equal(row.NullableInt32, (int?)null);
            Assert.Equal(IntEnum.Six, row.NonNullableInt32Enum);
            Assert.Equal(row.NullableInt32Enum, (IntEnum?)null);
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
            Assert.True(user.Active);
            Assert.Equal(1, user.UserID);
            Assert.Equal("abc", user.Email);
            Assert.Equal("changeme", user.Password);
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
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
        }

        [Fact]
        public void ExecuteInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                var ex = Assert.ThrowsAny<Exception>(() => conn.Execute("nop"));
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
        }

        [Fact]
        public void QueryFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                var i = conn.Query<int>("select 1").Single();
                Assert.Equal(ConnectionState.Closed, conn.State);
                Assert.Equal(1, i);
            }
        }

        [Fact]
        public void QueryInvalidFromClosed()
        {
            using (var conn = GetClosedConnection())
            {
                Assert.ThrowsAny<Exception>(() => conn.Query<int>("select gibberish").Single());
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
        }

        [Fact]
        public void TestDynamicMutation()
        {
            var obj = connection.Query("select 1 as [a], 2 as [b], 3 as [c]").Single();
            Assert.Equal(1, (int)obj.a);
            IDictionary<string, object> dict = obj;
            Assert.Equal(3, dict.Count);
            Assert.True(dict.Remove("a"));
            Assert.False(dict.Remove("d"));
            Assert.Equal(2, dict.Count);
            dict.Add("d", 4);
            Assert.Equal(3, dict.Count);
            Assert.Equal("b,c,d", string.Join(",", dict.Keys.OrderBy(x => x)));
            Assert.Equal("2,3,4", string.Join(",", dict.OrderBy(x => x.Key).Select(x => x.Value)));

            Assert.Equal(2, (int)obj.b);
            Assert.Equal(3, (int)obj.c);
            Assert.Equal(4, (int)obj.d);
            try
            {
                Assert.Equal(1, (int)obj.a);
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

            Assert.True(asDict.ContainsKey("Id"));
            Assert.True(asDict.ContainsKey("Title"));
            Assert.True(asDict.ContainsKey("Surname"));
            Assert.False(asDict.ContainsKey("AddressCount"));
        }

        // see https://stackoverflow.com/questions/13127886/dapper-returns-null-for-singleordefaultdatediff
        [Fact]
        public void TestNullFromInt_NoRows()
        {
            var result = connection.Query<int>( // case with rows
             "select DATEDIFF(day, GETUTCDATE(), @date)", new { date = DateTime.UtcNow.AddDays(20) })
             .SingleOrDefault();
            Assert.Equal(20, result);

            result = connection.Query<int>( // case without rows
                "select DATEDIFF(day, GETUTCDATE(), @date) where 1 = 0", new { date = DateTime.UtcNow.AddDays(20) })
                .SingleOrDefault();
            Assert.Equal(0, result); // zero rows; default of int over zero rows is zero
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
            Assert.Equal(Enumerable.Empty<dynamic>(), connection.Query("SELECT value FROM #sut"));

            Assert.Equal(1, connection.Execute("INSERT INTO #sut (value) VALUES ('test')"));
            var result = connection.Query("SELECT value FROM #sut");

            var first = result.First();
            Assert.Equal("test", (string)first.value);
        }

        [Fact]
        public void DbStringAnsi()
        {
            var a = connection.Query<int>("select datalength(@x)",
                new { x = new DbString { Value = "abc", IsAnsi = true } }).Single();
            var b = connection.Query<int>("select datalength(@x)",
                new { x = new DbString { Value = "abc", IsAnsi = false } }).Single();
            Assert.Equal(3, a);
            Assert.Equal(6, b);
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
            Assert.Equal(42, i);

            i = connection.Query<int>(sql).Single();
            Assert.Equal(42, i);
        }

        [Fact]
        public void TypeBasedViaDynamic()
        {
            Type type = Common.GetSomeType();

            dynamic template = Activator.CreateInstance(type);
            dynamic actual = CheetViaDynamic(template, "select @A as [A], @B as [B]", new { A = 123, B = "abc" });
            Assert.Equal(((object)actual).GetType(), type);
            int a = actual.A;
            string b = actual.B;
            Assert.Equal(123, a);
            Assert.Equal("abc", b);
        }

        [Fact]
        public void TypeBasedViaType()
        {
            Type type = Common.GetSomeType();

            dynamic actual = connection.Query(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).FirstOrDefault();
            Assert.Equal(((object)actual).GetType(), type);
            int a = actual.A;
            string b = actual.B;
            Assert.Equal(123, a);
            Assert.Equal("abc", b);
        }

        private T CheetViaDynamic<T>(T template, string query, object args)
        {
            return connection.Query<T>(query, args).SingleOrDefault();
        }

        [Fact]
        public void Issue22_ExecuteScalar()
        {
            int i = connection.ExecuteScalar<int>("select 123");
            Assert.Equal(123, i);

            i = connection.ExecuteScalar<int>("select cast(123 as bigint)");
            Assert.Equal(123, i);

            long j = connection.ExecuteScalar<long>("select 123");
            Assert.Equal(123L, j);

            j = connection.ExecuteScalar<long>("select cast(123 as bigint)");
            Assert.Equal(123L, j);

            int? k = connection.ExecuteScalar<int?>("select @i", new { i = default(int?) });
            Assert.Null(k);
        }

        [Fact]
        public void Issue142_FailsNamedStatus()
        {
            var row1 = connection.Query<Issue142_Status>("select @Status as [Status]", new { Status = StatusType.Started }).Single();
            Assert.Equal(StatusType.Started, row1.Status);

            var row2 = connection.Query<Issue142_StatusType>("select @Status as [Status]", new { Status = Status.Started }).Single();
            Assert.Equal(Status.Started, row2.Status);
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
            const string sql = "select count(*) from Issue178";
            try { connection.Execute("drop table Issue178"); }
            catch { /* don't care */ }
            try { connection.Execute("create table Issue178(id int not null)"); }
            catch { /* don't care */ }
            // raw ADO.net
            using (var sqlCmd = connection.CreateCommand())
            {
                sqlCmd.CommandText = sql;
                using (IDataReader reader1 = sqlCmd.ExecuteReader())
                {
                    Assert.True(reader1.Read());
                    Assert.Equal(0, reader1.GetInt32(0));
                    Assert.False(reader1.Read());
                    Assert.False(reader1.NextResult());
                }
            }

            // dapper
            using (var reader2 = connection.ExecuteReader(sql))
            {
                Assert.True(reader2.Read());
                Assert.Equal(0, reader2.GetInt32(0));
                Assert.False(reader2.Read());
                Assert.False(reader2.NextResult());
            }
        }

        [Fact]
        public void QueryBasicWithoutQuery()
        {
            int? i = connection.Query<int?>("print 'not a query'").FirstOrDefault();
            Assert.Null(i);
        }

        [Fact]
        public void QueryComplexWithoutQuery()
        {
            var obj = connection.Query<Foo1>("print 'not a query'").FirstOrDefault();
            Assert.Null(obj);
        }

        [FactLongRunning]
        public void Issue263_Timeout()
        {
            var watch = Stopwatch.StartNew();
            var i = connection.Query<int>("waitfor delay '00:01:00'; select 42;", commandTimeout: 300, buffered: false).Single();
            watch.Stop();
            Assert.Equal(42, i);
            var minutes = watch.ElapsedMilliseconds / 1000 / 60;
            Assert.True(minutes >= 0.95 && minutes <= 1.05);
        }

        [Fact]
        public void SO30435185_InvalidTypeOwner()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
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
                Assert.Equal(1, rowcount);
            });
            Assert.Equal("An enumerable sequence of parameters (arrays, lists, etc) is not allowed in this context", ex.Message);
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
            Assert.Equal(2, rows.Count);

            // check row 1
            var row = rows[1];
            Assert.Equal(1, row.Pid);
            Assert.Equal(2, row.Value);

            // check row 2
            row = rows[2];
            Assert.Equal(2, row.Pid);
            Assert.Equal(568, row.Value);
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
            Assert.Equal(42, obj.Id);
            Assert.Equal("def", obj.Name);
        }

        private class HazGetOnly
        {
            public int Id { get; }
            public string Name { get; } = "abc";
        }
    }
}
