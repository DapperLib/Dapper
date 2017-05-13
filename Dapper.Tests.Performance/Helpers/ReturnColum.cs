using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Dapper.Tests.Performance.Helpers
{
    public class ReturnColum : IColumn
    {
        public string Id => nameof(ReturnColum);
        public string ColumnName { get; } = "Return";
        public string Legend => "The return type of the method";

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public string GetValue(Summary summary, Benchmark benchmark) => benchmark.Target.Method.ReturnType.Name;
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => benchmark.Target.Method.ReturnType.Name;

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 1;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public override string ToString() => ColumnName;
    }
}
