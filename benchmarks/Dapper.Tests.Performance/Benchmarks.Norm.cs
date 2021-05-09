#if !NET4X
using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using System.Linq;
using Norm;
using System;

namespace Dapper.Tests.Performance
{
    [Description("Norm")]
    public class NormBenchmarks : BenchmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
        }

        [Benchmark(Description = "Read<> (class)")]
        public Post Read()
        {
            Step();
            return _connection.Read<Post>("select * from Posts where Id = @Id", i).First();
        }

        [Benchmark(Description = "Read<> (tuples)")]
        public (int, string, DateTime, DateTime, int?, int?, int?, int?, int?, int?, int?, int?) ReadSimpleValues()
        {
            Step();
            return _connection.Read<int, string, DateTime, DateTime, int?, int?, int?, int?, int?, int?, int?, int?>("select * from Posts where Id = @Id", i).First();
        }

        [Benchmark(Description = "Read<()> (named tuples)")]
        public (int Id, string Text, DateTime CreationDate, DateTime LastChangeDate, int? Counter1, int? Counter2, int? Counter3, int? Counter4, int? Counter5, int? Counter6, int? Counter7, int? Counter8) ReadTuple()
        {
            Step();
            return _connection.Read<(int Id, string Text, DateTime CreationDate, DateTime LastChangeDate, int? Counter1, int? Counter2, int? Counter3, int? Counter4, int? Counter5, int? Counter6, int? Counter7, int? Counter8)>("select * from Posts where Id = @Id", i).First();
        }
    }
}
#endif
