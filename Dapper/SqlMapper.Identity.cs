using System;
using System.Data;

namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Identity of a cached query in Dapper, used for extensibility
        /// </summary>
        public class Identity : IEquatable<Identity>
        {
            internal Identity ForGrid(Type primaryType, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, null, gridIndex);
            }

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);
            }
            /// <summary>
            /// Create an identity for use with DynamicParameters, internal use only
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public Identity ForDynamicParameters(Type type)
            {
                return new Identity(sql, commandType, connectionString, this.type, type, null, -1);
            }

            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType, Type[] otherTypes)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, otherTypes, 0)
            { }
            private Identity(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, Type[] otherTypes, int gridIndex)
            {
                this.sql = sql;
                this.commandType = commandType;
                this.connectionString = connectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.gridIndex = gridIndex;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                    hashCode = hashCode * 23 + commandType.GetHashCode();
                    hashCode = hashCode * 23 + gridIndex.GetHashCode();
                    hashCode = hashCode * 23 + (sql?.GetHashCode() ?? 0);
                    hashCode = hashCode * 23 + (type?.GetHashCode() ?? 0);
                    if (otherTypes != null)
                    {
                        foreach (var t in otherTypes)
                        {
                            hashCode = hashCode * 23 + (t?.GetHashCode() ?? 0);
                        }
                    }
                    hashCode = hashCode * 23 + (connectionString == null ? 0 : connectionStringComparer.GetHashCode(connectionString));
                    hashCode = hashCode * 23 + (parametersType?.GetHashCode() ?? 0);
                }
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return Equals(obj as Identity);
            }
            /// <summary>
            /// The sql
            /// </summary>
            public readonly string sql;
            /// <summary>
            /// The command type
            /// </summary>
            public readonly CommandType? commandType;

            /// <summary>
            ///
            /// </summary>
            public readonly int hashCode, gridIndex;
            /// <summary>
            ///
            /// </summary>
            public readonly Type type;
            /// <summary>
            ///
            /// </summary>
            public readonly string connectionString;
            /// <summary>
            ///
            /// </summary>
            public readonly Type parametersType;
            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return hashCode;
            }
            /// <summary>
            /// Compare 2 Identity objects
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(Identity other)
            {
                return
                    other != null &&
                    gridIndex == other.gridIndex &&
                    type == other.type &&
                    sql == other.sql &&
                    commandType == other.commandType &&
                    connectionStringComparer.Equals(connectionString, other.connectionString) &&
                    parametersType == other.parametersType;
            }
        }
    }
}
