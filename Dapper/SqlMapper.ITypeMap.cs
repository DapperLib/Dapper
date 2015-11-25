using System;
using System.Reflection;

namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Implement this interface to change default mapping of reader columns to type members
        /// </summary>
        public interface ITypeMap
        {
            /// <summary>
            /// Finds best constructor
            /// </summary>
            /// <param name="names">DataReader column names</param>
            /// <param name="types">DataReader column types</param>
            /// <returns>Matching constructor or default one</returns>
            ConstructorInfo FindConstructor(string[] names, Type[] types);

            /// <summary>
            /// Returns a constructor which should *always* be used.
            /// 
            /// Parameters will be default values, nulls for reference types and zero'd for value types.
            /// 
            /// Use this class to force object creation away from parameterless constructors you don't control.
            /// </summary>
            ConstructorInfo FindExplicitConstructor();

            /// <summary>
            /// Gets mapping for constructor parameter
            /// </summary>
            /// <param name="constructor">Constructor to resolve</param>
            /// <param name="columnName">DataReader column name</param>
            /// <returns>Mapping implementation</returns>
            IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName);

            /// <summary>
            /// Gets member mapping for column
            /// </summary>
            /// <param name="columnName">DataReader column name</param>
            /// <returns>Mapping implementation</returns>
            IMemberMap GetMember(string columnName);
        }
    }
}
