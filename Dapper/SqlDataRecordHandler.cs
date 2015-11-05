﻿using System;
using System.Collections.Generic;
using System.Data;

#if !DNXCORE50
namespace Dapper
{
    sealed class SqlDataRecordHandler : Dapper.SqlMapper.ITypeHandler
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
#endif