using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Dapper;

namespace SqlMapper
{

#if EXTERNALS
    [ServiceStack.DataAnnotations.Alias("Posts")]
	[Soma.Core.Table(Name = "Posts")]
#endif
    public class Post
    {
#if EXTERNALS
		[Soma.Core.Id(Soma.Core.IdKind.Identity)]
#endif
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public int? Counter1 { get; set; }
        public int? Counter2 { get; set; }
        public int? Counter3 { get; set; }
        public int? Counter4 { get; set; }
        public int? Counter5 { get; set; }
        public int? Counter6 { get; set; }
        public int? Counter7 { get; set; }
        public int? Counter8 { get; set; }
        public int? Counter9 { get; set; }
    }

    class Program
    {
        public const string ConnectionString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True",
            AppveyorConnectionStrng = @"Server=(local)\SQL2014;Database=master;User ID=sa;Password=Password12!",
            OleDbConnectionString = "Provider=SQLOLEDB;Data Source=.;Initial Catalog=tempdb;Integrated Security=SSPI";

        public static SqlConnection GetOpenConnection(bool mars = false)
        {
            var isAppveyor = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Appveyor"));
            var cs = isAppveyor ? AppveyorConnectionStrng : ConnectionString;
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
            return new SqlConnection(ConnectionString);
        }

        static void RunPerformanceTests()
        {
#if PERF
            var test = new PerformanceTests();
            const int iterations = 500;
            Console.WriteLine("Running {0} iterations that load up a post entity", iterations);
            test.Run(iterations);
#else
            Console.WriteLine("Performance tests have not been built; add the PERF symbol");
#endif
        }

        static void Main()
        {
#if COREFX
                Console.WriteLine("CoreCLR");
#else
                Console.WriteLine(".NET: " + Environment.Version);
#endif
            Console.WriteLine("Dapper: " + typeof(global::Dapper.SqlMapper).AssemblyQualifiedName);

#if DEBUG
            int fail = 0, skip = 0, pass = 0, frameworkFail = 0;
            var failNames = new List<string>();

            RunTests<SqlMapper.Tests>(ref fail, ref skip, ref pass, ref frameworkFail, failNames);
#if ASYNC
            RunTests<DapperTests_NET45.Tests>(ref fail, ref skip, ref pass, ref frameworkFail, failNames);
#endif
            
            if (fail == 0)
            {
                Console.WriteLine("(all tests successful)");
            }
            else
            {
                Console.WriteLine("Failures:");
                foreach (var failName in failNames)
                {
                    Console.WriteLine(failName);
                }
            }
            Console.WriteLine("Passed: {0}, Failed: {1}, Skipped: {2}, Framework-fail: {3}", pass, fail, skip, frameworkFail);
#else
            EnsureDBSetup();
            RunPerformanceTests();
#endif

#if COREFX
            Console.WriteLine("(end of tests; press return)");
            Console.ReadLine();
#else
            Console.WriteLine("(end of tests; press any key)");
            Console.ReadKey();
#endif
        }

        private static void EnsureDBSetup()
        {
            using (var cnn = GetOpenConnection())
            {
                var cmd = cnn.CreateCommand();
                cmd.CommandText = @"
if (OBJECT_ID('Posts') is null)
begin
	create table Posts
	(
		Id int identity primary key, 
		[Text] varchar(max) not null, 
		CreationDate datetime not null, 
		LastChangeDate datetime not null,
		Counter1 int,
		Counter2 int,
		Counter3 int,
		Counter4 int,
		Counter5 int,
		Counter6 int,
		Counter7 int,
		Counter8 int,
		Counter9 int
	)
	   
	set nocount on 

	declare @i int
	declare @c int

	declare @id int

	set @i = 0

	while @i <= 5001
	begin 
		
		insert Posts ([Text],CreationDate, LastChangeDate) values (replicate('x', 2000), GETDATE(), GETDATE())
		set @id = @@IDENTITY
		
		set @i = @i + 1
	end
end
";
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
        }
        private static bool HasAttribute<T>(MemberInfo member) where T : Attribute
        {
#if COREFX
            return member.CustomAttributes.Any(x => x.AttributeType == typeof(T));
#else
            return Attribute.IsDefined(member, typeof(T), true);
#endif
        }

        private static void RunTests<T>(ref int fail, ref int skip, ref int pass, ref int frameworkFail, List<string> failNames) where T : class, IDisposable, new()
        {
            var tester = new T();
            using (tester as IDisposable)
            {

                MethodInfo[] methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var activeTests = methods.Where(HasAttribute<ActiveTestAttribute>).ToArray();
                if (activeTests.Length != 0) methods = activeTests;
                foreach (var method in methods)
                {
#if COREFX
                    var fact = (FactAttribute)method.GetCustomAttribute(typeof(FactAttribute));
#else
                    var fact = (FactAttribute)Attribute.GetCustomAttribute(method, typeof(FactAttribute));
#endif
                    if(fact == null)
                    {
                        Console.WriteLine(" - missing [Fact]");
                        fail++;
                        failNames.Add(method.Name);
                        continue;
                    }
                    if(!string.IsNullOrWhiteSpace(fact.Skip))
                    {
                        Console.WriteLine("Skipping " + method.Name);
                        skip++;
                        continue;

                    }

                    Console.Write("Running " + method.Name);
                    try
                    {
                        using (var t = new T())
                        {
                            method.Invoke(t, null);
                        }

                        Console.WriteLine(" - OK!");
                        pass++;
                    }
                    catch (TargetInvocationException tie)
                    {
                        Console.WriteLine(" - " + tie.InnerException.Message);

                        fail++;
                        failNames.Add(method.Name);
                        if (tie.InnerException is TypeInitializationException)
                        {
                            Console.WriteLine("> " + tie.InnerException.InnerException.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        fail++;
                        Console.WriteLine(" - " + ex.Message);
                        failNames.Add(method.Name);
                    }
                }
                Console.WriteLine();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ActiveTestAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactUnlessCoreCLRAttribute : FactAttribute {
        public FactUnlessCoreCLRAttribute(string url)
        {
#if COREFX
            Skip = $"CoreFX: {url}";
#endif
            this.Url = url;
        } 
        public string Url { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactLongRunningAttribute : FactAttribute
    {
        public FactLongRunningAttribute()
        {
#if !LONG_RUNNING
            Skip = $"Long running";
#endif
        }
        public string Url { get; private set; }
    }
    class FactUnlessCaseSensitiveDatabaseAttribute : FactAttribute
    {
        public FactUnlessCaseSensitiveDatabaseAttribute() : base()
        {
            if (IsCaseSensitive)
            {
                Skip = "Case sensitive database";
            }
        }

        public static readonly bool IsCaseSensitive;
        static FactUnlessCaseSensitiveDatabaseAttribute()
        {
            using (var conn = Program.GetOpenConnection())
            {
                try
                {
                    conn.Execute("declare @i int; set @I = 1;");
                }
                catch (SqlException s)
                {
                    if (s.Number == 137)
                        IsCaseSensitive = true;
                    else
                        throw;
                }
            }
        }
    }

}
