﻿#if ASYNC
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace DapperTests_NET45
{
    class Program
    {
        static void Main()
        {
            RunTests();
            Console.WriteLine("(end of tests; press any key)");

            Console.ReadKey();
        }
        public static readonly string connectionString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public static SqlConnection GetOpenConnection(bool mars = false)
        {
            var cs = connectionString;
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
            return new SqlConnection(connectionString);
        }
        private static void RunTests()
        {
            var tester = new Tests();
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name);
                try
                {
                    method.Invoke(tester, null);
                    Console.WriteLine(" - OK!");
                } catch(TargetInvocationException ex)
                {
                    var inner = ex.InnerException;
                    if(inner is AggregateException && ((AggregateException)inner).InnerExceptions.Count == 1)
                    {
                        inner = ((AggregateException)inner).InnerExceptions.Single();
                    }
                    Console.WriteLine(" - ERR: " + inner.Message);
                }
            }
        }
    }
}
#endif