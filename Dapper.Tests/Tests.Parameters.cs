using Dapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Dynamic;
using System.Linq;
using Xunit;

#if ENTITY_FRAMEWORK
using System.Data.Entity.Spatial;
using Microsoft.SqlServer.Types;
#endif

namespace Dapper.Tests
{
    public partial class TestSuite
    {
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


#if !COREFX
        [Fact]
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

        // SQL Server specific test to demonstrate TVP 
        [Fact]
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
        
        [Fact]
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

        [Fact]
        public void DataTableParameters()
        {
            try { connection.Execute("drop proc #DataTableParameters"); }
            catch { }
            try { connection.Execute("drop table #DataTableParameters"); }
            catch { }
            try { connection.Execute("drop type MyTVPType"); }
            catch { }
            connection.Execute("create type MyTVPType as table (id int)");
            connection.Execute("create proc #DataTableParameters @ids MyTVPType readonly as select count(1) from @ids");

            var table = new DataTable { Columns = { { "id", typeof(int) } }, Rows = { { 1 }, { 2 }, { 3 } } };

            int count = connection.Query<int>("#DataTableParameters", new { ids = table.AsTableValuedParameter() }, commandType: CommandType.StoredProcedure).First();
            count.IsEqualTo(3);

            count = connection.Query<int>("select count(1) from @ids", new { ids = table.AsTableValuedParameter("MyTVPType") }).First();
            count.IsEqualTo(3);

            try
            {
                connection.Query<int>("select count(1) from @ids", new { ids = table.AsTableValuedParameter() }).First();
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                ex.Message.Equals("The table type parameter 'ids' must have a valid type name.");
            }
        }
        
        [Fact]
        public void SO29533765_DataTableParametersViaDynamicParameters()
        {
            try { connection.Execute("drop proc #DataTableParameters"); } catch { }
            try { connection.Execute("drop table #DataTableParameters"); } catch { }
            try { connection.Execute("drop type MyTVPType"); } catch { }
            connection.Execute("create type MyTVPType as table (id int)");
            connection.Execute("create proc #DataTableParameters @ids MyTVPType readonly as select count(1) from @ids");

            var table = new DataTable { TableName="MyTVPType", Columns = { { "id", typeof(int) } }, Rows = { { 1 }, { 2 }, { 3 } } };
            table.SetTypeName(table.TableName); // per SO29533765
            IDictionary<string, object> args = new Dictionary<string, object>();
            args.Add("ids", table);
            int count = connection.Query<int>("#DataTableParameters", args, commandType: CommandType.StoredProcedure).First();
            count.IsEqualTo(3);

            count = connection.Query<int>("select count(1) from @ids", args).First();
            count.IsEqualTo(3);
        }
        
