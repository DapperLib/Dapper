﻿#if ASYNC
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if DNXCORE50
using IDbDataParameter = global::System.Data.Common.DbParameter;
using IDataParameter = global::System.Data.Common.DbParameter;
using IDbTransaction = global::System.Data.Common.DbTransaction;
using IDbConnection = global::System.Data.Common.DbConnection;
using IDbCommand = global::System.Data.Common.DbCommand;
using IDataReader = global::System.Data.Common.DbDataReader;
using IDataRecord = global::System.Data.Common.DbDataReader;
using IDataParameterCollection = global::System.Data.Common.DbParameterCollection;
using DataException = global::System.InvalidOperationException;
using ApplicationException = global::System.InvalidOperationException;
#endif
namespace Dapper
{
    partial class SqlMapper
    {
        partial class GridReader
        {
            CancellationToken cancel;
            internal GridReader(IDbCommand command, IDataReader reader, Identity identity, DynamicParameters dynamicParams, bool addToCache, CancellationToken cancel)
                : this(command, reader, identity, dynamicParams, addToCache)
            {
                this.cancel = cancel;
            }

            /// <summary>
            /// Read the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public Task<IEnumerable<dynamic>> ReadAsync(bool buffered = true)
            {
                return ReadAsyncImpl<dynamic>(typeof(DapperRow), buffered);
            }

            /// <summary>
            /// Read the next grid of results
            /// </summary>
            public Task<IEnumerable<object>> ReadAsync(Type type, bool buffered = true)
            {
                if (type == null) throw new ArgumentNullException("type");
                return ReadAsyncImpl<object>(type, buffered);
            }
            /// <summary>
            /// Read the next grid of results
            /// </summary>
            public Task<IEnumerable<T>> ReadAsync<T>(bool buffered = true)
            {
                return ReadAsyncImpl<T>(typeof(T), buffered);
            }

            private async Task NextResultAsync()
            {
                if (await ((DbDataReader)reader).NextResultAsync(cancel).ConfigureAwait(false))
                {
                    readCount++;
                    gridIndex++;
                    consumed = false;
                }
                else
                {
                    // happy path; close the reader cleanly - no
                    // need for "Cancel" etc
                    reader.Dispose();
                    reader = null;
                    if (callbacks != null) callbacks.OnCompleted();
                    Dispose();
                }
            }

            private Task<IEnumerable<T>> ReadAsyncImpl<T>(Type type, bool buffered)
            {
                if (reader == null) throw new ObjectDisposedException(GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
                if (consumed) throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                var typedIdentity = identity.ForGrid(type, gridIndex);
                CacheInfo cache = GetCacheInfo(typedIdentity, null, addToCache);
                var deserializer = cache.Deserializer;

                int hash = GetColumnHash(reader);
                if (deserializer.Func == null || deserializer.Hash != hash)
                {
                    deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                    cache.Deserializer = deserializer;
                }
                consumed = true;
                if (buffered && this.reader is DbDataReader)
                {
                    return ReadBufferedAsync<T>(gridIndex, deserializer.Func, typedIdentity);
                }
                else
                {
                    var result = ReadDeferred<T>(gridIndex, deserializer.Func, typedIdentity);
                    if (buffered) result = result.ToList(); // for the "not a DbDataReader" scenario
                    return Task.FromResult(result);
                }
            }

            private async Task<IEnumerable<T>> ReadBufferedAsync<T>(int index, Func<IDataReader, object> deserializer, Identity typedIdentity)
            {
                //try
                //{
                var reader = (DbDataReader)this.reader;
                List<T> buffer = new List<T>();
                while (index == gridIndex && await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    buffer.Add((T)deserializer(reader));
                }
                if (index == gridIndex) // need to do this outside of the finally pre-C#6
                {
                    await NextResultAsync().ConfigureAwait(false);
                }
                return buffer;
                //}
                //finally // finally so that First etc progresses things even when multiple rows
                //{
                //    if (index == gridIndex)
                //    {
                //        await NextResultAsync().ConfigureAwait(false);
                //    }
                //}
            }
        }

    }
}
#endif