using Microsoft.SqlServer.Types;
using System;
using System.Data;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace Dapper.EntityFramework
{
    /// <summary>
    /// Type-handler for the DbGeography spatial type
    /// </summary>
    public class DbGeographyHandler : SqlMapper.TypeHandler<DbGeography>
    {
        /// <summary>
        /// Create a new handler instance
        /// </summary>
        protected DbGeographyHandler() { }
        /// <summary>
        /// Default handler instance
        /// </summary>
        public static readonly DbGeographyHandler Default = new DbGeographyHandler();
        /// <summary>
        /// Assign the value of a parameter before a command executes
        /// </summary>
        /// <param name="parameter">The parameter to configure</param>
        /// <param name="value">Parameter value</param>
        public override void SetValue(IDbDataParameter parameter, DbGeography value)
        {
            object parsed = null;
            if (value != null)
            {
                parsed = SqlGeography.STGeomFromWKB(new SqlBytes(value.AsBinary()), value.CoordinateSystemId);
            }
            parameter.Value = parsed ?? DBNull.Value;
            var sqlParameter = parameter as SqlParameter;
            if (sqlParameter != null)
            {
                sqlParameter.UdtTypeName = "geography";
            }
        }
        /// <summary>
        /// Parse a database value back to a typed value
        /// </summary>
        /// <param name="value">The value from the database</param>
        /// <returns>The typed value</returns>
        public override DbGeography Parse(object value)
        {
            if (value == null || value is DBNull) return null;
            if (value is SqlGeography)
            {
                var geo = (SqlGeography)value;
                return DbGeography.FromBinary(geo.STAsBinary().Value);
            }
            return DbGeography.FromText(value.ToString());
        }
    }
}