        [Fact]
        public void SO26468710_InWithTVPs()
        {
            // this is just to make it re-runnable; normally you only do this once
            try { connection.Execute("drop type MyIdList"); }
            catch { }
            connection.Execute("create type MyIdList as table(id int);");

            DataTable ids = new DataTable
            {
                Columns = { { "id", typeof(int) } },
                Rows = { { 1 }, { 3 }, { 5 } }
            };
            ids.SetTypeName("MyIdList");
            int sum = connection.Query<int>(@"
            declare @tmp table(id int not null);
            insert @tmp (id) values(1), (2), (3), (4), (5), (6), (7);
            select * from @tmp t inner join @ids i on i.id = t.id", new { ids }).Sum();
            sum.IsEqualTo(9);
        }
        
        [Fact]
        public void DataTableParametersWithExtendedProperty()
        {
            try { connection.Execute("drop proc #DataTableParameters"); }
            catch { }
            try { connection.Execute("drop table #DataTableParameters"); }
            catch { }
            try { connection.Execute("drop type MyTVPType"); }
            catch { }
            connection.Execute("create type MyTVPType as table (id int)");
            connection.Execute("create proc #DataTableParameters @ids MyTVPType readonly as select count(1) from @ids");

            var table = new DataTable { Columns = { { "id", typeof(int) } }, Rows = { { 1 }, { 2 }, { 3 } } };
            table.SetTypeName("MyTVPType"); // <== extended metadata
            int count = connection.Query<int>("#DataTableParameters", new { ids = table }, commandType: CommandType.StoredProcedure).First();
            count.IsEqualTo(3);

            count = connection.Query<int>("select count(1) from @ids", new { ids = table }).First();
            count.IsEqualTo(3);

            try
            {
                connection.Query<int>("select count(1) from @ids", new { ids = table }).First();
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                ex.Message.Equals("The table type parameter 'ids' must have a valid type name.");
            }
        }
#endif

#if !COREFX
        [Fact]
        public void SupportInit()
        {
            var obj = connection.Query<WithInit>("select 'abc' as Value").Single();
            obj.Value.Equals("abc");
            obj.Flags.Equals(31);
        }
#endif

#if ENTITY_FRAMEWORK
        class HazGeo
        {
            public int Id { get; set; }
            public DbGeography Geo { get; set; }
            public DbGeometry Geometry { get; set; }
        }
        class HazSqlGeo
        {
            public int Id { get; set; }
            public SqlGeography Geo { get; set; }
            public SqlGeometry Geometry { get; set; }
        }
        
        [Fact]
        public void DBGeography_SO24405645_SO24402424()
        {
            Dapper.EntityFramework.Handlers.Register();

            connection.Execute("create table #Geo (id int, geo geography, geometry geometry)");

            var obj = new HazGeo
            {
                Id = 1,
                Geo = DbGeography.LineFromText("LINESTRING(-122.360 47.656, -122.343 47.656 )", 4326),
                Geometry = DbGeometry.LineFromText("LINESTRING (100 100, 20 180, 180 180)", 0)
            };
            connection.Execute("insert #Geo(id, geo, geometry) values (@Id, @Geo, @Geometry)", obj);
            var row = connection.Query<HazGeo>("select * from #Geo where id=1").SingleOrDefault();
            row.IsNotNull();
            row.Id.IsEqualTo(1);
            row.Geo.IsNotNull();
            row.Geometry.IsNotNull();
        }
        
        [Fact]
        public void SqlGeography_SO25538154()
        {
            Dapper.SqlMapper.ResetTypeHandlers();
            connection.Execute("create table #SqlGeo (id int, geo geography, geometry geometry)");

            var obj = new HazSqlGeo
            {
                Id = 1,
                Geo = SqlGeography.STLineFromText(new SqlChars(new SqlString("LINESTRING(-122.360 47.656, -122.343 47.656 )")), 4326),
                Geometry = SqlGeometry.STLineFromText(new SqlChars(new SqlString("LINESTRING (100 100, 20 180, 180 180)")), 0)
            };
            connection.Execute("insert #SqlGeo(id, geo, geometry) values (@Id, @Geo, @Geometry)", obj);
            var row = connection.Query<HazSqlGeo>("select * from #SqlGeo where id=1").SingleOrDefault();
            row.IsNotNull();
            row.Id.IsEqualTo(1);
            row.Geo.IsNotNull();
            row.Geometry.IsNotNull();
        }
        
        [Fact]
        public void NullableSqlGeometry()
        {
            Dapper.SqlMapper.ResetTypeHandlers();
            connection.Execute("create table #SqlNullableGeo (id int, geometry geometry null)");

            var obj = new HazSqlGeo
            {
                Id = 1,
                Geometry = null
            };
            connection.Execute("insert #SqlNullableGeo(id, geometry) values (@Id, @Geometry)", obj);
            var row = connection.Query<HazSqlGeo>("select * from #SqlNullableGeo where id=1").SingleOrDefault();
            row.IsNotNull();
            row.Id.IsEqualTo(1);
            row.Geometry.IsNull();
        }
        
        [Fact]
        public void SqlHierarchyId_SO18888911()
        {
            Dapper.SqlMapper.ResetTypeHandlers();
            var row = connection.Query<HazSqlHierarchy>("select 3 as [Id], hierarchyid::Parse('/1/2/3/') as [Path]").Single();
            row.Id.Equals(3);
            row.Path.IsNotNull();

            var val = connection.Query<SqlHierarchyId>("select @Path", row).Single();
            val.IsNotNull();
        }

        public class HazSqlHierarchy
        {
            public int Id { get; set; }
            public SqlHierarchyId Path { get; set; }
        }

#endif

#if OLEDB

        // see http://stackoverflow.com/q/18847510/23354
        [Fact]
        public void TestOleDbParameters()
        {
            using (var conn = ConnectViaOledb())
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

        System.Data.OleDb.OleDbConnection ConnectViaOledb()
        {
            var conn = new System.Data.OleDb.OleDbConnection(OleDbConnectionString);
            conn.Open();
            return conn;
        }
#endif

        [Fact]
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

        [Fact]
        public void TestAppendingADictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                {"A", 1},
                {"B", "two"}
            };

            DynamicParameters p = new DynamicParameters();
            p.AddDynamicParams(dictionary);

            var result = connection.Query("select @A a, @B b", p).Single();

            ((int)result.a).IsEqualTo(1);
            ((string)result.b).IsEqualTo("two");
        }

