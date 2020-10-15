#if NET4X
using Soma.Core;
using System;

namespace Dapper.Tests.Performance.Soma
{
    internal class SomaConfig : MsSqlConfig
    {
        public override string ConnectionString => BenchmarkBase.ConnectionString;

        public override Action<PreparedStatement> Logger => noOp;

        private static readonly Action<PreparedStatement> noOp = x => { /* nope */ };
    }
}
#endif
