using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using BLToolkit.Data;
using Dapper;
using Massive;
using NHibernate.Criterion;
using NHibernate.Linq;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using SqlMapper.Linq2Sql;
using SqlMapper.NHibernate;
using Dapper.Contrib.Extensions;
using SqlMapper.EntityFramework;

namespace SqlMapper
{
    class PerformanceTests
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

        static DataClassesDataContext GetL2SContext(SqlConnection connection)
        {
            return new DataClassesDataContext(connection);
        }

		internal class SomaConfig : Soma.Core.MsSqlConfig
		{
			public override string ConnectionString
			{
				get { return Program.ConnectionString; }
			}

			public override void Log(Soma.Core.PreparedStatement preparedStatement)
			{
				// no op
			}
		}

        public void Run(int iterations)
        {
            using (var connection = Program.GetOpenConnection())
            {
                var tests = new Tests();

                var l2scontext1 = GetL2SContext(connection);
                tests.Add(id => l2scontext1.Posts.First(p => p.Id == id), "Linq 2 SQL");

                var l2scontext2 = GetL2SContext(connection);
                var compiledGetPost = CompiledQuery.Compile((Linq2Sql.DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));
                tests.Add(id => compiledGetPost(l2scontext2, id), "Linq 2 SQL Compiled");

                var l2scontext3 = GetL2SContext(connection);
                tests.Add(id => l2scontext3.ExecuteQuery<Post>("select * from Posts where Id = {0}", id).First(), "Linq 2 SQL ExecuteQuery");

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

                var mapperConnection = Program.GetOpenConnection();
                tests.Add(id => mapperConnection.Query<Post>("select * from Posts where Id = @Id", new { Id = id }, buffered: true).First(), "Mapper Query (buffered)");
                tests.Add(id => mapperConnection.Query<Post>("select * from Posts where Id = @Id", new { Id = id }, buffered: false).First(), "Mapper Query (non-buffered)");

                var mapperConnection2 = Program.GetOpenConnection();
                tests.Add(id => mapperConnection2.Query("select * from Posts where Id = @Id", new { Id = id }, buffered: true).First(), "Dynamic Mapper Query (buffered)");
                tests.Add(id => mapperConnection2.Query("select * from Posts where Id = @Id", new { Id = id }, buffered: false).First(), "Dynamic Mapper Query (non-buffered)");

                // dapper.contrib
                var mapperConnection3 = Program.GetOpenConnection();
                tests.Add(id => mapperConnection3.Get<Post>(id), "Dapper.Cotrib");

                var massiveModel = new DynamicModel(Program.ConnectionString);
                var massiveConnection = Program.GetOpenConnection();
                tests.Add(id => massiveModel.Query("select * from Posts where Id = @0", massiveConnection, id).First(), "Dynamic Massive ORM Query");

                // PetaPoco test with all default options
                var petapoco = new PetaPoco.Database(Program.ConnectionString, "System.Data.SqlClient");
                petapoco.OpenSharedConnection();
                tests.Add(id => petapoco.Fetch<Post>("SELECT * from Posts where Id=@0", id), "PetaPoco (Normal)");

                // PetaPoco with some "smart" functionality disabled
                var petapocoFast = new PetaPoco.Database(Program.ConnectionString, "System.Data.SqlClient");
                petapocoFast.OpenSharedConnection();
                petapocoFast.EnableAutoSelect = false;
                petapocoFast.EnableNamedParams = false;
                petapocoFast.ForceDateTimesToUtc = false;
                tests.Add(id => petapocoFast.Fetch<Post>("SELECT * from Posts where Id=@0", id), "PetaPoco (Fast)");

                // Subsonic ActiveRecord 
                tests.Add(id => SubSonic.Post.SingleOrDefault(x => x.Id == id), "SubSonic ActiveRecord.SingleOrDefault");

                // Subsonic coding horror
                SubSonic.tempdbDB db = new SubSonic.tempdbDB();
                tests.Add(id => new SubSonic.Query.CodingHorror(db.Provider, "select * from Posts where Id = @0", id).ExecuteTypedList<Post>(), "SubSonic Coding Horror");

                // NHibernate

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
                    .Where(p => p.Id == id).First(), "NHibernate LINQ");

                var nhSession5 = NHibernateHelper.OpenSession();
                tests.Add(id => nhSession5.Get<Post>(id), "NHibernate Session.Get");

                // bltoolkit
                var db1 = new DbManager(Program.GetOpenConnection());
                tests.Add(id => db1.SetCommand("select * from Posts where Id = @id", db1.Parameter("id", id)).ExecuteList<Post>(), "BLToolkit");

                // Simple.Data
                var sdb = Simple.Data.Database.OpenConnection(Program.ConnectionString);
                tests.Add(id => sdb.Posts.FindById(id), "Simple.Data");

                // Soma
                var somadb = new Soma.Core.Db(new SomaConfig());
                tests.Add(id => somadb.Find<Post>(id), "Soma");

                //ServiceStack's OrmLite:
                OrmLiteConfig.DialectProvider = SqlServerOrmLiteDialectProvider.Instance; //Using SQL Server
                IDbCommand ormLiteCmd = Program.GetOpenConnection().CreateCommand();
                tests.Add(id => ormLiteCmd.QueryById<Post>(id), "OrmLite QueryById");

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