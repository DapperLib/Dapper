using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Dapper.Tests.Performance.Helpers
{
    public class ORMColum : IColumn
    {
        public string Id => nameof(ORMColum);
        public string ColumnName { get; } = "ORM";
        public string Legend => "The object/relational mapper being tested";

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public string GetValue(Summary summary, Benchmark benchmark) => benchmark.Target.Method.DeclaringType.Name.Replace("Benchmarks", string.Empty);
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => benchmark.Target.Method.DeclaringType.Name.Replace("Benchmarks", string.Empty);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => -10;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public override string ToString() => ColumnName;
    }
}
