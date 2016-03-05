using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

using Dapper.Tests.Linq2Sql;
using Dapper.Contrib.Extensions;


#if SOMA
using Soma.Core;
#endif
#if NHIBERNATE
using NHibernate.Criterion;
using NHibernate.Linq;
using Dapper.Tests.NHibernate;
#endif
#if LINQ2SQL
using System.Data.Linq;
#endif
#if MASSIVE
using Massive;
#endif
#if ORMLITE
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.OrmLite.Converters;
using ServiceStack.OrmLite.Dapper;
#endif
#if BLTOOLKIT
using BLToolkit.Data;
#endif
#if ENTITY_FRAMEWORK
using Dapper.Tests.EntityFramework;
#endif
#if SUSANOO
using Susanoo;
#endif


namespace Dapper.Tests
{
    public class PerformanceTests
    {
        class Test
        {
            public static Test Create(Action<int> iteration, string name)
            {
                return new Test {Iteration = iteration, Name = name };
            }

            public Action<int> Iteration { get; set; }
            public string Name { get; set; }
            public Stopwatch Watch { get; set; }
        }

        class Tests : List<Test>
        {
            public void Add(Action<int> iteration, string name)
            {
                Add(Test.Create(iteration, name));
            }

            public void Run(int iterations)
            { 
                // warmup 
                foreach (var test in this)
                {
                    test.Iteration(iterations + 1);
                    test.Watch = new Stopwatch();
                    test.Watch.Reset();
                }

                var rand = new Random();
                for (int i = 1; i <= iterations; i++)
                {
                    foreach (var test in this.OrderBy(ignore => rand.Next()))
                    {
                        test.Watch.Start();
                        test.Iteration(i);
                        test.Watch.Stop();
                    }
                }

                foreach (var test in this.OrderBy(t => t.Watch.ElapsedMilliseconds))
                {
                    Console.WriteLine(test.Name + " took " + test.Watch.ElapsedMilliseconds + "ms");
                }
            }
        }
#if LINQ2SQL
        static DataClassesDataContext GetL2SContext(SqlConnection connection)
        {
            return new DataClassesDataContext(connection);
        }
#endif

#if SOMA

        internal class SomaConfig : Soma.Core.MsSqlConfig
		{
			public override string ConnectionString => TestSuite.ConnectionString;

