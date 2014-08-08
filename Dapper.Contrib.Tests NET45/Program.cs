using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Contrib.Tests_NET45
{
    class Program
    {
        static void Main(string[] args)
        {
            Setup();
            RunTests();
            Setup();
            RunAsyncTests();
            Console.ReadKey();
        }

        private static void Setup()
        {
            var projLoc = Assembly.GetAssembly(typeof(Program)).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            if (File.Exists(projFolder + "\\Test.sdf"))
                File.Delete(projFolder + "\\Test.sdf");
            var connectionString = "Data Source = " + projFolder + "\\Test.sdf;";
            var engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
            using (var connection = new SqlCeConnection(connectionString))
            {
                connection.Open();
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null) ");
                connection.Execute(@" create table Automobiles (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
                connection.Execute(@" create table Results (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, [Order] int not null) ");
            }
            Console.WriteLine("Created database");
        }

        private static void RunTests()
        {
            var tester = new Tests();
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.ReturnType != typeof(Task))
                {
                    Console.Write("Running " + method.Name);
                    method.Invoke(tester, null);
                    Console.WriteLine(" - OK!");
                }
            }
        }

        private static void RunAsyncTests()
        {
            var tester = new TestsAsync();

            Console.Write("Running TableNameAsync");
            Task.WaitAll(tester.TableNameAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running TestSimpleGetAsync");
            Task.WaitAll(tester.TestSimpleGetAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running InsertGetUpdateAsync");
            Task.WaitAll(tester.InsertGetUpdateAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running InsertCheckKeyAsync");
            Task.WaitAll(tester.InsertCheckKeyAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running BuilderSelectClauseAsync");
            Task.WaitAll(tester.BuilderSelectClauseAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running BuilderTemplateWOCompositionAsync");
            Task.WaitAll(tester.BuilderTemplateWOCompositionAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running InsertFieldWithReservedNameAsync");
            Task.WaitAll(tester.InsertFieldWithReservedNameAsync());
            Console.WriteLine(" - OK!");

            Console.Write("Running DeleteAllAsync");
            Task.WaitAll(tester.DeleteAllAsync());
            Console.WriteLine(" - OK!");
        }
    }
}
