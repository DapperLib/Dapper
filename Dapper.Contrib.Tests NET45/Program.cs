using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Dapper.Contrib.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Setup();
            RunTests();
            Setup();
            RunAsyncTests();
            Console.WriteLine("Press any key...");
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
                connection.Execute(@" create table Stuff (TheId int IDENTITY(1,1) not null, Name nvarchar(100) not null, Created DateTime null) ");
                connection.Execute(@" create table People (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
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
                Console.Write("Running " + method.Name);
                method.Invoke(tester, null);
                Console.WriteLine(" - OK!");
            }
        }

        private static void RunAsyncTests()
        {
            var tester = new TestsAsync();
            foreach (var method in typeof(TestsAsync).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name);
                Task.WaitAll((Task)method.Invoke(tester, null));
                Console.WriteLine(" - OK!");
            }
        }
    }
}
