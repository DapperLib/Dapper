
using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper.Data;
using Dapper.Data.Tests.Properties;

namespace Dapper.Data.Tests
{
    class Program
    {
	    private static Settings Settings
	    {
		    get { return Dapper.Data.Tests.Properties.Settings.Default; }
	    }

	    static void Main(string[] args)
	    {
			Setup(Settings.DefaultConnection);
            RunTests();
        }

        private static void Setup(string connectionString)
        {
	        var dbFile = Directory.GetFiles(Environment.CurrentDirectory, "Test.sdf").FirstOrDefault();
			if (File.Exists(dbFile))
			{ File.Delete(dbFile); }
			var engine = new SqlCeEngine(connectionString);
			engine.CreateDatabase();
			// execute multiple statatements using same connection
			// connection will be cleanedup automatically onec execution
			// compleats
	        TestDb.Instance().Batch(s =>
		    {
			    s.Execute(
					@"create table Users (
						 Id int IDENTITY(1,1) not null
						,Name nvarchar(100) not null
						,Age int not null)");
				s.Execute(
					@"create table Automobiles (
						 Id int IDENTITY(1,1) not null
						,Name nvarchar(100) not null)");
			});
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
            Console.ReadKey();
        }

    }
}