            public override Action<PreparedStatement> Logger
            {
                get { return noOp; }
            }
            static readonly Action<PreparedStatement> noOp = x => { };
		}
#endif
        static void Try(Action action, string blame)
        {
            try
            {
                action();
            } catch(Exception ex)
            {
                Console.Error.WriteLine($"{blame}: {ex.Message}");
            }
        }
        public void Run(int iterations)
        {
            using (var connection = TestSuite.GetOpenConnection())
            {
                var tests = new Tests();
#if LINQ2SQL
                Try(() =>
                {
                    var l2scontext1 = GetL2SContext(connection);
                    tests.Add(id => l2scontext1.Posts.First(p => p.Id == id), "Linq 2 SQL");

                    var l2scontext2 = GetL2SContext(connection);
                    var compiledGetPost = CompiledQuery.Compile((Linq2Sql.DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));
                    tests.Add(id => compiledGetPost(l2scontext2, id), "Linq 2 SQL Compiled");

                    var l2scontext3 = GetL2SContext(connection);
                    tests.Add(id => l2scontext3.ExecuteQuery<Post>("select * from Posts where Id = {0}", id).First(), "Linq 2 SQL ExecuteQuery");
                }, "LINQ-to-SQL");
#endif

#if ENTITY_FRAMEWORK
                Try(() =>
                {
                    var entityContext = new EFContext(connection);
                    tests.Add(id => entityContext.Posts.First(p => p.Id == id), "Entity framework");


                    var entityContext2 = new EFContext(connection);
                    tests.Add(id => entityContext2.Database.SqlQuery<Post>("select * from Posts where Id = {0}", id).First(), "Entity framework SqlQuery");

                    //var entityContext3 = new EFContext(connection);
                    //tests.Add(id => entityFrameworkCompiled(entityContext3, id), "Entity framework CompiledQuery");

                    //var entityContext4 = new EFContext(connection);
                    //tests.Add(id => entityContext4.Posts.Where("it.Id = @id", new System.Data.Objects.ObjectParameter("id", id)).First(), "Entity framework ESQL");

                    var entityContext5 = new EFContext(connection);
                    tests.Add(id => entityContext5.Posts.AsNoTracking().First(p => p.Id == id), "Entity framework No Tracking");
                }, "Entity Framework");
#endif
                Try(() =>
                {
                    var mapperConnection = TestSuite.GetOpenConnection();
                    tests.Add(id => mapperConnection.Query<Post>("select * from Posts where Id = @Id", new { Id = id }, buffered: true).First(), "Mapper Query (buffered)");
                    tests.Add(id => mapperConnection.Query<Post>("select * from Posts where Id = @Id", new { Id = id }, buffered: false).First(), "Mapper Query (non-buffered)");
                    tests.Add(id => mapperConnection.QueryFirstOrDefault<Post>("select * from Posts where Id = @Id", new { Id = id }), "Mapper QueryFirstOrDefault");


                    var mapperConnection2 = TestSuite.GetOpenConnection();
                    tests.Add(id => mapperConnection2.Query("select * from Posts where Id = @Id", new { Id = id }, buffered: true).First(), "Dynamic Mapper Query (buffered)");
                    tests.Add(id => mapperConnection2.Query("select * from Posts where Id = @Id", new { Id = id }, buffered: false).First(), "Dynamic Mapper Query (non-buffered)");
                    tests.Add(id => mapperConnection2.QueryFirstOrDefault("select * from Posts where Id = @Id", new { Id = id }), "Dynamic Mapper QueryQueryFirstOrDefault");

                    // dapper.contrib
                    var mapperConnection3 = TestSuite.GetOpenConnection();
                    tests.Add(id => mapperConnection3.Get<Post>(id), "Dapper.Contrib");
                }, "Dapper");

#if MASSIVE
                Try(() =>
                {
                    // massive
                    var massiveModel = new DynamicModel(TestSuite.ConnectionString);
                    var massiveConnection = TestSuite.GetOpenConnection();
                    tests.Add(id => massiveModel.Query("select * from Posts where Id = @0", massiveConnection, id).First(), "Dynamic Massive ORM Query");
                }, "Massive");
#endif

#if PETAPOCO
                Try(() =>
                {
                    // PetaPoco test with all default options
                    var petapoco = new PetaPoco.Database(TestSuite.ConnectionString, "System.Data.SqlClient");
                    petapoco.OpenSharedConnection();
                    tests.Add(id => petapoco.Fetch<Post>("SELECT * from Posts where Id=@0", id).First(), "PetaPoco (Normal)");

                    // PetaPoco with some "smart" functionality disabled
                    var petapocoFast = new PetaPoco.Database(TestSuite.ConnectionString, "System.Data.SqlClient");
                    petapocoFast.OpenSharedConnection();
                    petapocoFast.EnableAutoSelect = false;
                    petapocoFast.EnableNamedParams = false;
                    petapocoFast.ForceDateTimesToUtc = false;
                    tests.Add(id => petapocoFast.Fetch<Post>("SELECT * from Posts where Id=@0", id).First(), "PetaPoco (Fast)");
                }, "PetaPoco");
#endif

#if SUBSONIC
                Try(() =>
                    {
                    // Subsonic ActiveRecord 
                    tests.Add(id => SubSonic.Post.SingleOrDefault(x => x.Id == id), "SubSonic ActiveRecord.SingleOrDefault");

                    // Subsonic coding horror
                    SubSonic.tempdbDB db = new SubSonic.tempdbDB();
                    tests.Add(id => new SubSonic.Query.CodingHorror(db.Provider, "select * from Posts where Id = @0", id).ExecuteTypedList<Post>(), "SubSonic Coding Horror");
                }, "Subsonic");
#endif
                // NHibernate

#if NHIBERNATE
                Try(() => {
                    var nhSession1 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession1.CreateSQLQuery(@"select * from Posts where Id = :id")
                        .SetInt32("id", id)
                        .List(), "NHibernate SQL");

                    var nhSession2 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession2.CreateQuery(@"from Post as p where p.Id = :id")
                        .SetInt32("id", id)
                        .List(), "NHibernate HQL");

                    var nhSession3 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession3.CreateCriteria<Post>()
                        .Add(Restrictions.IdEq(id))
                        .List(), "NHibernate Criteria");

                    var nhSession4 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession4
                        .Query<Post>()
                        .First(p => p.Id == id), "NHibernate LINQ");

                    var nhSession5 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession5.Get<Post>(id), "NHibernate Session.Get");
                }, "NHibernate");
#endif
#if BLTOOLKIT
                // bltoolkit
                var db1 = new DbManager(TestSuite.GetOpenConnection());
                tests.Add(id => db1.SetCommand("select * from Posts where Id = @id", db1.Parameter("id", id)).ExecuteList<Post>(), "BLToolkit");
#endif
#if SIMPLEDATA
                // Simple.Data
                Try(() =>
                {
                    var sdb = Simple.Data.Database.OpenConnection(TestSuite.ConnectionString);
                    tests.Add(id => sdb.Posts.FindById(id), "Simple.Data");
                }, "Simple.Data");
#endif

