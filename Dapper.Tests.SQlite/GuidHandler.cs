using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Dapper.Tests.SQlite
{
    /// <summary>
    /// 
    /// </summary>
    public class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Guid Parse(object value)
        {
            return new Guid((byte[])value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToByteArray();
        }
    }
}
