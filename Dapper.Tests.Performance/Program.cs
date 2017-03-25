using System;
using System.Threading.Tasks;

namespace Dapper.Tests.Performance
{
    // Note: VSTest injects an entry point in .NET Core land...so we have to split this out into
    // a separate project...so here we are.
    // See https://github.com/Microsoft/vstest/issues/636 for details
    public static class Program
    {
        public static void Main()
        {
#if DEBUG
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Warning: DEBUG configuration; performance may be impacted");
            Console.ForegroundColor = fg;
            Console.WriteLine();
#endif
            Console.WriteLine("Using ConnectionString: " + PerformanceTests.ConnectionString);

            EnsureDBSetup();
            RunPerformanceTestsAsync().GetAwaiter().GetResult();
        }

        private static void EnsureDBSetup()
        {
            using (var cnn = PerformanceTests.GetOpenConnection())
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

        private static async Task RunPerformanceTestsAsync()
        {
            var test = new PerformanceTests();
            const int iterations = 500;
            Console.WriteLine("Running {0} iterations that load up a post entity", iterations);
            await test.RunAsync(iterations).ConfigureAwait(false);
        }
    }
}