#if SUSANOO
                //Susanoo
                var susanooDb = new DatabaseManager("Smackdown.Properties.Settings.tempdbConnectionString");
                var susanooDb2 = new DatabaseManager("Smackdown.Properties.Settings.tempdbConnectionString");


                var susanooPreDefinedCommand =
                    CommandManager.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<Post>()
                        .Realize("PostById");

                var susanooDynamicPreDefinedCommand =
                    CommandManager.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<dynamic>()
                        .Realize("DynamicById");

                tests.Add(Id =>
                    CommandManager.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<Post>()
                        .Realize("PostById")
                        .Execute(susanooDb, new { Id }).First(), "Susanoo Mapping Cache Retrieval");

                tests.Add(Id =>
                    CommandManager.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<dynamic>()
                        .Realize("DynamicById")
                        .Execute(susanooDb, new { Id }).First(), "Susanoo Dynamic Mapping Cache Retrieval");

                tests.Add(Id =>
                    susanooDynamicPreDefinedCommand
                        .Execute(susanooDb, new { Id }).First(), "Susanoo Dynamic Mapping Static");

                tests.Add(Id =>
                    susanooPreDefinedCommand
                        .Execute(susanooDb, new { Id }).First(), "Susanoo Mapping Static");
#endif

#if SOMA
                // Soma

                // DISABLED: assembly fail loading FSharp.PowerPack, Version=2.0.0.0
                // var somadb = new Soma.Core.Db(new SomaConfig());
                // tests.Add(id => somadb.Find<Post>(id), "Soma");
#endif
#if ORMLITE
                //ServiceStack's OrmLite:

                // DISABLED: can't find QueryById
                //OrmLiteConfig.DialectProvider = SqlServerOrmLiteDialectProvider.Instance; //Using SQL Server
                //IDbCommand ormLiteCmd = TestSuite.GetOpenConnection().CreateCommand();
                // tests.Add(id => ormLiteCmd.QueryById<Post>(id), "OrmLite QueryById");
#endif
                // HAND CODED 

                var postCommand = new SqlCommand();
                postCommand.Connection = connection;
                postCommand.CommandText = @"select Id, [Text], [CreationDate], LastChangeDate, 
                Counter1,Counter2,Counter3,Counter4,Counter5,Counter6,Counter7,Counter8,Counter9 from Posts where Id = @Id";
                var idParam = postCommand.Parameters.Add("@Id", System.Data.SqlDbType.Int);

                tests.Add(id =>
            {
                idParam.Value = id;

                using (var reader = postCommand.ExecuteReader())
                {
                    reader.Read();
                    var post = new Post();
                    post.Id = reader.GetInt32(0);
                    post.Text = reader.GetNullableString(1);
                    post.CreationDate = reader.GetDateTime(2);
                    post.LastChangeDate = reader.GetDateTime(3);

                    post.Counter1 = reader.GetNullableValue<int>(4);
                    post.Counter2 = reader.GetNullableValue<int>(5);
                    post.Counter3 = reader.GetNullableValue<int>(6);
                    post.Counter4 = reader.GetNullableValue<int>(7);
                    post.Counter5 = reader.GetNullableValue<int>(8);
                    post.Counter6 = reader.GetNullableValue<int>(9);
                    post.Counter7 = reader.GetNullableValue<int>(10);
                    post.Counter8 = reader.GetNullableValue<int>(11);
                    post.Counter9 = reader.GetNullableValue<int>(12);
                }
            }, "hand coded");

#if !COREFX
                DataTable table = new DataTable
                {
                    Columns =
                    {
                        {"Id", typeof (int)},
                        {"Text", typeof (string)},
                        {"CreationDate", typeof (DateTime)},
                        {"LastChangeDate", typeof (DateTime)},
                        {"Counter1", typeof (int)},
                        {"Counter2", typeof (int)},
                        {"Counter3", typeof (int)},
                        {"Counter4", typeof (int)},
                        {"Counter5", typeof (int)},
                        {"Counter6", typeof (int)},
                        {"Counter7", typeof (int)},
                        {"Counter8", typeof (int)},
                        {"Counter9", typeof (int)},
                    }
                };
                tests.Add(id =>
                {
                    idParam.Value = id;
                    object[] values = new object[13];
                    using (var reader = postCommand.ExecuteReader())
                    {
                        reader.Read();
                        reader.GetValues(values);
                        table.Rows.Add(values);
                    }
                }, "DataTable via IDataReader.GetValues");
#endif
                Console.WriteLine();
                Console.WriteLine("Running...");
                tests.Run(iterations);
            }
        }

    }

    static class SqlDataReaderHelper
    {
        public static string GetNullableString(this SqlDataReader reader, int index) 
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (string)tmp;
            }
            return null;
        }

        public static Nullable<T> GetNullableValue<T>(this SqlDataReader reader, int index) where T : struct
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (T)tmp;
            }
            return null;
        }
    }
}