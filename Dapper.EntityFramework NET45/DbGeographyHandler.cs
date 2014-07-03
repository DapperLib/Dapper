using Microsoft.SqlServer.Types;
using System;
using System.Data;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;

namespace Dapper.EntityFramework
{
    /// <summary>
    /// Type-handler for the DbGeography spatial type
    /// </summary>
    public class DbGeographyHandler : Dapper.SqlMapper.TypeHandler<DbGeography>
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
            parameter.Value = value == null ? (object)DBNull.Value : (object)SqlGeography.Parse(value.AsText());
            if (parameter is SqlParameter)
            {
                ((SqlParameter)parameter).UdtTypeName = "GEOGRAPHY";
            }
        }
        /// <summary>
        /// Parse a database value back to a typed value
        /// </summary>
        /// <param name="value">The value from the database</param>
        /// <returns>The typed value</returns>
        public override DbGeography Parse(object value)
        {
            return (value == null || value is DBNull) ? null : DbGeography.FromText(value.ToString());
        }
    }
}
