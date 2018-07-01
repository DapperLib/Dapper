using System;
using System.Data;

namespace Dapper
{
    public partial class DynamicParameters
    {
        private sealed class ParamInfo
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public ParameterDirection ParameterDirection { get; set; }
            public DbType? DbType { get; set; }
            public int? Size { get; set; }
            public IDbDataParameter AttachedParam { get; set; }
            internal Action<object, DynamicParameters> OutputCallback { get; set; }
            internal object OutputTarget { get; set; }
            internal bool CameFromTemplate { get; set; }

            public byte? Precision { get; set; }
            public byte? Scale { get; set; }
        }
    }
}
