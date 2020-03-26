using System.Data;
using System.Data.SqlClient;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Dapper.EntityFrameworkCore
{
    /// <summary>
    /// Type-handler for the Geometry spatial type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetTopologySuiteGeometryHandler<T> : SqlMapper.TypeHandler<T>
        where T : Geometry
    {
        readonly SqlServerBytesWriter _writer;
        readonly SqlServerBytesReader _reader;

        /// <summary>
        /// Create a new handler instance.
        /// </summary>
        public NetTopologySuiteGeometryHandler()
        {
            _writer = new SqlServerBytesWriter { IsGeography = false };
            _reader = new SqlServerBytesReader { IsGeography = false };
        }

        /// <summary>
        /// Parse a database value back to a typed value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override T Parse(object value)
            => (T)_reader.Read((byte[])value);

        /// <summary>
        /// Assign the value of a parameter before a command executes.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public override void SetValue(IDbDataParameter parameter, T value)
        {
            parameter.Value = _writer.Write(value);

            ((SqlParameter)parameter).SqlDbType = SqlDbType.Udt;
            ((SqlParameter)parameter).UdtTypeName = "geometry";
        }
    }

}
