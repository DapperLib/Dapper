#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Dapper
{
    public static partial class SqlMapper
    {
        public partial class GridReader
        {
            /// <summary>
            /// Read the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            /// <param name="buffered">Whether to buffer the results.</param>
            public IAsyncEnumerable<dynamic> StreamAsync(bool buffered = false) => StreamAsyncImpl<dynamic>(typeof(DapperRow), buffered);

            /// <summary>
            /// Read the next grid of results
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <param name="buffered">Whether to buffer the results.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public IAsyncEnumerable<object> StreamAsync(Type type, bool buffered = false)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return StreamAsyncImpl<object>(type, buffered);
            }

            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            /// <param name="buffered">Whether the results should be buffered in memory.</param>
            public IAsyncEnumerable<T> StreamAsync<T>(bool buffered = false) => StreamAsyncImpl<T>(typeof(T), buffered);

            private async IAsyncEnumerable<T> StreamAsyncImpl<T>(Type type, bool buffered)
            {
                if (reader == null) throw new ObjectDisposedException(GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
                if (IsConsumed) throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                var typedIdentity = identity.ForGrid(type, gridIndex);
                CacheInfo cache = GetCacheInfo(typedIdentity, null, addToCache);
                var deserializer = cache.Deserializer;

                int hash = GetColumnHash(reader);
                if (deserializer.Func == null || deserializer.Hash != hash)
                {
                    deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                    cache.Deserializer = deserializer;
                }
                IsConsumed = true;
                if (reader is DbDataReader)
                {
                    var buffer = buffered ? new List<T>() : null;

                    await foreach (var value in ReadStreamAsync<T>(gridIndex, deserializer.Func).WithCancellation(cancel).ConfigureAwait(false))
                    {
                        if (buffer != null) buffer.Add(value);
                        else yield return value;
                    }

                    if (buffer == null) yield break;
                    foreach (var value in buffer) yield return value;
                }
                else
                {
                    var result = ReadDeferred<T>(gridIndex, deserializer.Func, type);
                    if (buffered) result = result?.ToList(); // for the "not a DbDataReader" scenario

                    if(result == null) yield break;
                    foreach (var value in result) yield return value;
                }
            }

            private async IAsyncEnumerable<T> ReadStreamAsync<T>(int index, Func<IDataReader, object> deserializer)
            {
                try
                {
                    var dbReader = (DbDataReader)this.reader;
                    while (index == gridIndex && await dbReader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        yield return ConvertTo<T>(deserializer(dbReader));
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == gridIndex)
                    {
                        await NextResultAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
#endif // NETCOREAPP
