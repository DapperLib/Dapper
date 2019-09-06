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

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var type = benchmarkCase.Descriptor.WorkloadMethod.ReturnType;
            return type == typeof(object) ? "dynamic" : type.Name;
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 1;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public override string ToString() => ColumnName;
    }
}
