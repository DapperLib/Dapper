using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// The grid reader provides interfaces for reading multiple result sets from a Dapper query
        /// </summary>
        public partial class GridReader : IDisposable
        {
            private IDataReader reader;
            private readonly Identity identity;
            private readonly bool addToCache;

            internal GridReader(IDbCommand command, IDataReader reader, Identity identity, IParameterCallbacks callbacks, bool addToCache)
            {
                Command = command;
                this.reader = reader;
                this.identity = identity;
                this.callbacks = callbacks;
                this.addToCache = addToCache;
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
            public dynamic ReadFirstOrDefault() => ReadRow<dynamic>(typeof(DapperRow), Row.FirstOrDefault);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public dynamic ReadSingle() => ReadRow<dynamic>(typeof(DapperRow), Row.Single);

            /// <summary>
            /// Read an individual row of the next grid of results, returned as a dynamic object.
            /// </summary>
            /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
            public dynamic ReadSingleOrDefault() => ReadRow<dynamic>(typeof(DapperRow), Row.SingleOrDefault);

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
            public T ReadFirstOrDefault<T>() => ReadRow<T>(typeof(T), Row.FirstOrDefault);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public T ReadSingle<T>() => ReadRow<T>(typeof(T), Row.Single);

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <typeparam name="T">The type to read.</typeparam>
            public T ReadSingleOrDefault<T>() => ReadRow<T>(typeof(T), Row.SingleOrDefault);

            /// <summary>
            /// Read the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <param name="buffered">Whether to buffer the results.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public IEnumerable<object> Read(Type type, bool buffered = true)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return ReadImpl<object>(type, buffered);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object ReadFirst(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.First);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object ReadFirstOrDefault(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.FirstOrDefault);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object ReadSingle(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.Single);
            }

            /// <summary>
            /// Read an individual row of the next grid of results.
            /// </summary>
            /// <param name="type">The type to read.</param>
            /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
            public object ReadSingleOrDefault(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return ReadRow<object>(type, Row.SingleOrDefault);
            }

            private IEnumerable<T> ReadImpl<T>(Type type, bool buffered)
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
                var result = ReadDeferred<T>(gridIndex, deserializer.Func, type);
                return buffered ? result.ToList() : result;
            }

            private T ReadRow<T>(Type type, Row row)
            {
                if (reader == null) throw new ObjectDisposedException(GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
                if (IsConsumed) throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                IsConsumed = true;

                T result = default(T);
                if (reader.Read() && reader.FieldCount != 0)
                {
                    var typedIdentity = identity.ForGrid(type, gridIndex);
                    CacheInfo cache = GetCacheInfo(typedIdentity, null, addToCache);
                    var deserializer = cache.Deserializer;

                    int hash = GetColumnHash(reader);
                    if (deserializer.Func == null || deserializer.Hash != hash)
                    {
                        deserializer = new DeserializerState(hash, GetDeserializer(type, reader, 0, -1, false));
                        cache.Deserializer = deserializer;
                    }
                    object val = deserializer.Func(reader);
                    if (val == null || val is T)
                    {
                        result = (T)val;
                    }
                    else
                    {
                        var convertToType = Nullable.GetUnderlyingType(type) ?? type;
                        result = (T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
                    }
                    if ((row & Row.Single) != 0 && reader.Read()) ThrowMultipleRows(row);
                    while (reader.Read()) { /* ignore subsequent rows */ }
                }
                else if ((row & Row.FirstOrDefault) == 0) // demanding a row, and don't have one
                {
                    ThrowZeroRows(row);
                }
                NextResult();
                return result;
            }

            private IEnumerable<TReturn> MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Delegate func, string splitOn)
            {
                var identity = this.identity.ForGrid<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(typeof(TReturn), gridIndex);

                IsConsumed = true;

                try
                {
                    foreach (var r in MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(null, default(CommandDefinition), func, splitOn, reader, identity, false))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    NextResult();
                }
            }

            private IEnumerable<TReturn> MultiReadInternal<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn)
            {
                var identity = this.identity.ForGrid(typeof(TReturn), types, gridIndex);
                try
                {
                    foreach (var r in MultiMapImpl<TReturn>(null, default(CommandDefinition), types, map, splitOn, reader, identity, false))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    NextResult();
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

            private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, object> deserializer, Type effectiveType)
            {
                try
                {
                    var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                    while (index == gridIndex && reader.Read())
                    {
                        object val = deserializer(reader);
                        if (val == null || val is T)
                        {
                            yield return (T)val;
                        }
                        else
                        {
                            yield return (T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
                        }
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == gridIndex)
                    {
                        NextResult();
                    }
                }
            }

            private int gridIndex, readCount;
            private readonly IParameterCallbacks callbacks;

            /// <summary>
            /// Has the underlying reader been consumed?
            /// </summary>
            public bool IsConsumed { get; private set; }

            /// <summary>
            /// The command associated with the reader
            /// </summary>
            public IDbCommand Command { get; set; }

            private void NextResult()
            {
                if (reader.NextResult())
                {
                    readCount++;
                    gridIndex++;
                    IsConsumed = false;
                }
                else
                {
                    // happy path; close the reader cleanly - no
                    // need for "Cancel" etc
                    reader.Dispose();
                    reader = null;
                    callbacks?.OnCompleted();
                    Dispose();
                }
            }

            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public void Dispose()
            {
                if (reader != null)
                {
                    if (!reader.IsClosed) Command?.Cancel();
                    reader.Dispose();
                    reader = null;
                }
                if (Command != null)
                {
                    Command.Dispose();
                    Command = null;
                }
            }
        }
    }
}
