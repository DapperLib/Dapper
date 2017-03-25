using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

//using BLToolkit.Data; // Note: this doesn't load in the new .csproj system...likely a bug
using Dapper.Tests.Performance.EntityFramework;
using Dapper.Tests.Performance.Linq2Sql;
using Dapper.Tests.Performance.NHibernate;
using Dapper.Contrib.Extensions;
using Massive;
using NHibernate.Criterion;
using NHibernate.Linq;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;
using Susanoo;
using System.Configuration;
using System.Threading.Tasks;

namespace Dapper.Tests.Performance
{
    public partial class PerformanceTests
    {
        private class Test
        {
            public Test(Action<int> iteration, string name)
            {
                Iteration = iteration;
                Name = name;
            }

            public Test(Func<int, Task> iterationAsync, string name)
            {
                IterationAsync = iterationAsync;
                Name = name;
            }

            public Action<int> Iteration { get; set; }
            public Func<int, Task> IterationAsync { get; set; }
            public string Name { get; set; }
            public Stopwatch Watch { get; set; }
        }

        private class Tests : List<Test>
        {
            public void Add(Action<int> iteration, string name)
            {
                Add(new Test(iteration, name));
            }
            public void AddAsync(Func<int, Task> iterationAsync, string name)
            {
                Add(new Test(iterationAsync, name));
            }

            public async Task RunAsync(int iterations)
            {
                // warmup 
                foreach (var test in this)
                {
                    test.Iteration?.Invoke(iterations + 1);
                    if (test.IterationAsync != null) await test.IterationAsync(iterations + 1).ConfigureAwait(false);
                    test.Watch = new Stopwatch();
                    test.Watch.Reset();
                }

                var rand = new Random();
                for (int i = 1; i <= iterations; i++)
                {
                    foreach (var test in this.OrderBy(ignore => rand.Next()))
                    {
                        test.Watch.Start();
                        test.Iteration?.Invoke(i);
                        if (test.IterationAsync != null) await test.IterationAsync(i).ConfigureAwait(false);
                        test.Watch.Stop();
                    }
                }

                foreach (var test in this.OrderBy(t => t.Watch.ElapsedMilliseconds))
                {
                    Console.WriteLine(test.Name + " took " + test.Watch.ElapsedMilliseconds + "ms");
                }
            }
        }

        public static string ConnectionString { get; } = ConfigurationManager.ConnectionStrings["Main"].ConnectionString;

        public static SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private static DataClassesDataContext GetL2SContext(SqlConnection connection) =>
            new DataClassesDataContext(connection);

