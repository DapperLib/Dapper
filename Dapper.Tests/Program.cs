using System;

namespace Dapper.Tests
{
#if ORMLITE
    [ServiceStack.DataAnnotations.Alias("Posts")]
#endif
#if SOMA
    [Soma.Core.Table(Name = "Posts")]
#endif
    public class Post
    {
#if SOMA
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
        static void Main()
        {
#if DEBUG
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Warning: DEBUG configuration; performance may be impacted");
#if DNX
            Console.WriteLine("use: dnx --configuration release perf");
#endif
            Console.ForegroundColor = fg;
            Console.WriteLine();
#endif
            EnsureDBSetup();
            RunPerformanceTests();
        }

        private static void EnsureDBSetup()
        {
            using (var cnn = TestSuite.GetOpenConnection())
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

        static void RunPerformanceTests()
        {
            var test = new PerformanceTests();
            const int iterations = 500;
            Console.WriteLine("Running {0} iterations that load up a post entity", iterations);
            test.Run(iterations);
        }
    }
}
