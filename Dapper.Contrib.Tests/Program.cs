using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Npgsql;

namespace Dapper.Contrib.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            //Test SqlCe Database.
            RunTests(new SqlCeTests());

            //If postgresql is set up, uncomment this section.  To run this on windows, try http://www.postgresql.org/download/windows/
            //This assumes you already have a database called test, and a test user with permissions on that database.
            RunTests(new PostgresqlTests());

            Console.WriteLine("All tests complete!  Press any key to exit.");
            Console.ReadKey();
        }

        private static void RunTests(Tests tester)
        {
            Console.WriteLine("Now testing: " + tester.GetType().Name);

            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name);
                method.Invoke(tester, null);
                Console.WriteLine(" - OK!");
            }
        }

    }
}
