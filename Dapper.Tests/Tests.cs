//#define POSTGRESQL // uncomment to run postgres tests

#if SQLITE && (NET40 || NET45)
using SqliteConnection = System.Data.SQLite.SQLiteConnection;
#endif

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using System.IO;
using System.Data;
using System.Collections;
using System.Reflection;
using System.Dynamic;
using System.ComponentModel;
using Microsoft.CSharp.RuntimeBinder;
using System.Globalization;
using System.Threading;
using System.Data.SqlTypes;
using System.Diagnostics;
using Xunit;
using System.Data.Common;
using System.Text.RegularExpressions;
#if FIREBIRD
using FirebirdSql.Data.FirebirdClient;
#endif
#if ENTITY_FRAMEWORK
using System.Data.Entity.Spatial;
using Microsoft.SqlServer.Types;
#endif
#if SQL_CE
using System.Data.SqlServerCe;
#endif
using SqlServerTypes;
#if POSTGRESQL
using Npgsql;
#endif
#if SQLITE
#if NET40 || NET45
using System.Data.SQLite;
#else
using Microsoft.Data.Sqlite;
#endif
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

#if COREFX
namespace System.ComponentModel {
    public sealed class DescriptionAttribute : Attribute {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
        public string Description {get;private set;}
    }
}
namespace System
{   
    public enum GenericUriParserOptions
    {
        Default
    }
    public class GenericUriParser
    {
        private GenericUriParserOptions options;

        public GenericUriParser(GenericUriParserOptions options)
        {
            this.options = options;
        }
    }
}
#endif

namespace Dapper.Tests
{
    public partial class TestSuite : IDisposable
    {
        public static string ConnectionString =>
            IsAppVeyor
                ? @"Server=(local)\SQL2014;Database=tempdb;User ID=sa;Password=Password12!"
                : "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public static string OleDbConnectionString =>
            IsAppVeyor
                ? @"Provider=SQLOLEDB;Data Source=(local)\SQL2014;Initial Catalog=tempdb;User Id=sa;Password=Password12!"
                : "Provider=SQLOLEDB;Data Source=.;Initial Catalog=tempdb;Integrated Security=SSPI";

        public static SqlConnection GetOpenConnection(bool mars = false)
        {
            var cs = ConnectionString;
            if (mars)
            {
                var scsb = new SqlConnectionStringBuilder(cs)
                {
                    MultipleActiveResultSets = true
                };
                cs = scsb.ConnectionString;
            }
            var connection = new SqlConnection(cs);
            connection.Open();
            return connection;
        }

        public static SqlConnection GetClosedConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            if (conn.State != ConnectionState.Closed) throw new InvalidOperationException("should be closed!");
            return conn;
        }

        private SqlConnection _connection, _marsConnection;

        private SqlConnection connection => _connection ?? (_connection = GetOpenConnection());
        private SqlConnection marsConnection => _marsConnection ?? (_marsConnection = GetOpenConnection(true));

        static TestSuite()
        {
#if COREFX
            Console.WriteLine("CoreCLR");
#else
            Console.WriteLine(".NET: " + Environment.Version);
#endif
            Console.WriteLine("Dapper: " + typeof(SqlMapper).AssemblyQualifiedName);
            Console.WriteLine("Using Connectionstring: {0}", ConnectionString);
#if !(COREFX || DNX)
            Console.Write("Loading native assemblies for SQL types...");
            try {
                Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
                Console.WriteLine("done.");
            } catch(Exception ex)
            {
                Console.WriteLine("failed.");
                Console.Error.WriteLine(ex.Message);
            }
#endif
        }

        public TestSuite()
        {
        }

        public void Dispose()
        {
            connection?.Dispose();
        }

        // http://stackoverflow.com/q/8593871

        [Fact]
        public void TestListOfAnsiStrings()
        {
            var results = connection.Query<string>("select * from (select 'a' str union select 'b' union select 'c') X where str in @strings",
                new { strings = new[] {
                    new DbString { IsAnsi = true, Value = "a" },
                    new DbString { IsAnsi = true, Value = "b" }
                } }).ToList();

            results.Count.IsEqualTo(2);
            results.Sort();
            results[0].IsEqualTo("a");
            results[1].IsEqualTo("b");
        }

        [Fact]
        public void TestListExpansionPadding_Enabled()
        {
            TestListExpansionPadding(true);
        }
        [Fact]
        public void TestListExpansionPadding_Disabled()
        {
            TestListExpansionPadding(false);
        }

