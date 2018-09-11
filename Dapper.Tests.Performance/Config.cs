using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using Dapper.Tests.Performance.Helpers;

namespace Dapper.Tests.Performance
{
    public class Config : ManualConfig
    {
        public const int Iterations = 5000;

        public Config()
        {
            Add(ConsoleLogger.Default);

            Add(CsvExporter.Default);
            Add(MarkdownExporter.GitHub);
            Add(HtmlExporter.Default);

            var md = new MemoryDiagnoser();
            Add(md);
            Add(new ORMColum());
            Add(TargetMethodColumn.Method);
            Add(new ReturnColum());
            Add(StatisticColumn.Mean);
            Add(StatisticColumn.StdDev);
            Add(StatisticColumn.Error);
            Add(BaselineScaledColumn.Scaled);
            Add(md.GetColumnProvider());

            Add(Job.Dry
                .WithLaunchCount(1)
                .WithWarmupCount(1)
                .WithInvocationCount(Iterations)
                .WithIterationCount(10)
            );
            Set(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
            SummaryPerType = false;
        }
    }
}