        [Fact]
        public void TestAppendingAnExpandoObject()
        {
            dynamic expando = new ExpandoObject();
            expando.A = 1;
            expando.B = "two";

            DynamicParameters p = new DynamicParameters();
            p.AddDynamicParams(expando);

            var result = connection.Query("select @A a, @B b", p).Single();

            ((int)result.a).IsEqualTo(1);
            ((string)result.b).IsEqualTo("two");
        }

        [Fact]
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

        [Fact]
        public void TestAppendingAListAsDictionary()
        {
            DynamicParameters p = new DynamicParameters();
            var list = new int[] { 1, 2, 3 };
            var args = new Dictionary<string, object> { { "ids", list } };
            p.AddDynamicParams(args);

            var result = connection.Query<int>("select * from (select 1 A union all select 2 union all select 3) X where A in @ids", p).ToList();

            result[0].IsEqualTo(1);
            result[1].IsEqualTo(2);
            result[2].IsEqualTo(3);
        }

        [Fact]
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

        [Fact]
        public void ParameterizedInWithOptimizeHint()
        {
            const string sql = @"
select count(1)
from(
    select 1 as x
    union all select 2
    union all select 5) y
where y.x in @vals
option (optimize for (@vals unKnoWn))";
            int count = connection.Query<int>(sql, new { vals = new[] { 1, 2, 3, 4 } }).Single();
            count.IsEqualTo(2);

            count = connection.Query<int>(sql, new { vals = new[] { 1 } }).Single();
            count.IsEqualTo(1);

            count = connection.Query<int>(sql, new { vals = new int[0] }).Single();
            count.IsEqualTo(0);
        }

        [Fact]
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

        [Fact]
        public void TestUniqueIdentifier()
        {
            var guid = Guid.NewGuid();
            var result = connection.Query<Guid>("declare @foo uniqueidentifier set @foo = @guid select @foo", new { guid }).Single();
            result.IsEqualTo(guid);
        }

        [Fact]
        public void TestNullableUniqueIdentifierNonNull()
        {
            Guid? guid = Guid.NewGuid();
            var result = connection.Query<Guid?>("declare @foo uniqueidentifier set @foo = @guid select @foo", new { guid }).Single();
            result.IsEqualTo(guid);
        }

        [Fact]
        public void TestNullableUniqueIdentifierNull()
        {
            Guid? guid = null;
            var result = connection.Query<Guid?>("declare @foo uniqueidentifier set @foo = @guid select @foo", new { guid }).Single();
            result.IsEqualTo(guid);
        }

