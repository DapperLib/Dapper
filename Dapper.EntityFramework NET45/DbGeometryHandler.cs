using Microsoft.SqlServer.Types;
using System;
using System.Data;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;

namespace Dapper.EntityFramework
{
    /// <summary>
    /// Type-handler for the DbGeometry spatial type
    /// </summary>
    public class DbGeometryHandler : Dapper.SqlMapper.TypeHandler<DbGeometry>
    {
        /// <summary>
        /// Create a new handler instance
        /// </summary>
        protected DbGeometryHandler() { }
        /// <summary>
        /// Default handler instance
        /// </summary>
        public static readonly DbGeometryHandler Default = new DbGeometryHandler();
        /// <summary>
        /// Assign the value of a parameter before a command executes
        /// </summary>
        /// <param name="parameter">The parameter to configure</param>
        /// <param name="value">Parameter value</param>
        public override void SetValue(IDbDataParameter parameter, DbGeometry value)
        {
            parameter.Value = value == null ? (object)DBNull.Value : (object)SqlGeometry.Parse(value.AsText());
            if (parameter is SqlParameter)
            {
                ((SqlParameter)parameter).UdtTypeName = "GEOMETRY";
            }
        }
        /// <summary>
        /// Parse a database value back to a typed value
        /// </summary>
        /// <param name="value">The value from the database</param>
        /// <returns>The typed value</returns>
        public override DbGeometry Parse(object value)
        {
            return (value == null || value is DBNull) ? null : DbGeometry.FromText(value.ToString());
        }
    }
}
