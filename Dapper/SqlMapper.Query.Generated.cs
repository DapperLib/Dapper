
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD1_3
using DataException = System.InvalidOperationException;
#endif

namespace Dapper
{
    public static partial class SqlMapper
    {
    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, TReturn>(this IDbConnection cnn, string sql,Func<T0, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,DontMap,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,DontMap,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,DontMap,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    
        /// <summary>
        /// Perform a multi-mapping query with 7 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
		/// <typeparam name="T29">29 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>        
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static IEnumerable<TReturn> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn>(this IDbConnection cnn, string sql,Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMap<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);

    

    private static Func<IDataReader, TReturn> GenerateMapper<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, object map)
    {
        switch (otherDeserializers.Length)
        {
            
            case 1:
                return r => ((Func<T0, TReturn>)map)((T0)deserializer(r));
            
            case 2:
                return r => ((Func<T0, T1, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r));
            
            case 3:
                return r => ((Func<T0, T1, T2, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r));
            
            case 4:
                return r => ((Func<T0, T1, T2, T3, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r));
            
            case 5:
                return r => ((Func<T0, T1, T2, T3, T4, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r));
            
            case 6:
                return r => ((Func<T0, T1, T2, T3, T4, T5, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r));
            
            case 7:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r));
            
            case 8:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r));
            
            case 9:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r));
            
            case 10:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r));
            
            case 11:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r));
            
            case 12:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r));
            
            case 13:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r));
            
            case 14:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r));
            
            case 15:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r));
            
            case 16:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r));
            
            case 17:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r));
            
            case 18:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r));
            
            case 19:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r));
            
            case 20:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r));
            
            case 21:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r));
            
            case 22:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r));
            
            case 23:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r));
            
            case 24:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r));
            
            case 25:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r),(T24)otherDeserializers[23](r));
            
            case 26:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r),(T24)otherDeserializers[23](r),(T25)otherDeserializers[24](r));
            
            case 27:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r),(T24)otherDeserializers[23](r),(T25)otherDeserializers[24](r),(T26)otherDeserializers[25](r));
            
            case 28:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r),(T24)otherDeserializers[23](r),(T25)otherDeserializers[24](r),(T26)otherDeserializers[25](r),(T27)otherDeserializers[26](r));
            
            case 29:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r),(T24)otherDeserializers[23](r),(T25)otherDeserializers[24](r),(T26)otherDeserializers[25](r),(T27)otherDeserializers[26](r),(T28)otherDeserializers[27](r));
            
            case 30:
                return r => ((Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn>)map)((T0)deserializer(r), (T1)otherDeserializers[0](r),(T2)otherDeserializers[1](r),(T3)otherDeserializers[2](r),(T4)otherDeserializers[3](r),(T5)otherDeserializers[4](r),(T6)otherDeserializers[5](r),(T7)otherDeserializers[6](r),(T8)otherDeserializers[7](r),(T9)otherDeserializers[8](r),(T10)otherDeserializers[9](r),(T11)otherDeserializers[10](r),(T12)otherDeserializers[11](r),(T13)otherDeserializers[12](r),(T14)otherDeserializers[13](r),(T15)otherDeserializers[14](r),(T16)otherDeserializers[15](r),(T17)otherDeserializers[16](r),(T18)otherDeserializers[17](r),(T19)otherDeserializers[18](r),(T20)otherDeserializers[19](r),(T21)otherDeserializers[20](r),(T22)otherDeserializers[21](r),(T23)otherDeserializers[22](r),(T24)otherDeserializers[23](r),(T25)otherDeserializers[24](r),(T26)otherDeserializers[25](r),(T27)otherDeserializers[26](r),(T28)otherDeserializers[27](r),(T29)otherDeserializers[28](r));
                      
            default:
                throw new NotSupportedException();
        }
    }
    

    private static IEnumerable<TReturn> MultiMap<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(
            this IDbConnection cnn, string sql, Delegate map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
            var results = MultiMapImpl<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(cnn, command, map, splitOn, null, null, true);
            return buffered ? results.ToList() : results;
        }


    
    private static IEnumerable<TReturn> MultiMapImpl<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this IDbConnection cnn, CommandDefinition command, Delegate map, string splitOn, IDataReader reader, Identity identity, bool finalize)
        {
            object param = command.Parameters;
            identity = identity ?? new Identity(command.CommandText, command.CommandType, cnn, typeof(T0), param?.GetType(), new[] {                 
                typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7),typeof(T8),typeof(T9),typeof(T10),typeof(T11),typeof(T12),typeof(T13),typeof(T14),typeof(T15)
            });
            CacheInfo cinfo = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand ownedCommand = null;
            IDataReader ownedReader = null;

            bool wasClosed = cnn?.State == ConnectionState.Closed;
            try
            {
                if (reader == null)
                {
                    ownedCommand = command.SetupCommand(cnn, cinfo.ParamReader);
                    if (wasClosed) cnn.Open();
                    ownedReader = ExecuteReaderWithFlagsFallback(ownedCommand, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
                    reader = ownedReader;
                }
                var deserializer = default(DeserializerState);
                Func<IDataReader, object>[] otherDeserializers;

                int hash = GetColumnHash(reader);
                if ((deserializer = cinfo.Deserializer).Func == null || (otherDeserializers = cinfo.OtherDeserializers) == null || hash != deserializer.Hash)
                {
                    var deserializers = GenerateDeserializers(new[] { 
                            typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7),typeof(T8),typeof(T9),typeof(T10),typeof(T11),typeof(T12),typeof(T13),typeof(T14),typeof(T15)
                        }, splitOn, reader);
                    deserializer = cinfo.Deserializer = new DeserializerState(hash, deserializers[0]);
                    otherDeserializers = cinfo.OtherDeserializers = deserializers.Skip(1).ToArray();
                    if (command.AddToCache) SetQueryCache(identity, cinfo);
                }

                Func<IDataReader, TReturn> mapIt = GenerateMapper<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(deserializer.Func, otherDeserializers, map);

                if (mapIt != null)
                {
                    while (reader.Read())
                    {
                        yield return mapIt(reader);
                    }
                    if (finalize)
                    {
                        while (reader.NextResult()) { /* ignore remaining result sets */ }
                        command.OnCompleted();
                    }
                }
            }
            finally
            {
                try
                {
                    ownedReader?.Dispose();
                }
                finally
                {
                    ownedCommand?.Dispose();
                    if (wasClosed) cnn.Close();
                }
            }
        }


    private static async Task<IEnumerable<TReturn>> MultiMapAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this IDbConnection cnn, CommandDefinition command, Delegate map, string splitOn)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, typeof(T0), param?.GetType(), new[] { 
                typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7),typeof(T8),typeof(T9),typeof(T10),typeof(T11),typeof(T12),typeof(T13),typeof(T14),typeof(T15)
            });
            var info = GetCacheInfo(identity, param, command.AddToCache);
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                using (var cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader))
                using (var reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, command.CancellationToken).ConfigureAwait(false))
                {
                    if (!command.Buffered) wasClosed = false; // handing back open reader; rely on command-behavior
                    var results = MultiMapImpl<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(null, CommandDefinition.ForCallback(command.Parameters), map, splitOn, reader, identity, true);
                    return command.Buffered ? results.ToList() : results;
                }
            }
            finally
            {
                if (wasClosed) cnn.Close();
            }
        }

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, TReturn>(this IDbConnection cnn, string sql, Func<T0, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,DontMap,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,DontMap,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,DontMap,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,DontMap,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,DontMap,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,DontMap,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,DontMap,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        
        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
		/// <typeparam name="T29">29 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn>(this IDbConnection cnn, string sql, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
		/// <typeparam name="T29">29 type in the recordset</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(cnn, command, map, splitOn);

        


        /// <summary>
        /// The grid reader provides interfaces for reading multiple result sets from a Dapper query
        /// </summary>
        public partial class GridReader : IDisposable
        {
            private IEnumerable<TReturn> MultiReadInternal<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(Delegate func, string splitOn)
            {
                var identity = this.identity.ForGrid(typeof(TReturn), new Type[] {
                   typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7),typeof(T8),typeof(T9),typeof(T10),typeof(T11),typeof(T12),typeof(T13),typeof(T14),typeof(T15)
                }, gridIndex);

                IsConsumed = true;

                try
                {
                    foreach (var r in MultiMapImpl<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(null, default(CommandDefinition), func, splitOn, reader, identity, false))
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
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, TReturn>(Func<T0, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, TReturn>(Func<T0, T1, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, TReturn>(Func<T0, T1, T2, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, TReturn>(Func<T0, T1, T2, T3, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, TReturn>(Func<T0, T1, T2, T3, T4, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, TReturn>(Func<T0, T1, T2, T3, T4, T5, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,DontMap,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,DontMap,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,DontMap,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,DontMap,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,DontMap,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,DontMap,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
           
		/// <typeparam name="T0">0 type in the recordset</typeparam>
		/// <typeparam name="T1">1 type in the recordset</typeparam>
		/// <typeparam name="T2">2 type in the recordset</typeparam>
		/// <typeparam name="T3">3 type in the recordset</typeparam>
		/// <typeparam name="T4">4 type in the recordset</typeparam>
		/// <typeparam name="T5">5 type in the recordset</typeparam>
		/// <typeparam name="T6">6 type in the recordset</typeparam>
		/// <typeparam name="T7">7 type in the recordset</typeparam>
		/// <typeparam name="T8">8 type in the recordset</typeparam>
		/// <typeparam name="T9">9 type in the recordset</typeparam>
		/// <typeparam name="T10">10 type in the recordset</typeparam>
		/// <typeparam name="T11">11 type in the recordset</typeparam>
		/// <typeparam name="T12">12 type in the recordset</typeparam>
		/// <typeparam name="T13">13 type in the recordset</typeparam>
		/// <typeparam name="T14">14 type in the recordset</typeparam>
		/// <typeparam name="T15">15 type in the recordset</typeparam>
		/// <typeparam name="T16">16 type in the recordset</typeparam>
		/// <typeparam name="T17">17 type in the recordset</typeparam>
		/// <typeparam name="T18">18 type in the recordset</typeparam>
		/// <typeparam name="T19">19 type in the recordset</typeparam>
		/// <typeparam name="T20">20 type in the recordset</typeparam>
		/// <typeparam name="T21">21 type in the recordset</typeparam>
		/// <typeparam name="T22">22 type in the recordset</typeparam>
		/// <typeparam name="T23">23 type in the recordset</typeparam>
		/// <typeparam name="T24">24 type in the recordset</typeparam>
		/// <typeparam name="T25">25 type in the recordset</typeparam>
		/// <typeparam name="T26">26 type in the recordset</typeparam>
		/// <typeparam name="T27">27 type in the recordset</typeparam>
		/// <typeparam name="T28">28 type in the recordset</typeparam>
		/// <typeparam name="T29">29 type in the recordset</typeparam>
            /// <typeparam name="TReturn">The type to return from the record set.</typeparam>
            /// <param name="func">The mapping function from the read types to the return type.</param>
            /// <param name="splitOn">The field(s) we should split and read the second object from (defaults to "id")</param>
            /// <param name="buffered">Whether to buffer results in memory.</param>
            public IEnumerable<TReturn> Read<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

             

        }         
    
    }// END class SqlMapper
}


