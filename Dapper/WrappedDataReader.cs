using System.Data;

#if DNXCORE50
using IDbCommand = System.Data.Common.DbCommand;
using IDataReader = System.Data.Common.DbDataReader;
#endif

namespace Dapper
{
#if DNXCORE50
    /// <summary>
    /// Describes a reader that controls the lifetime of both a command and a reader,
    /// exposing the downstream command/reader as properties.
    /// </summary>
    public abstract class WrappedDataReader : IDataReader
    {
        /// <summary>
        /// Obtain the underlying reader
        /// </summary>
        public abstract IDataReader Reader { get; }
        /// <summary>
        /// Obtain the underlying command
        /// </summary>
        public abstract IDbCommand Command { get; }
    }
#else
    /// <summary>
    /// Describes a reader that controls the lifetime of both a command and a reader,
    /// exposing the downstream command/reader as properties.
    /// </summary>
    public interface IWrappedDataReader : IDataReader
    {
        /// <summary>
        /// Obtain the underlying reader
        /// </summary>
        IDataReader Reader { get; }
        /// <summary>
        /// Obtain the underlying command
        /// </summary>
        IDbCommand Command { get; }
    }
#endif
}
