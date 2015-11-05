﻿using System;
using System.Data;
#if !DNXCORE50
namespace Dapper
{
    sealed class DataTableHandler : Dapper.SqlMapper.ITypeHandler
    {
        public object Parse(Type destinationType, object value)
        {
            throw new NotImplementedException();
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            TableValuedParameter.Set(parameter, value as DataTable, null);
        }
    }
}
#endif