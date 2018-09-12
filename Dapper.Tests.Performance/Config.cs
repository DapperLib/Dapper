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
        public const int Iterations = 500;

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
            //Add(StatisticColumn.StdDev);
            //Add(StatisticColumn.Error);
            Add(BaselineScaledColumn.Scaled);
            Add(md.GetColumnProvider());

            Add(Job.ShortRun
                   .WithLaunchCount(1)
                   .WithWarmupCount(2)
                   .WithUnrollFactor(Iterations)
                   .WithIterationCount(1)
            );
            Set(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
            SummaryPerType = false;
        }
    }
}
