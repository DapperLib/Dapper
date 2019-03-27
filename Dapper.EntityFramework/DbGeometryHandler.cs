using Microsoft.SqlServer.Types;
using System;
using System.Data;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace Dapper.EntityFramework
{
    /// <summary>
    /// Type-handler for the DbGeometry spatial type.
    /// </summary>
    public class DbGeometryHandler : SqlMapper.TypeHandler<DbGeometry>
    {
        /// <summary>
        /// Create a new handler instance.
        /// </summary>
        protected DbGeometryHandler() { /* create new */ }

        /// <summary>
        /// Default handler instance.
        /// </summary>
        public static readonly DbGeometryHandler Default = new DbGeometryHandler();

        /// <summary>
        /// Assign the value of a parameter before a command executes.
        /// </summary>
        /// <param name="parameter">The parameter to configure.</param>
        /// <param name="value">Parameter value.</param>
        public override void SetValue(IDbDataParameter parameter, DbGeometry value)
        {
            object parsed = null;
            if (value != null)
            {
                parsed = SqlGeometry.STGeomFromWKB(new SqlBytes(value.AsBinary()), value.CoordinateSystemId);
            }
            parameter.Value = parsed ?? DBNull.Value;
            if (parameter is SqlParameter)
            {
                ((SqlParameter)parameter).UdtTypeName = "geometry";
            }
        }

        /// <summary>
        /// Parse a database value back to a typed value.
        /// </summary>
        /// <param name="value">The value from the database.</param>
        /// <returns>The typed value.</returns>
        public override DbGeometry Parse(object value)
        {
            if (value == null || value is DBNull) return null;
            if (value is SqlGeometry geo)
            {
                return DbGeometry.FromBinary(geo.STAsBinary().Value, geo.STSrid.Value);
            }
            return DbGeometry.FromText(value.ToString());
        }
    }
}
