using BenchmarkDotNet.Running;
using System;
using System.Data.SqlClient;
using System.Linq;
using static System.Console;

namespace Dapper.Tests.Performance
{
    public static class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            WriteLineColor("Warning: DEBUG configuration; performance may be impacted!", ConsoleColor.Red);
            WriteLine();
#endif
            WriteLine("Welcome to Dapper's ORM performance benchmark suite, based on BenchmarkDotNet.");
            Write("  If you find a problem, please report it at: ");
            WriteLineColor("https://github.com/StackExchange/Dapper", ConsoleColor.Blue);
            WriteLine("  Or if you're up to it, please submit a pull request! We welcome new additions.");
            WriteLine();

            if (args.Length == 0)
            {
                WriteLine("Optional arguments:");
                WriteColor("  (no args)", ConsoleColor.Blue);
                WriteLine(": run all benchmarks");
                WriteColor("  --legacy", ConsoleColor.Blue);
                WriteLineColor(": run the legacy benchmark suite/format", ConsoleColor.Gray);
                WriteLine();
            }
            WriteLine("Using ConnectionString: " + BenchmarkBase.ConnectionString);
            EnsureDBSetup();
            WriteLine("Database setup complete.");

            if (args.Any(a => a == "--legacy"))
            {
                var test = new LegacyTests();
                const int iterations = 500;
                WriteLineColor($"Running legacy benchmarks: {iterations} iterations that load up a Post entity.", ConsoleColor.Green);
                test.RunAsync(iterations).GetAwaiter().GetResult();
                WriteLine();
                WriteLineColor("Run complete.", ConsoleColor.Green);
            }
            else
            {
                WriteLine("Iterations: " + Config.Iterations);
                new BenchmarkSwitcher(typeof(BenchmarkBase).Assembly).Run(args, new Config());
            }
        }

        private static void EnsureDBSetup()
        {
            using (var cnn = new SqlConnection(BenchmarkBase.ConnectionString))
            {
                cnn.Open();
                var cmd = cnn.CreateCommand();
                cmd.CommandText = @"
If (Object_Id('Posts') Is Null)
Begin
	Create Table Posts
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
	);
	   
	Set NoCount On;
	Declare @i int = 0;

	While @i <= 5001
	Begin
		Insert Posts ([Text],CreationDate, LastChangeDate) values (replicate('x', 2000), GETDATE(), GETDATE());
		Set @i = @i + 1;
	End
End
";
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
        }

        public static void WriteLineColor(string message, ConsoleColor color)
        {
            var orig = ForegroundColor;
            ForegroundColor = color;
            WriteLine(message);
            ForegroundColor = orig;
        }

        public static void WriteColor(string message, ConsoleColor color)
        {
            var orig = ForegroundColor;
            ForegroundColor = color;
            Write(message);
            ForegroundColor = orig;
        }
    }
}