        private static void Try(Action action, string blame)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{blame}: {ex.Message}");
            }
        }

        public async Task RunAsync(int iterations)
        {
            using (var connection = GetOpenConnection())
            {
                var tests = new Tests();

                // Linq2SQL
                Try(() =>
                {
                    var l2scontext1 = GetL2SContext(connection);
                    tests.Add(id => l2scontext1.Posts.First(p => p.Id == id), "Linq2Sql: Normal");

                    var l2scontext2 = GetL2SContext(connection);
                    var compiledGetPost = CompiledQuery.Compile((Linq2Sql.DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));
                    tests.Add(id => compiledGetPost(l2scontext2, id), "Linq2Sql: Compiled");

                    var l2scontext3 = GetL2SContext(connection);
                    tests.Add(id => l2scontext3.ExecuteQuery<Post>("select * from Posts where Id = {0}", id).First(), "Linq2Sql: ExecuteQuery");
                }, "LINQ-to-SQL");

                // Entity Framework
                Try(() =>
                {
                    var entityContext = new EFContext(connection);
                    tests.Add(id => entityContext.Posts.First(p => p.Id == id), "Entity Framework");

                    var entityContext2 = new EFContext(connection);
                    tests.Add(id => entityContext2.Database.SqlQuery<Post>("select * from Posts where Id = {0}", id).First(), "Entity Framework: SqlQuery");

                    //var entityContext3 = new EFContext(connection);
                    //tests.Add(id => entityFrameworkCompiled(entityContext3, id), "Entity Framework CompiledQuery");

                    //var entityContext4 = new EFContext(connection);
                    //tests.Add(id => entityContext4.Posts.Where("it.Id = @id", new System.Data.Objects.ObjectParameter("id", id)).First(), "Entity Framework ESQL");

                    var entityContext5 = new EFContext(connection);
                    tests.Add(id => entityContext5.Posts.AsNoTracking().First(p => p.Id == id), "Entity Framework: No Tracking");
                }, "Entity Framework");

                // Dapper
                Try(() =>
                {
                    var mapperConnection = GetOpenConnection();
                    tests.Add(id => mapperConnection.Query<Post>("select * from Posts where Id = @Id", new { Id = id }, buffered: true).First(), "Dapper: Query (buffered)");
                    tests.Add(id => mapperConnection.Query<Post>("select * from Posts where Id = @Id", new { Id = id }, buffered: false).First(), "Dapper: Query (non-buffered)");
                    tests.Add(id => mapperConnection.QueryFirstOrDefault<Post>("select * from Posts where Id = @Id", new { Id = id }), "Dapper: QueryFirstOrDefault");


                    var mapperConnection2 = GetOpenConnection();
                    tests.Add(id => mapperConnection2.Query("select * from Posts where Id = @Id", new { Id = id }, buffered: true).First(), "Dapper: Dynamic Query (buffered)");
                    tests.Add(id => mapperConnection2.Query("select * from Posts where Id = @Id", new { Id = id }, buffered: false).First(), "Dapper: Dynamic Query (non-buffered)");
                    tests.Add(id => mapperConnection2.QueryFirstOrDefault("select * from Posts where Id = @Id", new { Id = id }), "Dapper: Dynamic QueryFirstOrDefault");

                    // dapper.contrib
                    var mapperConnection3 = GetOpenConnection();
                    tests.Add(id => mapperConnection3.Get<Post>(id), "Dapper.Contrib");
                }, "Dapper");

                // Massive
                Try(() =>
                {
                    var massiveModel = new DynamicModel(ConnectionString);
                    var massiveConnection = GetOpenConnection();
                    tests.Add(id => massiveModel.Query("select * from Posts where Id = @0", massiveConnection, id).First(), "Massive: Dynamic ORM Query");
                }, "Massive");

                // PetaPoco
                Try(() =>
                {
                    // PetaPoco test with all default options
                    var petapoco = new PetaPoco.Database(ConnectionString, "System.Data.SqlClient");
                    petapoco.OpenSharedConnection();
                    tests.Add(id => petapoco.Fetch<Post>("SELECT * from Posts where Id=@0", id).First(), "PetaPoco: Normal");

                    // PetaPoco with some "smart" functionality disabled
                    var petapocoFast = new PetaPoco.Database(ConnectionString, "System.Data.SqlClient");
                    petapocoFast.OpenSharedConnection();
                    petapocoFast.EnableAutoSelect = false;
                    petapocoFast.EnableNamedParams = false;
                    petapocoFast.ForceDateTimesToUtc = false;
                    tests.Add(id => petapocoFast.Fetch<Post>("SELECT * from Posts where Id=@0", id).First(), "PetaPoco: Fast");
                }, "PetaPoco");

                // NHibernate
                Try(() =>
                {
                    var nhSession1 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession1.CreateSQLQuery(@"select * from Posts where Id = :id")
                        .SetInt32("id", id)
                        .List(), "NHibernate: SQL");

                    var nhSession2 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession2.CreateQuery(@"from Post as p where p.Id = :id")
                        .SetInt32("id", id)
                        .List(), "NHibernate: HQL");

                    var nhSession3 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession3.CreateCriteria<Post>()
                        .Add(Restrictions.IdEq(id))
                        .List(), "NHibernate: Criteria");

                    var nhSession4 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession4
                        .Query<Post>()
                        .First(p => p.Id == id), "NHibernate: LINQ");

                    var nhSession5 = NHibernateHelper.OpenSession();
                    tests.Add(id => nhSession5.Get<Post>(id), "NHibernate: Session.Get");
                }, "NHibernate");
                
                // Simple.Data
                Try(() =>
                {
                    var sdb = Simple.Data.Database.OpenConnection(ConnectionString);
                    tests.Add(id => sdb.Posts.FindById(id).FirstOrDefault(), "Simple.Data");
                }, "Simple.Data");

                // Belgrade
                Try(() =>
                {
                    var query = new Belgrade.SqlClient.SqlDb.QueryMapper(ConnectionString);
                    tests.AddAsync(id => query.ExecuteReader("SELECT TOP 1 * FROM Posts WHERE Id = " + id,
                        reader =>
                        {
                            var post = new Post();
                            post.Id = reader.GetInt32(0);
                            post.Text = reader.GetString(1);
                            post.CreationDate = reader.GetDateTime(2);
                            post.LastChangeDate = reader.GetDateTime(3);

                            post.Counter1 = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4);
                            post.Counter2 = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5);
                            post.Counter3 = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6);
                            post.Counter4 = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7);
                            post.Counter5 = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8);
                            post.Counter6 = reader.IsDBNull(9) ? null : (int?)reader.GetInt32(9);
                            post.Counter7 = reader.IsDBNull(10) ? null : (int?)reader.GetInt32(10);
                            post.Counter8 = reader.IsDBNull(11) ? null : (int?)reader.GetInt32(11);
                            post.Counter9 = reader.IsDBNull(12) ? null : (int?)reader.GetInt32(12);
                        }), "Belgrade Sql Client");
                }, "Belgrade Sql Client");

                //Susanoo
                var susanooDb = new DatabaseManager(connection);

                var susanooPreDefinedCommand =
                    CommandManager.Instance.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<Post>()
                        .Realize();

                var susanooDynamicPreDefinedCommand =
                    CommandManager.Instance.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<dynamic>()
                        .Realize();

                tests.Add(Id =>
                    CommandManager.Instance.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<Post>()
                        .Realize()
                        .Execute(susanooDb, new { Id }).First(), "Susanoo: Mapping Cache Retrieval");

                tests.Add(Id =>
                    CommandManager.Instance.DefineCommand("SELECT * FROM Posts WHERE Id = @Id", CommandType.Text)
                        .DefineResults<dynamic>()
                        .Realize()
                        .Execute(susanooDb, new { Id }).First(), "Susanoo: Dynamic Mapping Cache Retrieval");

                tests.Add(Id => susanooDynamicPreDefinedCommand
                        .Execute(susanooDb, new { Id }).First(), "Susanoo: Dynamic Mapping Static");

                tests.Add(Id => susanooPreDefinedCommand
                        .Execute(susanooDb, new { Id }).First(), "Susanoo: Mapping Static");

                //ServiceStack's OrmLite:
                Try(() =>
                {
                    var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServerDialect.Provider);
                    var db = dbFactory.Open();
                    tests.Add(id => db.SingleById<Post>(id), "ServiceStack.OrmLite: SingleById");
                }, "ServiceStack.OrmLite");
                
                // Hand Coded
                var postCommand = new SqlCommand();
                postCommand.Connection = connection;
                postCommand.CommandText = @"select Id, [Text], [CreationDate], LastChangeDate, 
                Counter1,Counter2,Counter3,Counter4,Counter5,Counter6,Counter7,Counter8,Counter9 from Posts where Id = @Id";
                var idParam = postCommand.Parameters.Add("@Id", SqlDbType.Int);

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
            }, "Hand Coded");

                // Subsonic isn't maintained anymore - doesn't import correctly
                //Try(() =>
                //    {
                //    // Subsonic ActiveRecord 
                //    tests.Add(id => 3SubSonic.Post.SingleOrDefault(x => x.Id == id), "SubSonic ActiveRecord.SingleOrDefault");

                //    // Subsonic coding horror
                //    SubSonic.tempdbDB db = new SubSonic.tempdbDB();
                //    tests.Add(id => new SubSonic.Query.CodingHorror(db.Provider, "select * from Posts where Id = @0", id).ExecuteTypedList<Post>(), "SubSonic Coding Horror");
                //}, "Subsonic");

                //// BLToolkit - doesn't import correctly in the new .csproj world
                //var db1 = new DbManager(GetOpenConnection());
                //tests.Add(id => db1.SetCommand("select * from Posts where Id = @id", db1.Parameter("id", id)).ExecuteList<Post>(), "BLToolkit");


#if !COREFX
                var table = new DataTable
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
                await tests.RunAsync(iterations).ConfigureAwait(false);
            }
        }
    }
}