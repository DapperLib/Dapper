using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapper.ProviderTools.Internal;
#nullable enable
namespace Dapper.ProviderTools
{
    /// <summary>
    /// Provides provider-agnostic access to bulk-copy services
    /// </summary>
    public abstract class BulkCopy : IDisposable
    {
        /// <summary>
        /// Attempt to create a BulkCopy instance for the connection provided
        /// </summary>
        public static BulkCopy? TryCreate(DbConnection connection)
        {
            if (connection == null) return null;
            var type = connection.GetType();
            if (!s_bcpFactory.TryGetValue(type, out var func))
            {
                s_bcpFactory[type] = func = CreateBcpFactory(type);
            }
            var obj = func?.Invoke(connection);
            return DynamicBulkCopy.Create(obj);
        }

        /// <summary>
        /// Create a BulkCopy instance for the connection provided
        /// </summary>
        public static BulkCopy Create(DbConnection connection)
        {
            var bcp = TryCreate(connection);
            if (bcp == null)
            {
                if (connection == null) throw new ArgumentNullException(nameof(connection));
                throw new NotSupportedException("Unable to create BulkCopy for " + connection.GetType().FullName);
            }
            return bcp;
        }

        ///// <summary>
        ///// Provide an external registration for a given connection type
        ///// </summary>
        //public static void Register(Type type, Func<DbConnection, BulkCopy> factory)
        //{
        //    throw new NotImplementedException();
        //}

        private static readonly ConcurrentDictionary<Type, Func<DbConnection, object>?> s_bcpFactory
            = new ConcurrentDictionary<Type, Func<DbConnection, object>?>();

        internal static Func<DbConnection, object>? CreateBcpFactory(Type connectionType)
        {
            try
            {
                var match = Regex.Match(connectionType.Name, "^(.+)Connection$");
                if (match.Success)
                {
                    var prefix = match.Groups[1].Value;
                    var bcpType = connectionType.Assembly.GetType($"{connectionType.Namespace}.{prefix}BulkCopy");
                    if (bcpType != null)
                    {
                        var ctor = bcpType.GetConstructor(new[] { connectionType });
                        if (ctor == null) return null;

                        var p = Expression.Parameter(typeof(DbConnection), "conn");
                        var body = Expression.New(ctor, Expression.Convert(p, connectionType));
                        return Expression.Lambda<Func<DbConnection, object>>(body, p).Compile();
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Name of the destination table on the server.
        /// </summary>
        public abstract string DestinationTableName { get; set; }
        /// <summary>
        /// Write a set of data to the server
        /// </summary>
        public abstract void WriteToServer(DataTable source);
        /// <summary>
        /// Write a set of data to the server
        /// </summary>
        public abstract void WriteToServer(DataRow[] source);
        /// <summary>
        /// Write a set of data to the server
        /// </summary>
        public abstract void WriteToServer(IDataReader source);
        /// <summary>
        /// Write a set of data to the server
        /// </summary>
        public abstract Task WriteToServerAsync(DbDataReader source, CancellationToken cancellationToken = default);
        /// <summary>
        /// Write a set of data to the server
        /// </summary>
        public abstract Task WriteToServerAsync(DataTable source, CancellationToken cancellationToken = default);
        /// <summary>
        /// Write a set of data to the server
        /// </summary>
        public abstract Task WriteToServerAsync(DataRow[] source, CancellationToken cancellationToken = default);
        /// <summary>
        /// Add a mapping between two columns by name
        /// </summary>
        public abstract void AddColumnMapping(string sourceColumn, string destinationColumn);
        /// <summary>
        /// Add a mapping between two columns by position
        /// </summary>
        public abstract void AddColumnMapping(int sourceColumn, int destinationColumn);
        /// <summary>
        /// The underlying untyped object providing the bulk-copy service
        /// </summary>
        public abstract object Wrapped { get; }

        /// <summary>
        /// Enables or disables streaming from a data-reader
        /// </summary>
        public bool EnableStreaming { get; set; }
        /// <summary>
        /// Number of rows in each batch
        /// </summary>
        public int BatchSize { get; set; }
        /// <summary>
        /// Number of seconds for the operation to complete before it times out.
        /// </summary>
        public int BulkCopyTimeout { get; set; }

        /// <summary>
        /// Release any resources associated with this instance
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Release any resources associated with this instance
        /// </summary>
        protected abstract void Dispose(bool disposing);
    }
}