        private void TestListExpansionPadding(bool enabled)
        {
            bool oldVal = SqlMapper.Settings.PadListExpansions;
            try
            {
                SqlMapper.Settings.PadListExpansions = enabled;
                connection.ExecuteScalar<int>(@"
create table #ListExpansion(id int not null identity(1,1), value int null);
insert #ListExpansion (value) values (null);
declare @loop int = 0;
while (@loop < 12)
begin -- double it
	insert #ListExpansion (value) select value from #ListExpansion;
	set @loop = @loop + 1;
end

select count(1) as [Count] from #ListExpansion").IsEqualTo(4096);

                var list = new List<int>();
                int nextId = 1, batchCount;
                var rand = new Random(12345);
                const int SQL_SERVER_MAX_PARAMS = 2095;
                TestListForExpansion(list, enabled); // test while empty
                while (list.Count < SQL_SERVER_MAX_PARAMS)
                {
                    try
                    {
                        if (list.Count <= 20) batchCount = 1;
                        else if (list.Count <= 200) batchCount = rand.Next(1, 40);
                        else batchCount = rand.Next(1, 100);

                        for (int j = 0; j < batchCount && list.Count < SQL_SERVER_MAX_PARAMS; j++)
                            list.Add(nextId++);

                        TestListForExpansion(list, enabled);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failure with {list.Count} items: {ex.Message}", ex);
                    }
                }
            }
            finally
            {
                SqlMapper.Settings.PadListExpansions = oldVal;
            }
        }

        private void TestListForExpansion(List<int> list, bool enabled)
        {
            var row = connection.QuerySingle(@"
declare @hits int, @misses int, @count int;
select @count = count(1) from #ListExpansion;
select @hits = count(1) from #ListExpansion where id in @ids ;
select @misses = count(1) from #ListExpansion where not id in @ids ;
declare @query nvarchar(max) = N' in @ids '; -- ok, I confess to being pleased with this hack ;p
select @hits as [Hits], (@count - @misses) as [Misses], @query as [Query];
", new { ids = list });
            int hits = row.Hits, misses = row.Misses;
            string query = row.Query;
            int argCount = Regex.Matches(query, "@ids[0-9]").Count;
            int expectedCount = GetExpectedListExpansionCount(list.Count, enabled);
            hits.IsEqualTo(list.Count);
            misses.IsEqualTo(list.Count);
            argCount.IsEqualTo(expectedCount);
        }

        static int GetExpectedListExpansionCount(int count, bool enabled)
        {
            if (!enabled) return count;

            if (count <= 5 || count > 2070) return count;

            int padFactor;
            if (count <= 150) padFactor = 10;
            else if (count <= 750) padFactor = 50;
            else if (count <= 2000) padFactor = 100;
            else if (count <= 2070) padFactor = 10;
            else padFactor = 200;

            int blocks = count / padFactor, delta = count % padFactor;
            if (delta != 0) blocks++;
            return blocks * padFactor;
        }

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

        struct CarWithAllProps
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
        public void PassInIntArray()
        {
            connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[] { 1, 2, 3 }.AsEnumerable() })
             .IsSequenceEqualTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void PassInEmptyIntArray()
        {
            connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[0] })
             .IsSequenceEqualTo(new int[0]);
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
        public void TestSchemaChangedMultiMap()
        {
            connection.Execute("create table #dog(Age int, Name nvarchar(max)) insert #dog values(1, 'Alf')");
            try
            {
                var tuple = connection.Query<Dog, Dog, Tuple<Dog, Dog>>("select * from #dog d1 join #dog d2 on 1=1", Tuple.Create, splitOn: "Age").Single();

                tuple.Item1.Name.IsEqualTo("Alf");
                tuple.Item1.Age.IsEqualTo(1);
                tuple.Item2.Name.IsEqualTo("Alf");
                tuple.Item2.Age.IsEqualTo(1);

                connection.Execute("alter table #dog drop column Name");
                tuple = connection.Query<Dog, Dog, Tuple<Dog, Dog>>("select * from #dog d1 join #dog d2 on 1=1", Tuple.Create, splitOn: "Age").Single();

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

        [Fact]
        public void TestReadMultipleIntegersWithSplitOnAny()
        {
            connection.Query<int, int, int, Tuple<int, int, int>>(
                "select 1,2,3 union all select 4,5,6", Tuple.Create, splitOn: "*")
             .IsSequenceEqualTo(new[] { Tuple.Create(1, 2, 3), Tuple.Create(4, 5, 6) });
        }

        [Fact]
        public void TestDoubleParam()
        {
            connection.Query<double>("select @d", new { d = 0.1d }).First()
                .IsEqualTo(0.1d);
        }

        [Fact]
        public void TestBoolParam()
        {
            connection.Query<bool>("select @b", new { b = false }).First()
                .IsFalse();
        }

        // http://code.google.com/p/dapper-dot-net/issues/detail?id=70
        // https://connect.microsoft.com/VisualStudio/feedback/details/381934/sqlparameter-dbtype-dbtype-time-sets-the-parameter-to-sqldbtype-datetime-instead-of-sqldbtype-time

        [Fact]
        public void TestTimeSpanParam()
        {
            connection.Query<TimeSpan>("select @ts", new { ts = TimeSpan.FromMinutes(42) }).First()
                .IsEqualTo(TimeSpan.FromMinutes(42));
        }

        [Fact]
        public void TestStrings()
        {
            connection.Query<string>(@"select 'a' a union select 'b'")
                .IsSequenceEqualTo(new[] { "a", "b" });
        }

        // see http://stackoverflow.com/questions/16726709/string-format-with-sql-wildcard-causing-dapper-query-to-break
        [Fact]
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

            string query = string.Format(formatted, allow_start_wildcards ? use_both : use_end_only);
            string term = "F"; // the term the user searched for

            connection.Execute(@"create table #users16726709 (first_name varchar(200), last_name varchar(200))
insert #users16726709 values ('Fred','Bloggs') insert #users16726709 values ('Tony','Farcus') insert #users16726709 values ('Albert','TenoF')");

            // Using Dapper
            connection.Query(end_wildcard, new { search_term = term }).Count().IsEqualTo(2);
            connection.Query(both_wildcards, new { search_term = term }).Count().IsEqualTo(3);
            connection.Query(query, new { search_term = term }).Count().IsEqualTo(2);

        }

        public class Dog
        {
            public int? Age { get; set; }
            public Guid Id { get; set; }
            public string Name { get; set; }
            public float? Weight { get; set; }

            public int IgnoredProperty { get { return 1; } }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TestSimpleNull()
        {
            connection.Query<DateTime?>("select null").First().IsNull();
        }

        [Fact]
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
        public void TestExecuteCommandWithHybridParameters()
        {
            var p = new DynamicParameters(new { a = 1, b = 2 });
            p.Add("c", dbType: DbType.Int32, direction: ParameterDirection.Output);
            connection.Execute(@"set @c = @a + @b", p);
            p.Get<int>("@c").IsEqualTo(3);
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

        class Student
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

        [Fact]
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

            ((int?)row.A)
                .IsNull();

            ((int?)row.B)
                .IsEqualTo(2);
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
            { }

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
            { }

            // should not exception, since enumertated
            en = connection.Query("select 1 as one", buffered: false);

            gotException.IsTrue();
        }

        [Fact]
        public void TestNakedBigInt()
        {
            long foo = 12345;
            var result = connection.Query<long>("select @foo", new { foo }).Single();
            foo.IsEqualTo(result);
        }

        [Fact]
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

#if !COREFX
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

#if SQL_CE
        [Fact]
        public void MultiRSSqlCE()
        {
            if (File.Exists("Test.DB.sdf"))
                File.Delete("Test.DB.sdf");

            var cnnStr = "Data Source = Test.DB.sdf;";
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
#endif
#if LINQ2SQL
        [Fact]
        public void TestLinqBinaryToClass()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);

            var output = connection.Query<WithBinary>("select @input as [Value]", new { input }).First().Value;

            output.ToArray().IsSequenceEqualTo(orig);
        }
        
        [Fact]
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
#endif

        [Fact]
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

        class WithBizarreData
        {
            public GenericUriParser Foo { get; set; }
            public int Bar { get; set; }
        }

        class WithCharValue
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

        [Fact]
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

        [Fact]
        public void TestDynamicParamNullSupport()
        {
            var p = new DynamicParameters();

            p.Add("@b", dbType: DbType.Int32, direction: ParameterDirection.Output);
            connection.Execute("select @b = null", p);

            p.Get<int?>("@b").IsNull();
        }

        class WithPrivateConstructor
        {
            public int Foo { get; set; }
            private WithPrivateConstructor() { }
        }

        [Fact]
        public void TestWithNonPublicConstructor()
        {
            var output = connection.Query<WithPrivateConstructor>("select 1 as Foo").First();
            output.Foo.IsEqualTo(1);
        }


        [Fact]
        public void WorkDespiteHavingWrongStructColumnTypes()
        {
            var hazInt = connection.Query<CanHazInt>("select cast(1 as bigint) Value").Single();
            hazInt.Value.Equals(1);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TestCustomTypeMap()
        {
            // default mapping
            var item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            item.A.IsEqualTo("AVal");
            item.B.IsEqualTo("BVal");

            // custom mapping
            var map = new CustomPropertyTypeMap(typeof(TypeWithMapping),
                (type, columnName) => type.GetProperties().FirstOrDefault(prop => GetDescriptionFromAttribute(prop) == columnName));
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
        static string GetDescriptionFromAttribute(MemberInfo member)
        {
            if (member == null) return null;
#if COREFX
            var data = member.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(DescriptionAttribute));
            return (string) data?.ConstructorArguments.Single().Value;
#else
            var attrib = (DescriptionAttribute)Attribute.GetCustomAttribute(member, typeof(DescriptionAttribute), false);
            return attrib == null ? null : attrib.Description;
#endif
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

        [Fact]
        public void TestWrongTypes_WithRightTypes()
        {
            var item = connection.Query<WrongTypes>("select 1 as A, cast(2.0 as float) as B, cast(3 as bigint) as C, cast(1 as bit) as D").Single();
            item.A.Equals(1);
            item.B.Equals(2.0);
            item.C.Equals(3L);
            item.D.Equals(true);
        }

        [Fact]
        public void TestWrongTypes_WithWrongTypes()
        {
            var item = connection.Query<WrongTypes>("select cast(1.0 as float) as A, 2 as B, 3 as C, cast(1 as bigint) as D").Single();
            item.A.Equals(1);
            item.B.Equals(2.0);
            item.C.Equals(3L);
            item.D.Equals(true);
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
        class Multi1
        {
            public int Id { get; set; }
        }
        class Multi2
        {
            public int Id { get; set; }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void SO35554284_QueryMultipleUntilConsumed()
        {
            using (var reader = connection.QueryMultiple("select 1 as Id; select 2 as Id; select 3 as Id;"))
            {
                List<HazNameId> items = new List<HazNameId>();
                while (!reader.IsConsumed)
                {
                    items.AddRange(reader.Read<HazNameId>());
                }
                items.Count.IsEqualTo(3);
                items[0].Id.IsEqualTo(1);
                items[1].Id.IsEqualTo(2);
                items[2].Id.IsEqualTo(3);
            }
        }

        [Fact]
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

        [Fact]
        public void TestMultiSelectWithSomeEmptyGridsUnbuffered()
        {
            TestMultiSelectWithSomeEmptyGrids(false);
        }
        [Fact]
        public void TestMultiSelectWithSomeEmptyGridsBuffered()
        {
            TestMultiSelectWithSomeEmptyGrids(true);
        }
        private void TestMultiSelectWithSomeEmptyGrids(bool buffered)
        {
            using (var reader = connection.QueryMultiple("select 1; select 2 where 1 = 0; select 3 where 1 = 0; select 4;"))
            {
                var one = reader.Read<int>(buffered: buffered).ToArray();
                var two = reader.Read<int>(buffered: buffered).ToArray();
                var three = reader.Read<int>(buffered: buffered).ToArray();
                var four = reader.Read<int>(buffered: buffered).ToArray();
                try
                { // only returned four grids; expect a fifth read to fail
                    reader.Read<int>(buffered: buffered);
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

        // see http://stackoverflow.com/questions/16955357/issue-about-dapper
        [Fact]
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
        public void TestChangingDefaultStringTypeMappingToAnsiString()
        {
            var sql = "SELECT SQL_VARIANT_PROPERTY(CONVERT(sql_variant, @testParam),'BaseType') AS BaseType";
            var param = new { testParam = "TestString" };

            var result01 = connection.Query<string>(sql, param).FirstOrDefault();
            result01.IsEqualTo("nvarchar");

            Dapper.SqlMapper.PurgeQueryCache();

            Dapper.SqlMapper.AddTypeMap(typeof(string), DbType.AnsiString);   // Change Default String Handling to AnsiString
            var result02 = connection.Query<string>(sql, param).FirstOrDefault();
            result02.IsEqualTo("varchar");

            Dapper.SqlMapper.PurgeQueryCache();
            Dapper.SqlMapper.AddTypeMap(typeof(string), DbType.String);  // Restore Default to Unicode String
        }

        [Fact]
        public void TestChangingDefaultStringTypeMappingToAnsiStringFirstOrDefault()
        {
            var sql = "SELECT SQL_VARIANT_PROPERTY(CONVERT(sql_variant, @testParam),'BaseType') AS BaseType";
            var param = new { testParam = "TestString" };

            var result01 = connection.QueryFirstOrDefault<string>(sql, param);
            result01.IsEqualTo("nvarchar");

            Dapper.SqlMapper.PurgeQueryCache();

            Dapper.SqlMapper.AddTypeMap(typeof(string), DbType.AnsiString);   // Change Default String Handling to AnsiString
            var result02 = connection.QueryFirstOrDefault<string>(sql, param);
            result02.IsEqualTo("varchar");

            Dapper.SqlMapper.PurgeQueryCache();
            Dapper.SqlMapper.AddTypeMap(typeof(string), DbType.String);  // Restore Default to Unicode String
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

        [Fact]
        public void TestDoubleDecimalConversions_SO18228523_RightWay()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(1 as float) as A, cast(2 as float) as B, cast(3 as decimal) as C, cast(4 as decimal) as D").Single();
            row.A.Equals(1.0);
            row.B.Equals(2.0);
            row.C.Equals(3.0M);
            row.D.Equals(4.0M);
        }

        [Fact]
        public void TestDoubleDecimalConversions_SO18228523_WrongWay()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(1 as decimal) as A, cast(2 as decimal) as B, cast(3 as float) as C, cast(4 as float) as D").Single();
            row.A.Equals(1.0);
            row.B.Equals(2.0);
            row.C.Equals(3.0M);
            row.D.Equals(4.0M);
        }

        [Fact]
        public void TestDoubleDecimalConversions_SO18228523_Nulls()
        {
            var row = connection.Query<HasDoubleDecimal>(
                "select cast(null as decimal) as A, cast(null as decimal) as B, cast(null as float) as C, cast(null as float) as D").Single();
            row.A.Equals(0.0);
            row.B.IsNull();
            row.C.Equals(0.0M);
            row.D.IsNull();
        }
        
        private static CultureInfo ActiveCulture
        {
#if COREFX
            get { return CultureInfo.CurrentCulture; }
            set { CultureInfo.CurrentCulture = value; }
#else
            get { return Thread.CurrentThread.CurrentCulture; }
            set { Thread.CurrentThread.CurrentCulture = value; }
#endif
        }

        [FactUnlessCaseSensitiveDatabase]
        public void TestParameterInclusionNotSensitiveToCurrentCulture()
        {
            // note this might fail if your database server is case-sensitive
            CultureInfo current = ActiveCulture;
            try
            {
                ActiveCulture = new CultureInfo("tr-TR");

                connection.Query<int>("select @pid", new { PId = 1 }).Single();
            }
            finally
            {
                ActiveCulture = current;
            }
        }

        [Fact]
        public void LiteralReplacement()
        {
            connection.Execute("create table #literal1 (id int not null, foo int not null)");
            connection.Execute("insert #literal1 (id,foo) values ({=id}, @foo)", new { id = 123, foo = 456 });
            var rows = new[] { new { id = 1, foo = 2 }, new { id = 3, foo = 4 } };
            connection.Execute("insert #literal1 (id,foo) values ({=id}, @foo)", rows);
            var count = connection.Query<int>("select count(1) from #literal1 where id={=foo}", new { foo = 123 }).Single();
            count.IsEqualTo(1);
            int sum = connection.Query<int>("select sum(id) + sum(foo) from #literal1").Single();
            sum.IsEqualTo(123 + 456 + 1 + 2 + 3 + 4);
        }

        [Fact]
        public void LiteralReplacementDynamic()
        {
            var args = new DynamicParameters();
            args.Add("id", 123);
            connection.Execute("create table #literal2 (id int not null)");
            connection.Execute("insert #literal2 (id) values ({=id})", args);

            args = new DynamicParameters();
            args.Add("foo", 123);
            var count = connection.Query<int>("select count(1) from #literal2 where id={=foo}", args).Single();
            count.IsEqualTo(1);
        }

        enum AnEnum
        {
            A = 2,
            B = 1
        }
        enum AnotherEnum : byte
        {
            A = 2,
            B = 1
        }

        [Fact]
        public void AdoNetEnumValue()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "select @foo";
                var p = cmd.CreateParameter();
                p.ParameterName = "@foo";
                p.DbType = DbType.Int32; // it turns out that this is the key piece; setting the DbType
                p.Value = AnEnum.B;         
                cmd.Parameters.Add(p);
                object value = cmd.ExecuteScalar();
                AnEnum val = (AnEnum)value;
                val.IsEqualTo(AnEnum.B);
            }
        }

        [Fact]
        public void DapperEnumValue_SqlServer()
        {
            DapperEnumValue(connection);
        }

#if SQLITE
        [FactSqlite]
        public void DapperEnumValue_Sqlite()
        {
            using (var connection = GetSqliteConnection())
            {
                DapperEnumValue(connection);
            }
        }
#endif
#if MYSQL
        [FactMySql]
        public void DapperEnumValue_Mysql()
        {
            using (var connection = GetMySqlConnection())
            {
                DapperEnumValue(connection);
            }
        }
#endif
        private static void DapperEnumValue(IDbConnection connection)
        {
            // test passing as AsEnum, reading as int
            var v = (AnEnum)connection.QuerySingle<int>("select @v, @y, @z", new { v = AnEnum.B, y = (AnEnum?)AnEnum.B, z = (AnEnum?)null });
            v.IsEqualTo(AnEnum.B);

            var args = new DynamicParameters();
            args.Add("v", AnEnum.B);
            args.Add("y", AnEnum.B);
            args.Add("z", null);
            v = (AnEnum)connection.QuerySingle<int>("select @v, @y, @z", args);
            v.IsEqualTo(AnEnum.B);

            // test passing as int, reading as AnEnum
            var k = (int)connection.QuerySingle<AnEnum>("select @v, @y, @z", new { v = (int)AnEnum.B, y = (int?)(int)AnEnum.B, z = (int?)null });
            k.IsEqualTo((int)AnEnum.B);

            args = new DynamicParameters();
            args.Add("v", (int)AnEnum.B);
            args.Add("y", (int)AnEnum.B);
            args.Add("z", null);
            k = (int)connection.QuerySingle<AnEnum>("select @v, @y, @z", args);
            k.IsEqualTo((int)AnEnum.B);
        }

        [Fact]
        public void LiteralReplacementEnumAndString()
        {
            var args = new { x = AnEnum.B, y = 123.45M, z = AnotherEnum.A };
            var row = connection.Query("select {=x} as x,{=y} as y,cast({=z} as tinyint) as z", args).Single();
            AnEnum x = (AnEnum)(int)row.x;
            decimal y = row.y;
            AnotherEnum z = (AnotherEnum)(byte)row.z;
            x.Equals(AnEnum.B);
            y.Equals(123.45M);
            z.Equals(AnotherEnum.A);
        }

        [Fact]
        public void LiteralReplacementDynamicEnumAndString()
        {
            var args = new DynamicParameters();
            args.Add("x", AnEnum.B);
            args.Add("y", 123.45M);
            args.Add("z", AnotherEnum.A);
            var row = connection.Query("select {=x} as x,{=y} as y,cast({=z} as tinyint) as z", args).Single();
            AnEnum x = (AnEnum)(int)row.x;
            decimal y = row.y;
            AnotherEnum z = (AnotherEnum)(byte)row.z;
            x.Equals(AnEnum.B);
            y.Equals(123.45M);
            z.Equals(AnotherEnum.A);
        }

        [Fact]
        public void LiteralReplacementBoolean()
        {
            var row = connection.Query<int?>("select 42 where 1 = {=val}", new { val = true }).SingleOrDefault();
            row.IsNotNull();
            row.IsEqualTo(42);
            row = connection.Query<int?>("select 42 where 1 = {=val}", new { val = false }).SingleOrDefault();
            row.IsNull();
        }

        [Fact]
        public void LiteralReplacementWithIn()
        {
            var data = connection.Query<MyRow>("select @x where 1 in @ids and 1 ={=a}",
                new { x = 1, ids = new[] { 1, 2, 3 }, a = 1 }).ToList();
        }

        class MyRow
        {
            public int x { get; set; }
        }

        [Fact]
        public void LiteralIn()
        {
            connection.Execute("create table #literalin(id int not null);");
            connection.Execute("insert #literalin (id) values (@id)", new[] {
                new { id = 1 },
                new { id = 2 },
                new { id = 3 },
            });
            var count = connection.Query<int>("select count(1) from #literalin where id in {=ids}",
                new { ids = new[] { 1, 3, 4 } }).Single();
            count.IsEqualTo(2);
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

        class HasInt32
        {
            public int Value { get; set; }
        }

        // http://stackoverflow.com/q/23696254/23354
        [Fact]
        public void DownwardIntegerConversion()
        {
            const string sql = "select cast(42 as bigint) as Value";
            int i = connection.Query<HasInt32>(sql).Single().Value;
            Assert.IsEqualTo(42, i);

            i = connection.Query<int>(sql).Single();
            Assert.IsEqualTo(42, i);
        }

        class HasDoubleDecimal
        {
            public double A { get; set; }
            public double? B { get; set; }
            public decimal C { get; set; }
            public decimal? D { get; set; }
        }
        
        [Fact]
        public void GuidIn_SO_24177902()
        {
            // invent and populate
            Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid(), d = Guid.NewGuid();
            connection.Execute("create table #foo (i int, g uniqueidentifier)");
            connection.Execute("insert #foo(i,g) values(@i,@g)",
                new[] { new { i = 1, g = a }, new { i = 2, g = b },
                new { i = 3, g = c },new { i = 4, g = d }});

            // check that rows 2&3 yield guids b&c
            var guids = connection.Query<Guid>("select g from #foo where i in (2,3)").ToArray();
            guids.Length.Equals(2);
            guids.Contains(a).Equals(false);
            guids.Contains(b).Equals(true);
            guids.Contains(c).Equals(true);
            guids.Contains(d).Equals(false);

            // in query on the guids
            var rows = connection.Query("select * from #foo where g in @guids order by i", new { guids })
                .Select(row => new { i = (int)row.i, g = (Guid)row.g }).ToArray();
            rows.Length.Equals(2);
            rows[0].i.Equals(2);
            rows[0].g.Equals(b);
            rows[1].i.Equals(3);
            rows[1].g.Equals(c);
        }

        [Fact]
        public void TypeBasedViaDynamic()
        {
            Type type = GetSomeType();

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
            Type type = GetSomeType();

            dynamic actual = connection.Query(type, "select @A as [A], @B as [B]", new { A = 123, B = "abc" }).FirstOrDefault();
            ((object)actual).GetType().IsEqualTo(type);
            int a = actual.A;
            string b = actual.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");
        }

        [Fact]
        public void TypeBasedViaTypeMulti()
        {
            Type type = GetSomeType();

            dynamic first, second;
            using (var multi = connection.QueryMultiple("select @A as [A], @B as [B]; select @C as [A], @D as [B]",
                new { A = 123, B = "abc", C = 456, D = "def" }))
            {
                first = multi.Read(type).Single();
                second = multi.Read(type).Single();
            }
            ((object)first).GetType().IsEqualTo(type);
            int a = first.A;
            string b = first.B;
            a.IsEqualTo(123);
            b.IsEqualTo("abc");

            ((object)second).GetType().IsEqualTo(type);
            a = second.A;
            b = second.B;
            a.IsEqualTo(456);
            b.IsEqualTo("def");
        }
        
        private T CheetViaDynamic<T>(T template, string query, object args)
        {
            return connection.Query<T>(query, args).SingleOrDefault();
        }
        static Type GetSomeType()
        {
            return typeof(SomeType);
        }
        public class SomeType
        {
            public int A { get; set; }
            public string B { get; set; }
        }
#if !COREFX
        class WithInit : ISupportInitialize
        {
            public string Value { get; set; }
            public int Flags { get; set; }

            void ISupportInitialize.BeginInit()
            {
                Flags += 1;
            }

            void ISupportInitialize.EndInit()
            {
                Flags += 30;
            }
        }
#endif

        [Fact]
        public void SO24607639_NullableBools()
        {
            var obj = connection.Query<HazBools>(
                @"declare @vals table (A bit null, B bit null, C bit null);
                insert @vals (A,B,C) values (1,0,null);
                select * from @vals").Single();
            obj.IsNotNull();
            obj.A.Value.IsEqualTo(true);
            obj.B.Value.IsEqualTo(false);
            obj.C.IsNull();
        }
        class HazBools
        {
            public bool? A { get; set; }
            public bool? B { get; set; }
            public bool? C { get; set; }
        }

        [Fact]
        public void SO24605346_ProcsAndStrings()
        {
            connection.Execute(@"create proc #GetPracticeRebateOrderByInvoiceNumber @TaxInvoiceNumber nvarchar(20) as
                select @TaxInvoiceNumber as [fTaxInvoiceNumber]");
            string InvoiceNumber = "INV0000000028PPN";
            var result = connection.Query<PracticeRebateOrders>("#GetPracticeRebateOrderByInvoiceNumber", new
            {
                TaxInvoiceNumber = InvoiceNumber
            }, commandType: CommandType.StoredProcedure).FirstOrDefault();

            result.TaxInvoiceNumber.IsEqualTo("INV0000000028PPN");
        }
        class PracticeRebateOrders
        {
            public string fTaxInvoiceNumber;
#if !COREFX
            [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
#endif
            public string TaxInvoiceNumber { get { return fTaxInvoiceNumber; } set { fTaxInvoiceNumber = value; } }
        }

        public class RatingValueHandler : Dapper.SqlMapper.TypeHandler<RatingValue>
        {
            private RatingValueHandler() { }
            public static readonly RatingValueHandler Default = new RatingValueHandler();
            public override RatingValue Parse(object value)
            {
                if (value is Int32)
                    return new RatingValue() { Value = (Int32)value };

                throw new FormatException("Invalid conversion to RatingValue");
            }

            public override void SetValue(IDbDataParameter parameter, RatingValue value)
            {
                // ... null, range checks etc ...
                parameter.DbType = System.Data.DbType.Int32;
                parameter.Value = value.Value;
            }
        }
        public class RatingValue
        {
            public Int32 Value { get; set; }
            // ... some other properties etc ...
        }

        public class MyResult
        {
            public String CategoryName { get; set; }
            public RatingValue CategoryRating { get; set; }
        }

        [Fact]
        public void SO24740733_TestCustomValueHandler()
        {
            Dapper.SqlMapper.AddTypeHandler(RatingValueHandler.Default);
            var foo = connection.Query<MyResult>("SELECT 'Foo' AS CategoryName, 200 AS CategoryRating").Single();

            foo.CategoryName.IsEqualTo("Foo");
            foo.CategoryRating.Value.IsEqualTo(200);
        }

        enum SO27024806Enum { Foo, Bar }

        private class SO27024806Class
        {
            public SO27024806Class(SO27024806Enum myField)
            {
                this.MyField = myField;
            }

            public SO27024806Enum MyField { get; set; }
        }

        [Fact]
        public void SO27024806_TestVarcharEnumMemberWithExplicitConstructor()
        {
            var foo = connection.Query<SO27024806Class>("SELECT 'Foo' AS myField").Single();
            foo.MyField.IsEqualTo(SO27024806Enum.Foo);
        }


        [Fact]
        public void SO24740733_TestCustomValueSingleColumn()
        {
            Dapper.SqlMapper.AddTypeHandler(RatingValueHandler.Default);
            var foo = connection.Query<RatingValue>("SELECT 200 AS CategoryRating").Single();

            foo.Value.IsEqualTo(200);
        }

        public class StringListTypeHandler : Dapper.SqlMapper.TypeHandler<List<String>>
        {
            private StringListTypeHandler() { }
            public static readonly StringListTypeHandler Default = new StringListTypeHandler();
            //Just a simple List<string> type handler implementation
            public override void SetValue(IDbDataParameter parameter, List<string> value)
            {
                parameter.Value = String.Join(",", value);
            }

            public override List<string> Parse(object value)
            {
                return ((value as String) ?? "").Split(',').ToList();
            }
        }
        public class MyObjectWithStringList
        {
            public List<String> Names { get; set; }
        }

        [Fact]
        public void Issue253_TestIEnumerableTypeHandlerParsing()
        {
            Dapper.SqlMapper.ResetTypeHandlers();
            Dapper.SqlMapper.AddTypeHandler(StringListTypeHandler.Default);
            var foo = connection.Query<MyObjectWithStringList>("SELECT 'Sam,Kyro' AS Names").Single();
            foo.Names.IsSequenceEqualTo(new[] { "Sam", "Kyro" });
        }

        [Fact]
        public void Issue253_TestIEnumerableTypeHandlerSetParameterValue()
        {
            Dapper.SqlMapper.ResetTypeHandlers();
            Dapper.SqlMapper.AddTypeHandler(StringListTypeHandler.Default);

            connection.Execute("CREATE TABLE #Issue253 (Names VARCHAR(50) NOT NULL);");
            try
            {
                String names = "Sam,Kyro";
                List<String> names_list = names.Split(',').ToList();
                var foo = connection.Query<String>("INSERT INTO #Issue253 (Names) VALUES (@Names); SELECT Names FROM #Issue253;", new { Names = names_list }).Single();
                foo.IsEqualTo(names);
            }
            finally
            {
                connection.Execute("DROP TABLE #Issue253;");
            }
        }

        [Fact]
        public void Issue130_IConvertible()
        {
            dynamic row = connection.Query("select 1 as [a], '2' as [b]").Single();
            int a = row.a;
            string b = row.b;
            a.IsEqualTo(1);
            b.IsEqualTo("2");

            row = connection.Query<dynamic>("select 3 as [a], '4' as [b]").Single();
            a = row.a;
            b = row.b;
            a.IsEqualTo(3);
            b.IsEqualTo("4");
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

#if ENTITY_FRAMEWORK
            Dapper.EntityFramework.Handlers.Register();
            var geo = DbGeography.LineFromText("LINESTRING(-122.360 47.656, -122.343 47.656 )", 4326);
            var geo2 = connection.ExecuteScalar<DbGeography>("select @geo", new { geo });
            geo2.IsNotNull();
#endif
        }

#if ENTITY_FRAMEWORK
        public void Issue570_DbGeo_HasValues()
        {
            Dapper.EntityFramework.Handlers.Register();
            string redmond = "POINT (122.1215 47.6740)";
            DbGeography point = DbGeography.PointFromText(redmond, DbGeography.DefaultCoordinateSystemId);
            DbGeography orig = point.Buffer(20);


            var fromDb = connection.QuerySingle<DbGeography>("declare @geos table(geo geography); insert @geos(geo) values(@val); select * from @geos",
                new { val = orig });

            fromDb.Area.IsNotNull();
            fromDb.Area.IsEqualTo(orig.Area);
        }
#endif
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
        public void Issue136_ValueTypeHandlers()
        {
            Dapper.SqlMapper.ResetTypeHandlers();
            Dapper.SqlMapper.AddTypeHandler(typeof(LocalDate), LocalDateHandler.Default);
            var param = new LocalDateResult
            {
                NotNullable = new LocalDate { Year = 2014, Month = 7, Day = 25 },
                NullableNotNull = new LocalDate { Year = 2014, Month = 7, Day = 26 },
                NullableIsNull = null,
            };

            var result = connection.Query<LocalDateResult>("SELECT @NotNullable AS NotNullable, @NullableNotNull AS NullableNotNull, @NullableIsNull AS NullableIsNull", param).Single();

            Dapper.SqlMapper.ResetTypeHandlers();
            Dapper.SqlMapper.AddTypeHandler(typeof(LocalDate?), LocalDateHandler.Default);
            result = connection.Query<LocalDateResult>("SELECT @NotNullable AS NotNullable, @NullableNotNull AS NullableNotNull, @NullableIsNull AS NullableIsNull", param).Single();
        }

        public class LocalDateHandler : Dapper.SqlMapper.TypeHandler<LocalDate>
        {
            private LocalDateHandler() { }

            // Make the field type ITypeHandler to ensure it cannot be used with SqlMapper.AddTypeHandler<T>(TypeHandler<T>)
            // by mistake.
            public static readonly Dapper.SqlMapper.ITypeHandler Default = new LocalDateHandler();

            public override LocalDate Parse(object value)
            {
                var date = (DateTime)value;
                return new LocalDate { Year = date.Year, Month = date.Month, Day = date.Day };
            }

            public override void SetValue(IDbDataParameter parameter, LocalDate value)
            {
                parameter.DbType = DbType.DateTime;
                parameter.Value = new DateTime(value.Year, value.Month, value.Day);
            }
        }

        public struct LocalDate
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int Day { get; set; }
        }

        public class LocalDateResult
        {
            public LocalDate NotNullable { get; set; }
            public LocalDate? NullableNotNull { get; set; }
            public LocalDate? NullableIsNull { get; set; }
        }

        public class LotsOfNumerics
        {
            public enum E_Byte : byte { A = 0, B = 1 }
            public enum E_SByte : sbyte { A = 0, B = 1 }
            public enum E_Short : short { A = 0, B = 1 }
            public enum E_UShort : ushort { A = 0, B = 1 }
            public enum E_Int : int { A = 0, B = 1 }
            public enum E_UInt : uint { A = 0, B = 1 }
            public enum E_Long : long { A = 0, B = 1 }
            public enum E_ULong : ulong { A = 0, B = 1 }

            public E_Byte P_Byte { get; set; }
            public E_SByte P_SByte { get; set; }
            public E_Short P_Short { get; set; }
            public E_UShort P_UShort { get; set; }
            public E_Int P_Int { get; set; }
            public E_UInt P_UInt { get; set; }
            public E_Long P_Long { get; set; }
            public E_ULong P_ULong { get; set; }

            public bool N_Bool { get; set; }
            public byte N_Byte { get; set; }
            public sbyte N_SByte { get; set; }
            public short N_Short { get; set; }
            public ushort N_UShort { get; set; }
            public int N_Int { get; set; }
            public uint N_UInt { get; set; }
            public long N_Long { get; set; }
            public ulong N_ULong { get; set; }

            public float N_Float { get; set; }
            public double N_Double { get; set; }
            public decimal N_Decimal { get; set; }

            public E_Byte? N_P_Byte { get; set; }
            public E_SByte? N_P_SByte { get; set; }
            public E_Short? N_P_Short { get; set; }
            public E_UShort? N_P_UShort { get; set; }
            public E_Int? N_P_Int { get; set; }
            public E_UInt? N_P_UInt { get; set; }
            public E_Long? N_P_Long { get; set; }
            public E_ULong? N_P_ULong { get; set; }

            public bool? N_N_Bool { get; set; }
            public byte? N_N_Byte { get; set; }
            public sbyte? N_N_SByte { get; set; }
            public short? N_N_Short { get; set; }
            public ushort? N_N_UShort { get; set; }
            public int? N_N_Int { get; set; }
            public uint? N_N_UInt { get; set; }
            public long? N_N_Long { get; set; }
            public ulong? N_N_ULong { get; set; }

            public float? N_N_Float { get; set; }
            public double? N_N_Double { get; set; }
            public decimal? N_N_Decimal { get; set; }
        }

        [Fact]
        public void TestBigIntForEverythingWorks_SqlLite()
        {
            TestBigIntForEverythingWorks_SqlLite_ByDataType<long>("bigint");
            TestBigIntForEverythingWorks_SqlLite_ByDataType<int>("int");
            TestBigIntForEverythingWorks_SqlLite_ByDataType<byte>("tinyint");
            TestBigIntForEverythingWorks_SqlLite_ByDataType<short>("smallint");
            TestBigIntForEverythingWorks_SqlLite_ByDataType<bool>("bit");
            TestBigIntForEverythingWorks_SqlLite_ByDataType<float>("float(24)");
            TestBigIntForEverythingWorks_SqlLite_ByDataType<double>("float(53)");
        }
        
        private void TestBigIntForEverythingWorks_SqlLite_ByDataType<T>(string dbType)
        {
            using (var reader = connection.ExecuteReader("select cast(1 as " + dbType + ")"))
            {
                reader.Read().IsTrue();
                reader.GetFieldType(0).Equals(typeof(T));
                reader.Read().IsFalse();
                reader.NextResult().IsFalse();
            }

            string sql = "select " + string.Join(",", typeof(LotsOfNumerics).GetProperties().Select(
                x => "cast (1 as " + dbType + ") as [" + x.Name + "]"));
            var row = connection.Query<LotsOfNumerics>(sql).Single();

            row.N_Bool.IsTrue();
            row.N_SByte.IsEqualTo((sbyte)1);
            row.N_Byte.IsEqualTo((byte)1);
            row.N_Int.IsEqualTo((int)1);
            row.N_UInt.IsEqualTo((uint)1);
            row.N_Short.IsEqualTo((short)1);
            row.N_UShort.IsEqualTo((ushort)1);
            row.N_Long.IsEqualTo((long)1);
            row.N_ULong.IsEqualTo((ulong)1);
            row.N_Float.IsEqualTo((float)1);
            row.N_Double.IsEqualTo((double)1);
            row.N_Decimal.IsEqualTo((decimal)1);

            row.P_Byte.IsEqualTo(LotsOfNumerics.E_Byte.B);
            row.P_SByte.IsEqualTo(LotsOfNumerics.E_SByte.B);
            row.P_Short.IsEqualTo(LotsOfNumerics.E_Short.B);
            row.P_UShort.IsEqualTo(LotsOfNumerics.E_UShort.B);
            row.P_Int.IsEqualTo(LotsOfNumerics.E_Int.B);
            row.P_UInt.IsEqualTo(LotsOfNumerics.E_UInt.B);
            row.P_Long.IsEqualTo(LotsOfNumerics.E_Long.B);
            row.P_ULong.IsEqualTo(LotsOfNumerics.E_ULong.B);

            row.N_N_Bool.Value.IsTrue();
            row.N_N_SByte.Value.IsEqualTo((sbyte)1);
            row.N_N_Byte.Value.IsEqualTo((byte)1);
            row.N_N_Int.Value.IsEqualTo((int)1);
            row.N_N_UInt.Value.IsEqualTo((uint)1);
            row.N_N_Short.Value.IsEqualTo((short)1);
            row.N_N_UShort.Value.IsEqualTo((ushort)1);
            row.N_N_Long.Value.IsEqualTo((long)1);
            row.N_N_ULong.Value.IsEqualTo((ulong)1);
            row.N_N_Float.Value.IsEqualTo((float)1);
            row.N_N_Double.Value.IsEqualTo((double)1);
            row.N_N_Decimal.IsEqualTo((decimal)1);

            row.N_P_Byte.Value.IsEqualTo(LotsOfNumerics.E_Byte.B);
            row.N_P_SByte.Value.IsEqualTo(LotsOfNumerics.E_SByte.B);
            row.N_P_Short.Value.IsEqualTo(LotsOfNumerics.E_Short.B);
            row.N_P_UShort.Value.IsEqualTo(LotsOfNumerics.E_UShort.B);
            row.N_P_Int.Value.IsEqualTo(LotsOfNumerics.E_Int.B);
            row.N_P_UInt.Value.IsEqualTo(LotsOfNumerics.E_UInt.B);
            row.N_P_Long.Value.IsEqualTo(LotsOfNumerics.E_Long.B);
            row.N_P_ULong.Value.IsEqualTo(LotsOfNumerics.E_ULong.B);

            TestBigIntForEverythingWorks<bool>(true, dbType);
            TestBigIntForEverythingWorks<sbyte>((sbyte)1, dbType);
            TestBigIntForEverythingWorks<byte>((byte)1, dbType);
            TestBigIntForEverythingWorks<int>((int)1, dbType);
            TestBigIntForEverythingWorks<uint>((uint)1, dbType);
            TestBigIntForEverythingWorks<short>((short)1, dbType);
            TestBigIntForEverythingWorks<ushort>((ushort)1, dbType);
            TestBigIntForEverythingWorks<long>((long)1, dbType);
            TestBigIntForEverythingWorks<ulong>((ulong)1, dbType);
            TestBigIntForEverythingWorks<float>((float)1, dbType);
            TestBigIntForEverythingWorks<double>((double)1, dbType);
            TestBigIntForEverythingWorks<decimal>((decimal)1, dbType);

            TestBigIntForEverythingWorks(LotsOfNumerics.E_Byte.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_SByte.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_Int.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_UInt.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_Short.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_UShort.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_Long.B, dbType);
            TestBigIntForEverythingWorks(LotsOfNumerics.E_ULong.B, dbType);

            TestBigIntForEverythingWorks<bool?>(true, dbType);
            TestBigIntForEverythingWorks<sbyte?>((sbyte)1, dbType);
            TestBigIntForEverythingWorks<byte?>((byte)1, dbType);
            TestBigIntForEverythingWorks<int?>((int)1, dbType);
            TestBigIntForEverythingWorks<uint?>((uint)1, dbType);
            TestBigIntForEverythingWorks<short?>((short)1, dbType);
            TestBigIntForEverythingWorks<ushort?>((ushort)1, dbType);
            TestBigIntForEverythingWorks<long?>((long)1, dbType);
            TestBigIntForEverythingWorks<ulong?>((ulong)1, dbType);
            TestBigIntForEverythingWorks<float?>((float)1, dbType);
            TestBigIntForEverythingWorks<double?>((double)1, dbType);
            TestBigIntForEverythingWorks<decimal?>((decimal)1, dbType);

            TestBigIntForEverythingWorks<LotsOfNumerics.E_Byte?>(LotsOfNumerics.E_Byte.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_SByte?>(LotsOfNumerics.E_SByte.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_Int?>(LotsOfNumerics.E_Int.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_UInt?>(LotsOfNumerics.E_UInt.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_Short?>(LotsOfNumerics.E_Short.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_UShort?>(LotsOfNumerics.E_UShort.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_Long?>(LotsOfNumerics.E_Long.B, dbType);
            TestBigIntForEverythingWorks<LotsOfNumerics.E_ULong?>(LotsOfNumerics.E_ULong.B, dbType);
        }

        private void TestBigIntForEverythingWorks<T>(T expected, string dbType)
        {
            var query = connection.Query<T>("select cast(1 as " + dbType + ")").Single();
            query.IsEqualTo(expected);

            var scalar = connection.ExecuteScalar<T>("select cast(1 as " + dbType + ")");
            scalar.IsEqualTo(expected);
        }

        [Fact]
        public void TestSubsequentQueriesSuccess()
        {
            var data0 = connection.Query<Fooz0>("select 1 as [Id] where 1 = 0").ToList();
            data0.Count.IsEqualTo(0);

            var data1 = connection.Query<Fooz1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ToList();
            data1.Count.IsEqualTo(0);

            var data2 = connection.Query<Fooz2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ToList();
            data2.Count.IsEqualTo(0);

            data0 = connection.Query<Fooz0>("select 1 as [Id] where 1 = 0").ToList();
            data0.Count.IsEqualTo(0);

            data1 = connection.Query<Fooz1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ToList();
            data1.Count.IsEqualTo(0);

            data2 = connection.Query<Fooz2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ToList();
            data2.Count.IsEqualTo(0);
        }
        class Fooz0 { public int Id { get; set; } }
        class Fooz1 { public int Id { get; set; } }
        class Fooz2 { public int Id { get; set; } }


        [Fact]
        public void Issue149_TypeMismatch_SequentialAccess()
        {
            string error;
            Guid guid = Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e");
            try
            {
                var result = connection.Query<Issue149_Person>(@"select @guid as Id", new { guid }).First();
                error = null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            error.IsEqualTo("Error parsing column 0 (Id=cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e - Object)");
        }
        public class Issue149_Person { public string Id { get; set; } }

        public class HazX
        {
            public string X { get; set; }
        }

        [Fact]
        public void Issue178_SqlServer()
        {
            const string sql = @"select count(*) from Issue178";
            try { connection.Execute("drop table Issue178"); }
            catch { }
            try { connection.Execute("create table Issue178(id int not null)"); }
            catch { }
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

        [Fact]
        public void SO29343103_UtcDates()
        {
            const string sql = "select @date";
            var date = DateTime.UtcNow;
            var returned = connection.Query<DateTime>(sql, new { date }).Single();
            var delta = returned - date;
            Assert.IsTrue(delta.TotalMilliseconds >= -10 && delta.TotalMilliseconds <= 10);
        }
        
        public void Issue261_Decimals()
        {
            var parameters = new DynamicParameters();
            parameters.Add("c", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 10, scale: 5);
            connection.Execute("create proc #Issue261 @c decimal(10,5) OUTPUT as begin set @c=11.884 end");
            connection.Execute("#Issue261", parameters, commandType: CommandType.StoredProcedure);
            var c = parameters.Get<Decimal>("c");
            c.IsEqualTo(11.884M);
        }
        public void Issue261_Decimals_ADONET_SetViaBaseClass()
        {
            Issue261_Decimals_ADONET(true);
        }

        [Fact]
        public void Issue261_Decimals_ADONET_SetViaConcreteClass()
        {
            Issue261_Decimals_ADONET(false);
        }
        private void Issue261_Decimals_ADONET(bool setPrecisionScaleViaAbstractApi)
        {
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "create proc #Issue261Direct @c decimal(10,5) OUTPUT as begin set @c=11.884 end";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* we don't care that it already exists */ }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "#Issue261Direct";
                var c = cmd.CreateParameter();
                c.ParameterName = "c";
                c.Direction = ParameterDirection.Output;
                c.Value = DBNull.Value;
                c.DbType = DbType.Decimal;

                if (setPrecisionScaleViaAbstractApi)
                {
                    IDbDataParameter baseParam = c;
                    baseParam.Precision = 10;
                    baseParam.Scale = 5;
                }
                else
                {
                    c.Precision = 10;
                    c.Scale = 5;
                }

                cmd.Parameters.Add(c);
                cmd.ExecuteNonQuery();
                decimal value = (decimal)c.Value;
                value.IsEqualTo(11.884M);
            }
        }

        [Fact]
        public void BasicDecimals()
        {
            var c = connection.Query<decimal>("select @c", new { c = 11.884M }).Single();
            c.IsEqualTo(11.884M);
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
                string sql = @" INSERT INTO #XXX
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
        public void Issue327_ReadEmptyProcedureResults()
        {
            // Actually testing for not erroring here on the mapping having no rows to map on in Read<T>();
            connection.Execute(@"
        CREATE PROCEDURE #TestEmptyResults
        AS
        SELECT Top 0 1 Id, 'Bob' Name;
        SELECT Top 0 'Billy Goat' Creature, 'Unicorn' SpiritAnimal, 'Rainbow' Location;");
            var query = connection.QueryMultiple("#TestEmptyResults", commandType: CommandType.StoredProcedure);
            var result1 = query.Read<Issue327_Person>();
            var result2 = query.Read<Issue327_Magic>();
            result1.Any().IsFalse();
            result2.Any().IsFalse();
        }
        class Issue327_Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        class Issue327_Magic
        {
            public string Creature { get; set; }
            public string SpiritAnimal { get; set; }
            public string Location { get; set; }
        }

        [Fact]
        public void Issue295_NullableDateTime_SqlServer()
        {
            TestDateTime(connection);
        }

        private static readonly bool IsAppVeyor = Environment.GetEnvironmentVariable("Appveyor")?.ToUpperInvariant() == "TRUE";
        
#if MYSQL
        private static MySql.Data.MySqlClient.MySqlConnection GetMySqlConnection(bool open = true,
            bool convertZeroDatetime = false, bool allowZeroDatetime = false)
        {
            string cs = IsAppVeyor
                ? "Server=localhost;Database=test;Uid=root;Pwd=Password12!;"
                : "Server=localhost;Database=tests;Uid=test;Pwd=pass;";
            var csb = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(cs);
            csb.AllowZeroDateTime = allowZeroDatetime;
            csb.ConvertZeroDateTime = convertZeroDatetime;
            var conn = new MySql.Data.MySqlClient.MySqlConnection(csb.ConnectionString);
            if (open) conn.Open();
            return conn;
        }

        [FactMySql]
        public void Issue552_SignedUnsignedBooleans()
        {

            using (var conn = GetMySqlConnection(true, false, false))
            {
                conn.Execute(@"
CREATE TEMPORARY TABLE IF NOT EXISTS `bar` (
  `id` INT NOT NULL,
  `bool_val` BOOL NULL,
  PRIMARY KEY (`id`));
  
  truncate table bar;
  insert bar (id, bool_val) values (1, null);
  insert bar (id, bool_val) values (2, 0);
  insert bar (id, bool_val) values (3, 1);
  insert bar (id, bool_val) values (4, null);
  insert bar (id, bool_val) values (5, 1);
  insert bar (id, bool_val) values (6, 0);
  insert bar (id, bool_val) values (7, null);
  insert bar (id, bool_val) values (8, 1);");

                var rows = conn.Query<MySqlHasBool>("select * from bar;").ToDictionary(x => x.Id);

                rows[1].Bool_Val.IsNull();
                rows[2].Bool_Val.IsEqualTo(false);
                rows[3].Bool_Val.IsEqualTo(true);
                rows[4].Bool_Val.IsNull();
                rows[5].Bool_Val.IsEqualTo(true);
                rows[6].Bool_Val.IsEqualTo(false);
                rows[7].Bool_Val.IsNull();
                rows[8].Bool_Val.IsEqualTo(true);
            }
        }
        class MySqlHasBool {
            public int Id {get;set;}
            public bool? Bool_Val {get;set;}
        }
        [FactMySql]
        public void Issue295_NullableDateTime_MySql_Default()
        {
            using (var conn = GetMySqlConnection(true, false, false)) { TestDateTime(connection); }
        }
        [FactMySql]
        public void Issue295_NullableDateTime_MySql_ConvertZeroDatetime()
        {
            using (var conn = GetMySqlConnection(true, true, false)) { TestDateTime(connection); }
        }
        [FactMySql]
        public void Issue295_NullableDateTime_MySql_AllowZeroDatetime()
        {
            using (var conn = GetMySqlConnection(true, false, true)) { TestDateTime(connection); }
        }
        [FactMySql]
        public void Issue295_NullableDateTime_MySql_ConvertAllowZeroDatetime()
        {
            using (var conn = GetMySqlConnection(true, true, true)) { TestDateTime(connection); }
        }
        [FactMySql]
        public void Issue426_SO34439033_DateTimeGainsTicks()
        {
            using (var conn = GetMySqlConnection(true, true, true))
            {
                try { conn.Execute("drop table Issue426_Test"); } catch { }
                try { conn.Execute("create table Issue426_Test (Id int not null, Time time not null)"); } catch { }
                const long ticks = 553440000000;
                const int Id = 426;
               
                var localObj = new Issue426_Test
                {
                    Id = Id,
                    Time = TimeSpan.FromTicks(ticks) // from code example
                };
                conn.Execute("replace into Issue426_Test values (@Id,@Time)", localObj);
                
                var dbObj = conn.Query<Issue426_Test>("select * from Issue426_Test where Id = @id", new { id = Id }).Single();
                dbObj.Id.IsEqualTo(Id);
                dbObj.Time.Value.Ticks.IsEqualTo(ticks);
                
            }
        }

        [FactMySql]
        public void SO36303462_Tinyint_Bools()
        {
            using (var conn = GetMySqlConnection(true, true, true))
            {
                try { conn.Execute("drop table SO36303462_Test"); } catch { }
                conn.Execute("create table SO36303462_Test (Id int not null, IsBold tinyint not null);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (1,1);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (2,0);");
                conn.Execute("insert SO36303462_Test (Id, IsBold) values (3,1);");

                var rows = conn.Query<SO36303462>("select * from SO36303462_Test").ToDictionary(x => x.Id);
                rows.Count.IsEqualTo(3);
                rows[1].IsBold.IsTrue();
                rows[2].IsBold.IsFalse();
                rows[3].IsBold.IsTrue();
            }
        }
        class SO36303462
        {
            public int Id { get; set; }
            public bool IsBold { get; set; }
        }

        public class Issue426_Test
        {
            public long Id { get; set; }
            public TimeSpan? Time { get; set; }
        }
        public class FactMySqlAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }
            private static string unavailable;
            static FactMySqlAttribute()
            {
                try
                {
                    using (GetMySqlConnection(true)) { }
                }
                catch(Exception ex)
                {
                    unavailable = $"MySql is unavailable: {ex.Message}";
                }
            }
        }
#endif
        private void TestDateTime(DbConnection connection)
        {
            DateTime? now = DateTime.UtcNow;
            try { connection.Execute("DROP TABLE Persons"); } catch { }
            connection.Execute(@"CREATE TABLE Persons (id int not null, dob datetime null)");
            connection.Execute(@"INSERT Persons (id, dob) values (@id, @dob)",
                 new { id = 7, dob = (DateTime?)null });
            connection.Execute(@"INSERT Persons (id, dob) values (@id, @dob)",
                 new { id = 42, dob = now });

            var row = connection.QueryFirstOrDefault<Issue295Person>(
                "SELECT id, dob, dob as dob2 FROM Persons WHERE id=@id", new { id = 7});
            row.IsNotNull();
            row.Id.IsEqualTo(7);
            row.DoB.IsNull();
            row.DoB2.IsNull();

            row = connection.QueryFirstOrDefault<Issue295Person>(
                "SELECT id, dob FROM Persons WHERE id=@id", new { id = 42 });
            row.IsNotNull();
            row.Id.IsEqualTo(42);
            row.DoB.Equals(now);
            row.DoB2.Equals(now);
        }
        class Issue295Person
        {
            public int Id { get; set; }
            public DateTime? DoB { get; set; }
            public DateTime? DoB2 { get; set; }
        }
#if FIREBIRD
        [Fact(Skip="Bug in Firebird; a PR to fix it has been submitted")]
        public void Issue178_Firebird()
        {
            var cs = @"initial catalog=localhost:database;user id=SYSDBA;password=masterkey";

            using (var connection = new FbConnection(cs))
            {
                connection.Open();
                const string sql = @"select count(*) from Issue178";
                try { connection.Execute("drop table Issue178"); }
                catch { }
                connection.Execute("create table Issue178(id int not null)");
                connection.Execute("insert into Issue178(id) values(42)");
                // raw ADO.net
                using (var sqlCmd = new FbCommand(sql, connection))
                using (IDataReader reader1 = sqlCmd.ExecuteReader())
                {
                    Assert.IsTrue(reader1.Read());
                    reader1.GetInt32(0).IsEqualTo(1);
                    Assert.IsFalse(reader1.Read());
                    Assert.IsFalse(reader1.NextResult());
                }

                // dapper
                using (var reader2 = connection.ExecuteReader(sql))
                {
                    Assert.IsTrue(reader2.Read());
                    reader2.GetInt32(0).IsEqualTo(1);
                    Assert.IsFalse(reader2.Read());
                    Assert.IsFalse(reader2.NextResult());
                }

                var count = connection.Query<int>(sql).Single();
                count.IsEqualTo(1);
            }
        }
#endif

#if OLEDB
        [Fact]
        public void PseudoPositionalParameters_Simple()
        {
            using (var connection = ConnectViaOledb())
            {
                int value = connection.Query<int>("select ?x? + ?y_2? + ?z?", new { x = 1, y_2 = 3, z = 5, z2 = 24 }).Single();
                value.IsEqualTo(9);
            }
        }

        [Fact]
        public void PseudoPositionalParameters_Dynamic()
        {
            using (var connection = ConnectViaOledb())
            {
                var args = new DynamicParameters();
                args.Add("x", 1);
                args.Add("y_2", 3);
                args.Add("z", 5);
                args.Add("z2", 24);
                int value = connection.Query<int>("select ?x? + ?y_2? + ?z?", args).Single();
                value.IsEqualTo(9);
            }
        }
        
        [Fact]
        public void PseudoPositionalParameters_ReusedParameter()
        {
            using (var connection = ConnectViaOledb())
            {
                try
                {
                    int value = connection.Query<int>("select ?x? + ?y_2? + ?x?", new { x = 1, y_2 = 3 }).Single();
                    Assert.Fail();
                }
                catch (InvalidOperationException ex)
                {
                    ex.Message.IsEqualTo("When passing parameters by position, each parameter can only be referenced once");
                }
            }
        }

        [Fact]
        public void Issue569_SO38527197_PseudoPositionalParameters_In()
        {
            using (var connection = ConnectViaOledb())
            {
                int[] ids = { 1, 2, 5, 7 };
                var list = connection.Query<int>("select * from string_split('1,2,3,4,5',',') where value in ?ids?", new { ids }).AsList();
                list.Sort();
                string.Join(",", list).IsEqualTo("1,2,5");
            }
        }

        [Fact]
        public void PseudoPositional_CanUseVariable()
        {
            using (var connection = ConnectViaOledb())
            {
                int id = 42;
                var row = connection.QuerySingle("declare @id int = ?id?; select @id as [A], @id as [B];", new { id });
                int a = (int)row.A;
                int b = (int)row.B;
                a.IsEqualTo(42);
                b.IsEqualTo(42);
            }
        }
        [Fact]
        public void PseudoPositional_CannotUseParameterMultipleTimes()
        {

            using (var connection = ConnectViaOledb())
            {
                try
                {
                    int id = 42;
                    var row = connection.QuerySingle("select ?id? as [A], ?id? as [B];", new { id });
                    Assert.Fail();
                }
                catch (InvalidOperationException ex) when (ex.Message == "When passing parameters by position, each parameter can only be referenced once")
                {
                    // that's a win
                }
            }
        }

        [Fact]
        public void PseudoPositionalParameters_ExecSingle()
        {
            using (var connection = ConnectViaOledb())
            {
                var data = new { x = 6 };
                connection.Execute("create table #named_single(val int not null)");
                int count = connection.Execute("insert #named_single (val) values (?x?)", data);
                int sum = (int)connection.ExecuteScalar("select sum(val) from #named_single");
                count.IsEqualTo(1);
                sum.IsEqualTo(6);
            }
        }
        
        [Fact]
        public void PseudoPositionalParameters_ExecMulti()
        {
            using (var connection = ConnectViaOledb())
            {
                var data = new[]
                {
                    new { x = 1, y = 1 },
                    new { x = 3, y = 1 },
                    new { x = 6, y = 1 },
                };
                connection.Execute("create table #named_multi(val int not null)");
                int count = connection.Execute("insert #named_multi (val) values (?x?)", data);
                int sum = (int)connection.ExecuteScalar("select sum(val) from #named_multi");
                count.IsEqualTo(3);
                sum.IsEqualTo(10);
            }
        }

        [Fact]
        public void Issue457_NullParameterValues()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ? -- ODBC parameter
SET @customerCode = ? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = ConnectViaOledb())
            {
                DateTime? since = null; // DateTime.Now.Date;
                string code = null;  // "abc";
                var row = connection.QuerySingle(sql, new
                {
                    since,
                    customerCode = code
                });
                var a = (DateTime?)row.Since;
                var b = (string)row.Code;

                a.IsEqualTo(since);
                b.IsEqualTo(code);
            }
        }

        [Fact]
        public void Issue457_NullParameterValues_Named()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ?since? -- ODBC parameter
SET @customerCode = ?customerCode? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = ConnectViaOledb())
            {
                DateTime? since = null; // DateTime.Now.Date;
                string code = null;  // "abc";
                var row = connection.QuerySingle(sql, new
                {
                    since,
                    customerCode = code
                });
                var a = (DateTime?)row.Since;
                var b = (string)row.Code;

                a.IsEqualTo(since);
                b.IsEqualTo(code);
            }
        }
#if ASYNC
        [Fact]
        public async void Issue457_NullParameterValues_MultiAsync()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ? -- ODBC parameter
SET @customerCode = ? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = ConnectViaOledb())
            {
                DateTime? since = null; // DateTime.Now.Date;
                string code = null;  // "abc";
                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    since,
                    customerCode = code
                }))
                {
                    var row = await multi.ReadSingleAsync();
                    var a = (DateTime?)row.Since;
                    var b = (string)row.Code;

                    a.IsEqualTo(since);
                    b.IsEqualTo(code);
                }
            }
        }

        [Fact]
        public async void Issue457_NullParameterValues_MultiAsync_Named()
        {
            const string sql = @"
DECLARE @since DATETIME, @customerCode nvarchar(10)
SET @since = ?since? -- ODBC parameter
SET @customerCode = ?customerCode? -- ODBC parameter

SELECT @since as [Since], @customerCode as [Code]";

            using (var connection = ConnectViaOledb())
            {
                DateTime? since = null; // DateTime.Now.Date;
                string code = null;  // "abc";
                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    since,
                    customerCode = code
                }))
                {
                    var row = await multi.ReadSingleAsync();
                    var a = (DateTime?)row.Since;
                    var b = (string)row.Code;

                    a.IsEqualTo(since);
                    b.IsEqualTo(code);
                }
            }
        }
#endif
#endif

#if !COREFX
        [Fact]
        public void SO29596645_TvpProperty()
        {
            try { connection.Execute("CREATE TYPE SO29596645_ReminderRuleType AS TABLE (id int NOT NULL)"); }
            catch { }
            connection.Execute(@"create proc #SO29596645_Proc (@Id int, @Rules SO29596645_ReminderRuleType READONLY)
                                as begin select @Id + ISNULL((select sum(id) from @Rules), 0); end");
            var obj = new SO29596645_OrganisationDTO();
            int val = connection.Query<int>("#SO29596645_Proc", obj.Rules, commandType: CommandType.StoredProcedure).Single();

            // 4 + 9 + 7 = 20
            val.IsEqualTo(20);

        }
        class SO29596645_RuleTableValuedParameters : Dapper.SqlMapper.IDynamicParameters {
            private string parameterName;

            public SO29596645_RuleTableValuedParameters(string parameterName)
            {
                this.parameterName = parameterName;
            }


            public void AddParameters(IDbCommand command, Dapper.SqlMapper.Identity identity)
            {
                Console.WriteLine("> AddParameters");
                SqlCommand lazy = (SqlCommand)command;
                lazy.Parameters.AddWithValue("Id", 7);
                DataTable table = new DataTable {
                    Columns = {{"Id", typeof(int)}},
                    Rows = {{4}, {9}}
                };
                lazy.Parameters.AddWithValue("Rules", table);
                Console.WriteLine("< AddParameters");
            }
        }
        class SO29596645_OrganisationDTO
        {
            public SO29596645_RuleTableValuedParameters Rules { get; private set; }

            public SO29596645_OrganisationDTO()
            {
                Rules = new SO29596645_RuleTableValuedParameters("@Rules");
            }
        }
#endif
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
        
        [FactPostgresqlAttribute]
        public void TestPostgresqlArrayParameters()
        {
            using (var conn = OpenPostgresqlConnection())
            {
                IDbTransaction transaction = conn.BeginTransaction();
                conn.Execute("create table tcat ( id serial not null, breed character varying(20) not null, name character varying (20) not null);");
                conn.Execute("insert into tcat(breed, name) values(:breed, :name) ", Cats);

                var r = conn.Query<Cat>("select * from tcat where id=any(:catids)", new { catids = new[] { 1, 3, 5 } });
                r.Count().IsEqualTo(3);
                r.Count(c => c.Id == 1).IsEqualTo(1);
                r.Count(c => c.Id == 3).IsEqualTo(1);
                r.Count(c => c.Id == 5).IsEqualTo(1);
                transaction.Rollback();
            }
        }
        static NpgsqlConnection OpenPostgresqlConnection()
        {
            string cs = IsAppVeyor
                ? "Server=localhost;Port=5432;User Id=postgres;Password=Password12!;Database=test"
                : "Server=localhost;Port=5432;User Id=dappertest;Password=dapperpass;Database=dappertest"; // ;Encoding = UNICODE
            var conn = new NpgsqlConnection(cs);
            conn.Open();
            return conn;
        }
        public class FactPostgresqlAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }
            private static string unavailable;
            static FactPostgresqlAttribute()
            {
                try
                {
                    using (OpenPostgresqlConnection()) { }
                }
                catch (Exception ex)
                {
                    unavailable = $"Postgresql is unavailable: {ex.Message}";
                }
            }
        }
#endif

#if ASYNC
        [Fact]
        public async void SO35470588_WrongValuePidValue()
        {
            // nuke, rebuild, and populate the table
            try { connection.Execute("drop table TPTable"); } catch { }
            connection.Execute(@"
create table TPTable (Pid int not null primary key identity(1,1), Value int not null);
insert TPTable (Value) values (2), (568)");

            // fetch the data using the query in the question, then force to a dictionary
            var rows = (await connection.QueryAsync<TPTable>("select * from TPTable"))
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
#endif

#if SQLITE
        [FactSqlite]
        public void Issue466_SqliteHatesOptimizations()
        {
            using (var connection = GetSqliteConnection())
            {
                SqlMapper.ResetTypeHandlers();
                var row = connection.Query<HazNameId>("select 42 as Id").First();
                row.Id.IsEqualTo(42);
                row = connection.Query<HazNameId>("select 42 as Id").First();
                row.Id.IsEqualTo(42);

                SqlMapper.ResetTypeHandlers();
                row = connection.QueryFirst<HazNameId>("select 42 as Id");
                row.Id.IsEqualTo(42);
                row = connection.QueryFirst<HazNameId>("select 42 as Id");
                row.Id.IsEqualTo(42);
            }
        }

#if ASYNC
        [FactSqlite]
        public async Task Issue466_SqliteHatesOptimizations_Async()
        {
            using (var connection = GetSqliteConnection())
            {
                SqlMapper.ResetTypeHandlers();
                var row = (await connection.QueryAsync<HazNameId>("select 42 as Id")).First();
                row.Id.IsEqualTo(42);
                row = (await connection.QueryAsync<HazNameId>("select 42 as Id")).First();
                row.Id.IsEqualTo(42);

                SqlMapper.ResetTypeHandlers();
                row = await connection.QueryFirstAsync<HazNameId>("select 42 as Id");
                row.Id.IsEqualTo(42);
                row = await connection.QueryFirstAsync<HazNameId>("select 42 as Id");
                row.Id.IsEqualTo(42);
            }
        }
#endif

        [FactSqlite]
        public void Isse467_SqliteLikesParametersWithPrefix()
        {
            Isse467_SqliteParameterNaming(true);
        }
        [FactSqlite]
        public void Isse467_SqliteLikesParametersWithoutPrefix()
        { // see issue 375 / 467; note: fixed from RC2 onwards
            Isse467_SqliteParameterNaming(false);
        }
        private void Isse467_SqliteParameterNaming(bool prefix)
        {
            using (var connection = GetSqliteConnection())
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select @foo";
#if NET40 || NET45
                const DbType type = DbType.Int32;
#else
                const SqliteType type = SqliteType.Integer;
#endif
                cmd.Parameters.Add(prefix ? "@foo" : "foo", type).Value = 42;
                var i = Convert.ToInt32(cmd.ExecuteScalar());
                i.IsEqualTo(42);
            }
        }
        public class FactSqliteAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }
            private static string unavailable;
            static FactSqliteAttribute()
            {
                try
                {
                    using (GetSqliteConnection()) { }
                }
                catch (Exception ex)
                {
                    unavailable = $"Sqlite is unavailable: {ex.Message}";
                }
            }
        }
        protected static SqliteConnection GetSqliteConnection(bool open = true)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            if (open) connection.Open();
            return connection;
        }
#endif
        [Fact]
        public void GetOnlyProperties()
        {
            var obj = connection.QuerySingle<HazGetOnly>("select 42 as [Id], 'def' as [Name];");
            obj.Id.IsEqualTo(42);
            obj.Name.IsEqualTo("def");
        }
        class HazGetOnly
        {
            public int Id { get; }
            public string Name { get; } = "abc";
        }
    }
}
