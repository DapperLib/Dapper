using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using SqlMapper.Linq2Sql;
using System.Data.Linq;
using System.Diagnostics;

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

        static DataClassesDataContext GetL2SContext()
        {
            return new DataClassesDataContext(Program.GetOpenConnection());
        }

        public void Run(int iterations)
        {
            var tests = new Tests();

            var l2scontext1 = GetL2SContext();
            tests.Add(id => l2scontext1.Posts.First(p => p.Id == id), "Linq 2 SQL");

            var l2scontext2 = GetL2SContext();
            var compiledGetPost = CompiledQuery.Compile((Linq2Sql.DataClassesDataContext ctx, int id) => ctx.Posts.First(p => p.Id == id));
            tests.Add(id => compiledGetPost(l2scontext2,id), "Linq 2 SQL Compiled");

            var l2scontext3 = GetL2SContext();
            tests.Add(id => l2scontext3.ExecuteQuery<Post>("select * from Posts where Id = {0}", id).ToList(), "Linq 2 SQL ExecuteQuery");
            
            var entityContext = new EntityFramework.tempdbEntities1();
            entityContext.Connection.Open();
            tests.Add(id => entityContext.Posts.First(p => p.Id == id), "Entity framework");

            var entityContext2 = new EntityFramework.tempdbEntities1();
            entityContext2.Connection.Open();
            tests.Add(id => entityContext.ExecuteStoreQuery<Post>("select * from Posts where Id = {0}", id).ToList(), "Entity framework ExecuteStoreQuery");

            var mapperConnection = Program.GetOpenConnection();
            tests.Add(id => mapperConnection.ExecuteMapperQuery<Post>("select * from Posts where Id = @Id", new { Id = id }).ToList(), "Mapper Query");

            // HAND CODED 

            var connection = Program.GetOpenConnection();

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

            tests.Run(iterations);

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
