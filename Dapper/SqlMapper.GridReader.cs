using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// The grid reader provides interfaces for reading multiple result sets from a Dapper query
        /// </summary>
        public partial class GridReader : IDisposable
        {
            private DbDataReader reader;
            private Identity? _identity;
            private readonly bool addToCache;
            private readonly Action<object?>? onCompleted;
            private readonly object? state;
            private readonly CancellationToken cancel;

            /// <summary>
            /// Creates a grid reader over an existing command and reader
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            protected GridReader(IDbCommand command, DbDataReader reader, Identity? identity, Action<object?>? onCompleted = null, object? state = null, bool addToCache = false, CancellationToken cancellationToken = default)
            {
                Command = command;
                this.reader = reader;
                _identity = identity;
                this.onCompleted = onCompleted;
                this.state = state;
                this.addToCache = addToCache;
                cancel = cancellationToken;
            }

            internal GridReader(IDbCommand command, DbDataReader reader, Identity identity, IParameterCallbacks? callbacks, bool addToCache,
                CancellationToken cancellationToken = default)
                : this(command, reader, identity, callbacks is null ? null : static state => ((IParameterCallbacks)state!).OnCompleted(),
                      callbacks, addToCache, cancellationToken)
            { }

            private Identity Identity => _identity ??= CreateIdentity();

            private Identity CreateIdentity()
            {
                var cmd = Command;
                if (cmd is not null && cmd.Connection is not null)
                {
                    return new Identity(cmd.CommandText, cmd.CommandType, cmd.Connection, null, null);
                }
                throw new InvalidOperationException("This operation requires an identity or a connected command");
            }

            /// <summary>
            /// Read the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <param name="buffered">Whether the results should be buffered in memory.</param>
            /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public IEnumerable<dynamic> Read(bool buffered = true) => ReadImpl<dynamic>(typeof(DapperRow), buffered);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public dynamic ReadFirst() => ReadRow<dynamic>(typeof(DapperRow), Row.First);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public dynamic? ReadFirstOrDefault() => ReadRow<dynamic>(typeof(DapperRow), Row.FirstOrDefault);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public dynamic ReadSingle() => ReadRow<dynamic>(typeof(DapperRow), Row.Single);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public dynamic? ReadSingleOrDefault() => ReadRow<dynamic>(typeof(DapperRow), Row.SingleOrDefault);

            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            /// <param name="buffered">Whether the results should be buffered in memory.</param>
            public IEnumerable<T> Read<T>(bool buffered = true) => ReadImpl<T>(typeof(T), buffered);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public T ReadFirst<T>() => ReadRow<T>(typeof(T), Row.First);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public T? ReadFirstOrDefault<T>() => ReadRow<T>(typeof(T), Row.FirstOrDefault);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public T ReadSingle<T>() => ReadRow<T>(typeof(T), Row.Single);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public T? ReadSingleOrDefault<T>() => ReadRow<T>(typeof(T), Row.SingleOrDefault);

            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <param name="buffered">Whether to buffer the results.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public IEnumerable<object> Read(Type type, bool buffered = true)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadImpl<object>(type, buffered);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object ReadFirst(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.First);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object? ReadFirstOrDefault(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.FirstOrDefault);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object ReadSingle(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.Single);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object? ReadSingleOrDefault(Type type)
            {
                if (type is null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.SingleOrDefault);
            }


            /// <summary>
            /// Validates that data is available, returning the <see cref="ResultIndex"/> that corresponds to the current grid - and marks the current grid as consumed;
            /// this call <em>must</em> be paired with a call to <see cref="OnAfterGrid(int)"/> or <see cref="OnAfterGridAsync(int)"/>
            /// </summary>
            protected int OnBeforeGrid()
            {
                if (reader is null) throw new ObjectDisposedException(GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
                if (IsConsumed) throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                _resultIndexAndConsumedFlag |= CONSUMED_FLAG;
                return ResultIndex;
            }

            private IEnumerable<T> ReadImpl<T>(Type type, bool buffered)
            {
                var index = OnBeforeGrid();
                var typedIdentity = Identity.ForGrid(type, index);
                CacheInfo cache = GetCacheInfo(typedIdentity, null, addToCache);
                var deserializer = cache.Deserializer;

                int hash = GetColumnHash(reader);
                if (deserializer.Func is null || deserializer.Hash != hash)
                {
                    deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                    cache.Deserializer = deserializer;
                }
                var result = ReadDeferred<T>(index, deserializer.Func, type);
                return buffered ? result.ToList() : result;
            }

            private T ReadRow<T>(Type type, Row row)
            {
                var index = OnBeforeGrid();

                T result = default!;
                if (reader.Read() && reader.FieldCount != 0)
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

                    if ((row & Row.Single) != 0 && reader.Read()) ThrowMultipleRows(row);
                    while (reader.Read()) { /* ignore subsequent rows */ }
                }
                else if ((row & Row.FirstOrDefault) == 0) // demanding a row, and don't have one
                {
                    ThrowZeroRows(row);
                }
                OnAfterGrid(index);
                return result;
            }

            private IEnumerable<TReturn> MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Delegate func, string splitOn)
            {
                var index = OnBeforeGrid();
                var identity = Identity.ForGrid<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(typeof(TReturn), index);

                try
                {
                    foreach (var r in MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(null, default, func, splitOn, reader, identity, false))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    OnAfterGrid(index);
                }
            }

            private IEnumerable<TReturn> MultiReadInternal<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn)
            {
                var index = OnBeforeGrid();
                var identity = Identity.ForGrid(typeof(TReturn), types, index);
                try
                {
                    foreach (var r in MultiMapImpl<TReturn>(null, default, types, map, splitOn, reader, identity, false))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    OnAfterGrid(index);
                }
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid.
            /// </summary>
            /// <typeparam name="TFirst">The first type in the record set.</typeparam>
            /// <typeparam name="TSecond">The second type in the record set.</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid.
            /// </summary>
            /// <typeparam name="TFirst">The first type in the record set.</typeparam>
            /// <typeparam name="TSecond">The second type in the record set.</typeparam>
            /// <typeparam name="TThird">The third type in the record set.</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TFirst">The first type in the record set.</typeparam>
            /// <typeparam name="TSecond">The second type in the record set.</typeparam>
            /// <typeparam name="TThird">The third type in the record set.</typeparam>
            /// <typeparam name="TFourth">The fourth type in the record set.</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TFirst">The first type in the record set.</typeparam>
            /// <typeparam name="TSecond">The second type in the record set.</typeparam>
            /// <typeparam name="TThird">The third type in the record set.</typeparam>
            /// <typeparam name="TFourth">The fourth type in the record set.</typeparam>
            /// <typeparam name="TFifth">The fifth type in the record set.</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TFirst">The first type in the record set.</typeparam>
            /// <typeparam name="TSecond">The second type in the record set.</typeparam>
            /// <typeparam name="TThird">The third type in the record set.</typeparam>
            /// <typeparam name="TFourth">The fourth type in the record set.</typeparam>
            /// <typeparam name="TFifth">The fifth type in the record set.</typeparam>
            /// <typeparam name="TSixth">The sixth type in the record set.</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TFirst">The first type in the record set.</typeparam>
            /// <typeparam name="TSecond">The second type in the record set.</typeparam>
            /// <typeparam name="TThird">The third type in the record set.</typeparam>
            /// <typeparam name="TFourth">The fourth type in the record set.</typeparam>
            /// <typeparam name="TFifth">The fifth type in the record set.</typeparam>
            /// <typeparam name="TSixth">The sixth type in the record set.</typeparam>
            /// <typeparam name="TSeventh">The seventh type in the record set.</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="types">The types to read from the result set.</param>
            /// <param name="map">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal(types, map, splitOn);
                return buffered ? result.ToList() : result;
            }

            private IEnumerable<T> ReadDeferred<T>(int index, Func<DbDataReader, object> deserializer, Type effectiveType)
            {
                try
                {
                    while (index == ResultIndex && reader?.Read() == true)
                    {
                        yield return ConvertTo<T>(deserializer(reader));
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    OnAfterGrid(index);
                }
            }

            const int CONSUMED_FLAG = 1 << 31;
            private int _resultIndexAndConsumedFlag; //, readCount;

            /// <summary>
            /// Indicates the current result index
            /// </summary>
            protected int ResultIndex => _resultIndexAndConsumedFlag & ~CONSUMED_FLAG;

            /// <summary>
            /// Has the underlying reader been consumed?
            /// </summary>
            /// <remarks>This also reports <c>true</c> if the current grid is actively being consumed</remarks>
            public bool IsConsumed => (_resultIndexAndConsumedFlag & CONSUMED_FLAG) != 0;

            /// <summary>
            /// The command associated with the reader
            /// </summary>
            public IDbCommand Command { get; set; }

            /// <summary>
            /// The underlying reader
            /// </summary>
            protected DbDataReader Reader => reader;

            /// <summary>
            /// The cancellation token associated with this reader
            /// </summary>
            protected CancellationToken CancellationToken => cancel;

            /// <summary>
            /// Marks the current grid as consumed, and moves to the next result
            /// </summary>
            protected void OnAfterGrid(int index)
            {
                if (index != ResultIndex)
                {
                    // not our data!
                }
                else if (reader is null)
                {
                    // nothing to do
                }
                else if (reader.NextResult())
                {
                    // readCount++;
                    _resultIndexAndConsumedFlag = index + 1;
                }
                else
                {
                    // happy path; close the reader cleanly - no
                    // need for "Cancel" etc
                    reader.Dispose();
                    reader = null!;
                    onCompleted?.Invoke(state);
                    Dispose();
                }
            }

            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public void Dispose()
            {
                if (reader is not null)
                {
                    if (!reader.IsClosed) Command?.Cancel();
                    reader.Dispose();
                    reader = null!;
                }
                if (Command is not null)
                {
                    Command.Dispose();
                    Command = null!;
                }
                GC.SuppressFinalize(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static T ConvertTo<T>(object? value) => value switch
            {
                T typed => typed,
                null or DBNull => default!,
                _ => (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture),
            };
        }
    }
}
