using System;
using System.Collections.Generic;
using System.Data;

namespace Dapper
{
    internal sealed class SqlDataRecordHandler<T> : SqlMapper.ITypeHandler
        where T : IDataRecord
    {
        public object Parse(Type destinationType, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            SqlDataRecordListTVPParameter<T>.Set(parameter, value as IEnumerable<T>, null);
        }
    }
}
