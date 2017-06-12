﻿using System;
using System.Collections.Generic;
using System.Data;

namespace Dapper
{
    internal sealed class SqlDataRecordHandler : SqlMapper.ITypeHandler
    {
        public object Parse(Type destinationType, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            SqlDataRecordListTVPParameter.Set(parameter, value as IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord>, null);
        }
    }
}
