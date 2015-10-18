using System;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dapper.Contrib.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupMsSqlDatabase();
            SetupTables();
            RunTests();
            DropTables();
            SetupTables();
            RunAsyncTests();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static void SetupMsSqlDatabase()
        {
            using (var connection = new SqlConnection("Data Source = .\\SQLEXPRESS;Initial Catalog=master;Integrated Security=SSPI"))
            {
                connection.Open();
                var exists = connection.Query<int>("SELECT count(*) FROM master.sys.databases WHERE name = @name",
                    new { name = "DapperContribMsSqlTests" }).First();
                if (exists > 0)
                {
                    connection.Execute("drop database DapperContribMsSqlTests");
                }
                connection.Execute("create database DapperContribMsSqlTests");

            }
        }

        private static void DropTables()
        {

            using (var connection = new SqlConnection("Data Source = .\\SQLEXPRESS;Initial Catalog=DapperContribMsSqlTests;Integrated Security=SSPI"))
            {
                connection.Open();
                connection.Execute("alter database DapperContribMsSqlTests set single_user with rollback immediate");
                connection.Execute(@" drop table Stuff");
                connection.Execute(@" drop table People ");
                connection.Execute(@" drop table Users");
                connection.Execute(@" drop table Automobiles ");
                connection.Execute(@" drop table Results ");
                connection.Execute(@" drop table ObjectX ");
                connection.Execute(@" drop table ObjectY ");
            }
            Console.WriteLine("Created database");
        }

        private static void SetupTables()
        {

            using (var connection = new SqlConnection("Data Source = .\\SQLEXPRESS;Initial Catalog=DapperContribMsSqlTests;Integrated Security=SSPI"))
            {
                connection.Open();
                connection.Execute(@" create table Stuff (TheId int IDENTITY(1,1) not null, Name nvarchar(100) not null, Created DateTime null) ");
                connection.Execute(@" create table People (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null) ");
                connection.Execute(@" create table Automobiles (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
                connection.Execute(@" create table Results (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, [Order] int not null) ");
                connection.Execute(@" create table ObjectX (ObjectXId nvarchar(100) not null, Name nvarchar(100) not null) ");
                connection.Execute(@" create table ObjectY (ObjectYId int not null, Name nvarchar(100) not null) ");
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