        [Fact]
        public void TestSupportForDynamicParameters()
        {
            var p = new DynamicParameters();
            p.Add("name", "bob");
            p.Add("age", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Query<string>("set @age = 11 select @name", p).First().IsEqualTo("bob");

            p.Get<int>("age").IsEqualTo(11);
        }

        [Fact]
        public void TestSupportForDynamicParametersOutputExpressions()
        {
            var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

            var p = new DynamicParameters(bob);
            p.Output(bob, b => b.PersonId);
            p.Output(bob, b => b.Occupation);
            p.Output(bob, b => b.NumberOfLegs);
            p.Output(bob, b => b.Address.Name);
            p.Output(bob, b => b.Address.PersonId);

            connection.Execute(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId", p);

            bob.Occupation.IsEqualTo("grillmaster");
            bob.PersonId.IsEqualTo(2);
            bob.NumberOfLegs.IsEqualTo(1);
            bob.Address.Name.IsEqualTo("bobs burgers");
            bob.Address.PersonId.IsEqualTo(2);
        }

        [Fact]
        public void TestSupportForDynamicParametersOutputExpressions_Scalar()
        {
            using (var connection = GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = (int)connection.ExecuteScalar(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p);

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }

        [Fact]
        public void TestSupportForDynamicParametersOutputExpressions_Query_Buffered()
        {
            using (var connection = GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = connection.Query<int>(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, buffered: true).Single();

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }

        [Fact]
        public void TestSupportForDynamicParametersOutputExpressions_Query_NonBuffered()
        {
            using (var connection = GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                var result = connection.Query<int>(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
SET @AddressPersonId = @PersonId
select 42", p, buffered: false).Single();

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                result.IsEqualTo(42);
            }
        }

        [Fact]
        public void TestSupportForDynamicParametersOutputExpressions_QueryMultiple()
        {
            using (var connection = GetOpenConnection())
            {
                var bob = new Person { Name = "bob", PersonId = 1, Address = new Address { PersonId = 2 } };

                var p = new DynamicParameters(bob);
                p.Output(bob, b => b.PersonId);
                p.Output(bob, b => b.Occupation);
                p.Output(bob, b => b.NumberOfLegs);
                p.Output(bob, b => b.Address.Name);
                p.Output(bob, b => b.Address.PersonId);

                int x, y;
                using (var multi = connection.QueryMultiple(@"
SET @Occupation = 'grillmaster' 
SET @PersonId = @PersonId + 1 
SET @NumberOfLegs = @NumberOfLegs - 1
SET @AddressName = 'bobs burgers'
select 42
select 17
SET @AddressPersonId = @PersonId", p))
                {
                    x = multi.Read<int>().Single();
                    y = multi.Read<int>().Single();
                }

                bob.Occupation.IsEqualTo("grillmaster");
                bob.PersonId.IsEqualTo(2);
                bob.NumberOfLegs.IsEqualTo(1);
                bob.Address.Name.IsEqualTo("bobs burgers");
                bob.Address.PersonId.IsEqualTo(2);
                x.IsEqualTo(42);
                y.IsEqualTo(17);
            }
        }

        [Fact]
        public void TestSupportForExpandoObjectParameters()
        {
            dynamic p = new ExpandoObject();
            p.name = "bob";
            object parameters = p;
            string result = connection.Query<string>("select @name", parameters).First();
            result.IsEqualTo("bob");
        }

        [Fact]
        public void SO25069578_DynamicParams_Procs()
        {
            var parameters = new DynamicParameters();
            parameters.Add("foo", "bar");
            // parameters = new DynamicParameters(parameters);
            try { connection.Execute("drop proc SO25069578"); }
            catch { }
            connection.Execute("create proc SO25069578 @foo nvarchar(max) as select @foo as [X]");
            var tran = connection.BeginTransaction(); // gist used transaction; behaves the same either way, though
            var row = connection.Query<HazX>("SO25069578", parameters,
                commandType: CommandType.StoredProcedure, transaction: tran).Single();
            tran.Rollback();
            row.X.IsEqualTo("bar");
        }

        [Fact]
        public void SO25297173_DynamicIn()
        {
            var query = @"
declare @table table(value int not null);
insert @table values(1);
insert @table values(2);
insert @table values(3);
insert @table values(4);
insert @table values(5);
insert @table values(6);
insert @table values(7);
SELECT value FROM @table WHERE value IN @myIds";
            var queryParams = new Dictionary<string, object> {
                { "myIds", new [] { 5, 6 } }
            };

            var dynamicParams = new DynamicParameters(queryParams);
            List<int> result = connection.Query<int>(query, dynamicParams).ToList();
            result.Count.IsEqualTo(2);
            result.Contains(5).IsTrue();
            result.Contains(6).IsTrue();
        }

        [Fact]
        public void Test_AddDynamicParametersRepeatedShouldWork()
        {
            var args = new DynamicParameters();
            args.AddDynamicParams(new { Foo = 123 });
            args.AddDynamicParams(new { Foo = 123 });
            int i = connection.Query<int>("select @Foo", args).Single();
            i.IsEqualTo(123);
        }

        [Fact]
        public void AllowIDictionaryParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "param1", 0 }
            };

            connection.Query("SELECT @param1", parameters);
        }

        [Fact]
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
        public class ParameterWithIndexer
        {
            public int A { get; set; }
            public virtual string this[string columnName]
            {
                get { return null; }
                set { }
            }
        }

        [Fact]
        public void TestMultipleParametersWithIndexer()
        {
            var order = connection.Query<MultipleParametersWithIndexer>("select 1 A,2 B").First();

            order.A.IsEqualTo(1);
            order.B.IsEqualTo(2);
        }
        public class MultipleParametersWithIndexer : MultipleParametersWithIndexerDeclaringType
        {
            public int A { get; set; }
        }
        public class MultipleParametersWithIndexerDeclaringType
        {
            public object this[object field] { get { return null; } set { } }
            public object this[object field, int index] { get { return null; } set { } }
            public int B { get; set; }
        }

        [Fact]
        public void Issue182_BindDynamicObjectParametersAndColumns()
        {
            connection.Execute("create table #Dyno ([Id] uniqueidentifier primary key, [Name] nvarchar(50) not null, [Foo] bigint not null);");

            var guid = Guid.NewGuid();
            var orig = new Dyno { Name = "T Rex", Id = guid, Foo = 123L };
            var result = connection.Execute("insert into #Dyno ([Id], [Name], [Foo]) values (@Id, @Name, @Foo);", orig);

            var fromDb = connection.Query<Dyno>("select * from #Dyno where Id=@Id", orig).Single();
            ((Guid)fromDb.Id).IsEqualTo(guid);
            fromDb.Name.IsEqualTo("T Rex");
            ((long)fromDb.Foo).IsEqualTo(123L);
        }
        public class Dyno
        {
            public dynamic Id { get; set; }
            public string Name { get; set; }

            public object Foo { get; set; }
        }

        [Fact]
        public void Issue151_ExpandoObjectArgsQuery()
        {
            dynamic args = new ExpandoObject();
            args.Id = 123;
            args.Name = "abc";

            var row = connection.Query("select @Id as [Id], @Name as [Name]", (object)args).Single();
            ((int)row.Id).Equals(123);
            ((string)row.Name).Equals("abc");
        }

        [Fact]
        public void Issue151_ExpandoObjectArgsExec()
        {
            dynamic args = new ExpandoObject();
            args.Id = 123;
            args.Name = "abc";
            connection.Execute("create table #issue151 (Id int not null, Name nvarchar(20) not null)");
            connection.Execute("insert #issue151 values(@Id, @Name)", (object)args).IsEqualTo(1);
            var row = connection.Query("select Id, Name from #issue151").Single();
            ((int)row.Id).Equals(123);
            ((string)row.Name).Equals("abc");
        }

        [Fact]
        public void Issue192_InParameterWorksWithSimilarNames()
        {
            var rows = connection.Query(@"
declare @Issue192 table (
    Field INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    Field_1 INT NOT NULL);
insert @Issue192(Field_1) values (1), (2), (3);
SELECT * FROM @Issue192 WHERE Field IN @Field AND Field_1 IN @Field_1",
    new { Field = new[] { 1, 2 }, Field_1 = new[] { 2, 3 } }).Single();
            ((int)rows.Field).IsEqualTo(2);
            ((int)rows.Field_1).IsEqualTo(2);
        }

        [Fact]
        public void Issue192_InParameterWorksWithSimilarNamesWithUnicode()
        {
            var rows = connection.Query(@"
declare @Issue192 table (
    Field INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    Field_1 INT NOT NULL);
insert @Issue192(Field_1) values (1), (2), (3);
SELECT * FROM @Issue192 WHERE Field IN @µ AND Field_1 IN @µµ",
    new { µ = new[] { 1, 2 }, µµ = new[] { 2, 3 } }).Single();
            ((int)rows.Field).IsEqualTo(2);
            ((int)rows.Field_1).IsEqualTo(2);
        }

        [FactUnlessCaseSensitiveDatabase]
        public void Issue220_InParameterCanBeSpecifiedInAnyCase()
        {
            // note this might fail if your database server is case-sensitive
            connection.Query<int>("select * from (select 1 as Id) as X where Id in @ids", new { Ids = new[] { 1 } })
                          .IsSequenceEqualTo(new[] { 1 });
        }

        [Fact]
        public void SO30156367_DynamicParamsWithoutExec()
        {
            var dbParams = new DynamicParameters();
            dbParams.Add("Field1", 1);
            var value = dbParams.Get<int>("Field1");
            value.IsEqualTo(1);
        }

        [Fact]
        public void RunAllStringSplitTestsDisabled()
        {
            RunAllStringSplitTests(-1, 1500);
        }
        [FactRequiredCompatibilityLevel(FactRequiredCompatibilityLevelAttribute.SqlServer2016)]
        public void RunAllStringSplitTestsEnabled()
        {
            RunAllStringSplitTests(10, 4500);
        }
        private void RunAllStringSplitTests(int stringSplit, int max = 150)
        {
            int oldVal = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = stringSplit;
                try { connection.Execute("drop table #splits"); } catch { }
                int count = connection.QuerySingle<int>("create table #splits (i int not null);"
                    + string.Concat(Enumerable.Range(-max, max * 3).Select(i => $"insert #splits (i) values ({i});"))
                    + "select count(1) from #splits");
                count.IsEqualTo(3 * max);

                for (int i = 0; i < max; Incr(ref i))
                {
                    try
                    {
                        var vals = Enumerable.Range(1, i);
                        var list = connection.Query<int>("select i from #splits where i in @vals", new { vals }).AsList();
                        list.Count.IsEqualTo(i);
                        list.Sum().IsEqualTo(vals.Sum());
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error when i={i}: {ex.Message}", ex);
                    }
                }
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = oldVal;
            }
        }
        static void Incr(ref int i)
        {
            if (i <= 15) i++;
            else if (i <= 80) i += 5;
            else if (i <= 200) i += 10;
            else if (i <= 1000) i += 50;
            else i += 100;
        }

        [Fact]
        public void Issue601_InternationalParameterNamesWork()
        {
            // regular parameter
            var result = connection.QuerySingle<int>("select @æøå٦", new { æøå٦ = 42 });
            result.IsEqualTo(42);

#if OLEDB
            // pseudo-positional
            using (var connection = ConnectViaOledb())
            {
                int value = connection.QuerySingle<int>("select ?æøå٦?", new { æøå٦ = 42 });
            }
#endif
        }

    }
}
