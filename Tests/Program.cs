using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
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
            OleDbConnectionString = "Provider=SQLOLEDB;Data Source=.;Initial Catalog=tempdb;Integrated Security=SSPI";

        public static SqlConnection GetOpenConnection(bool mars = false)
        {
            var cs = ConnectionString;
            if (mars)
            {
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(cs);
                scsb.MultipleActiveResultSets = true;
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
#if DNXCORE50
            Console.WriteLine("CoreCLR");
#else
            Console.WriteLine(Environment.Version);
#endif

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

#if DNXCORE50
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
#if DNXCORE50
            return member.CustomAttributes.Any(x => x.AttributeType == typeof(T));
#else
            return Attribute.IsDefined(member, typeof(T), true);
#endif
        }

        private static void RunTests<T>(ref int fail, ref int skip, ref int pass, ref int frameworkFail, List<string> failNames) where T : class, new()
        {
            var tester = new T();
            using (tester as IDisposable)
            {

                MethodInfo[] methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var activeTests = methods.Where(m => HasAttribute<ActiveTestAttribute>(m)).ToArray();
                if (activeTests.Length != 0) methods = activeTests;
                foreach (var method in methods)
                {
                    if (HasAttribute<SkipTestAttribute>(method))
                    {
                        Console.Write("Skipping " + method.Name);
                        skip++;
                        continue;
                    }
                    bool expectFrameworkFail = HasAttribute<FrameworkFail>(method);

                    Console.Write("Running " + method.Name);
                    try
                    {
                        method.Invoke(tester, null);
                        if (expectFrameworkFail)
                        {
                            Console.WriteLine(" - was expected to framework-fail, but didn't");
                            fail++;
                            failNames.Add(method.Name);
                        }
                        else
                        {
                            Console.WriteLine(" - OK!");
                            pass++;
                        }
                    }
                    catch (TargetInvocationException tie)
                    {
                        Console.WriteLine(" - " + tie.InnerException.Message);
                        if (expectFrameworkFail)
                        {
                            frameworkFail++;
                        }
                        else
                        {
                            fail++;
                            failNames.Add(method.Name);
                            if (tie.InnerException is TypeInitializationException)
                            {
                                Console.WriteLine("> " + tie.InnerException.InnerException.Message);
                            }
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
    public sealed class SkipTestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FrameworkFail : Attribute {
        public FrameworkFail(string url) {
            this.Url = url;
        } 
        public string Url { get; private set; }
    }

}
