using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    public static partial class SqlMapper
    {
        public partial class GridReader
#if NET5_0_OR_GREATER
            : IAsyncDisposable
#endif
        {
            /// <summary>
            /// Read the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            /// <param name="buffered">Whether to buffer the results.</param>
            public Task<IEnumerable<dynamic>> ReadAsync(bool buffered = true) => ReadAsyncImpl<dynamic>(typeof(DapperRow), buffered);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public Task<dynamic> ReadFirstAsync() => ReadRowAsyncImpl<dynamic>(typeof(DapperRow), Row.First);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public Task<dynamic?> ReadFirstOrDefaultAsync() => ReadRowAsyncImpl<dynamic?>(typeof(DapperRow), Row.FirstOrDefault);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public Task<dynamic> ReadSingleAsync() => ReadRowAsyncImpl<dynamic>(typeof(DapperRow), Row.Single);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public Task<dynamic?> ReadSingleOrDefaultAsync() => ReadRowAsyncImpl<dynamic?>(typeof(DapperRow), Row.SingleOrDefault);

            /// <summary>
            /// Read the next grid of results
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <param name="buffered">Whether to buffer the results.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public Task<IEnumerable<object>> ReadAsync(Type type, bool buffered = true)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadAsyncImpl<object>(type, buffered);
            }

            /// <summary>
            /// Read an individual row of the next grid of results
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public Task<object> ReadFirstAsync(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRowAsyncImpl<object>(type, Row.First);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public Task<object?> ReadFirstOrDefaultAsync(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRowAsyncImpl<object?>(type, Row.FirstOrDefault);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public Task<object> ReadSingleAsync(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRowAsyncImpl<object>(type, Row.Single);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public Task<object?> ReadSingleOrDefaultAsync(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRowAsyncImpl<object?>(type, Row.SingleOrDefault);
            }

            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            /// <param name="buffered">Whether the results should be buffered in memory.</param>
            public Task<IEnumerable<T>> ReadAsync<T>(bool buffered = true) => ReadAsyncImpl<T>(typeof(T), buffered);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public Task<T> ReadFirstAsync<T>() => ReadRowAsyncImpl<T>(typeof(T), Row.First);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public Task<T?> ReadFirstOrDefaultAsync<T>() => ReadRowAsyncImpl<T?>(typeof(T), Row.FirstOrDefault);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public Task<T> ReadSingleAsync<T>() => ReadRowAsyncImpl<T>(typeof(T), Row.Single);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public Task<T?> ReadSingleOrDefaultAsync<T>() => ReadRowAsyncImpl<T?>(typeof(T), Row.SingleOrDefault);

            /// <summary>
            /// Marks the current grid as consumed, and moves to the next result
            /// </summary>
            protected async Task OnAfterGridAsync(int index)
            {
                if (index != ResultIndex)
                {
                    // not our data
                }
                else if (reader is null)
                {
                    // nothing to do
                }
                else if (await reader.NextResultAsync(cancel).ConfigureAwait(false))
                {
                    // readCount++;
                    _resultIndexAndConsumedFlag = index + 1;
                }
                else
                {
                    // happy path; close the reader cleanly - no
                    // need for "Cancel" etc
#if NET5_0_OR_GREATER
                    await reader.DisposeAsync();
#else
                    reader.Dispose();
#endif
                    reader = null!;
                    onCompleted?.Invoke(state);
#if NET5_0_OR_GREATER
                    await DisposeAsync();
#else
                    Dispose();
#endif
                }
            }

            private Task<IEnumerable<T>> ReadAsyncImpl<T>(Type type, bool buffered)
            {
                var deserializer = ValidateAndMarkConsumed(type, out var index);
                if (buffered)
                {
                    return ReadBufferedAsync<T>(index, deserializer);
                }
                else
                {
                    var result = ReadDeferred<T>(index, deserializer, type);
                    return Task.FromResult(result);
                }
            }

            private Func<DbDataReader, object> ValidateAndMarkConsumed(Type type, out int index)
            {
                index = OnBeforeGrid();
                var typedIdentity = Identity.ForGrid(type, index);
                CacheInfo cache = GetCacheInfo(typedIdentity, null, addToCache);
                var deserializer = cache.Deserializer;

                int hash = GetColumnHash(reader);
                if (deserializer.Func is null || deserializer.Hash != hash)
                {
                    deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                    cache.Deserializer = deserializer;
                }
                return deserializer.Func;
            }

            private async Task<T> ReadRowAsyncImpl<T>(Type type, Row row)
            {
                var index = OnBeforeGrid();

                T result = default!;
                if (await reader.ReadAsync(cancel).ConfigureAwait(false) && reader.FieldCount != 0)
                {
                    var typedIdentity = Identity.ForGrid(type, index);
                    CacheInfo cache = GetCacheInfo(typedIdentity, null, addToCache);
                    var deserializer = cache.Deserializer;

                    int hash = GetColumnHash(reader);
                    if (deserializer.Func is null || deserializer.Hash != hash)
                    {
                        deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                        cache.Deserializer = deserializer;
                    }

                    result = ConvertTo<T>(deserializer.Func(reader));

                    if ((row & Row.Single) != 0 && await reader.ReadAsync(cancel).ConfigureAwait(false)) ThrowMultipleRows(row);
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false)) { /* ignore subsequent rows */ }
                }
                else if ((row & Row.FirstOrDefault) == 0) // demanding a row, and don't have one
                {
                    ThrowZeroRows(row);
                }
                await OnAfterGridAsync(index).ConfigureAwait(false);
                return result;
            }

            private async Task<IEnumerable<T>> ReadBufferedAsync<T>(int index, Func<DbDataReader, object> deserializer)
            {
                try
                {
                    var buffer = new List<T>();
                    while (index == ResultIndex && await reader!.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        buffer.Add(ConvertTo<T>(deserializer(reader)));
                    }
                    return buffer;
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    await OnAfterGridAsync(index).ConfigureAwait(false);
                }
            }

#if NET5_0_OR_GREATER
            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public IAsyncEnumerable<T> ReadUnbufferedAsync<T>() => ReadAsyncUnbufferedImpl<T>(typeof(T));

            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            public IAsyncEnumerable<dynamic> ReadUnbufferedAsync() => ReadAsyncUnbufferedImpl<dynamic>(typeof(DapperRow));

            private IAsyncEnumerable<T> ReadAsyncUnbufferedImpl<T>(Type type)
            {
                var deserializer = ValidateAndMarkConsumed(type, out var index);
                return ReadUnbufferedAsync<T>(index, deserializer, cancel);
            }

            private async IAsyncEnumerable<T> ReadUnbufferedAsync<T>(int index, Func<DbDataReader, object> deserializer, [EnumeratorCancellation] CancellationToken cancel)
            {
                try
                {
                    while (index == ResultIndex && await reader!.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        yield return ConvertTo<T>(deserializer(reader));
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    await OnAfterGridAsync(index).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public async ValueTask DisposeAsync()
            {
                if (reader is not null)
                {
                    if (!reader.IsClosed) Command?.Cancel();
                    await reader.DisposeAsync();
                    reader = null!;
                }
                if (Command is not null)
                {
                    if (Command is DbCommand typed)
                    {
                        await typed.DisposeAsync();
                    }
                    else
                    {
                        Command.Dispose();
                    }
                    Command = null!;
                }
                GC.SuppressFinalize(this);
            }
#endif
        }
    }
}
