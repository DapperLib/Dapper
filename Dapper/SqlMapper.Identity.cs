using System;
using System.Data;
using System.Runtime.CompilerServices;

namespace Dapper
{
    public static partial class SqlMapper
    {
        internal sealed class Identity<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh> : Identity
        {
            private static readonly int s_typeHash;
            private static readonly int s_typeCount = CountNonTrivial(out s_typeHash);

            internal Identity(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, int gridIndex = 0)
                : base(sql, commandType, connectionString, type, parametersType, s_typeHash, gridIndex)
            {}
            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType, int gridIndex = 0)
                : base(sql, commandType, connection.ConnectionString, type, parametersType, s_typeHash, gridIndex)
            { }

            static int CountNonTrivial(out int hashCode)
            {
                int hashCodeLocal = 0;
                int count = 0;
                bool Map<T>()
                {
                    if(typeof(T) != typeof(DontMap))
                    {
                        count++;
                        hashCodeLocal = (hashCodeLocal * 23) + (typeof(T).GetHashCode());
                        return true;
                    }
                    return false;
                }
                _ = Map<TFirst>() && Map<TSecond>() && Map<TThird>()
                    && Map<TFourth>() && Map<TFifth>() && Map<TSixth>()
                    && Map<TSeventh>();
                hashCode = hashCodeLocal;
                return count;
            }
            internal override int TypeCount => s_typeCount;
            internal override Type GetType(int index)
            {
                switch (index)
                {
                    case 0: return typeof(TFirst);
                    case 1: return typeof(TSecond);
                    case 2: return typeof(TThird);
                    case 3: return typeof(TFourth);
                    case 4: return typeof(TFifth);
                    case 5: return typeof(TSixth);
                    case 6: return typeof(TSeventh);
                    default: return base.GetType(index);
                }
            }
        }
        internal sealed class IdentityWithTypes : Identity
        {
            private readonly Type[] _types;

            internal IdentityWithTypes(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, Type[] otherTypes, int gridIndex = 0)
                : base(sql, commandType, connectionString, type, parametersType, HashTypes(otherTypes), gridIndex)
            {
                _types = otherTypes ?? Type.EmptyTypes;
            }
            internal IdentityWithTypes(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType, Type[] otherTypes, int gridIndex = 0)
                : base(sql, commandType, connection.ConnectionString, type, parametersType, HashTypes(otherTypes), gridIndex)
            {
                _types = otherTypes ?? Type.EmptyTypes;
            }

            internal override int TypeCount => _types.Length;

            internal override Type GetType(int index) => _types[index];

            static int HashTypes(Type[] types)
            {
                var hashCode = 0;
                if (types != null)
                {
                    foreach (var t in types)
                    {
                        hashCode = (hashCode * 23) + (t?.GetHashCode() ?? 0);
                    }
                }
                return hashCode;
            }
        }

        /// <summary>
        /// Identity of a cached query in Dapper, used for extensibility.
        /// </summary>
        public class Identity : IEquatable<Identity>
        {
            internal virtual int TypeCount => 0;

            internal virtual Type GetType(int index) => throw new IndexOutOfRangeException(nameof(index));

            internal Identity ForGrid<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(Type primaryType, int gridIndex) =>
                new Identity<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(sql, commandType, connectionString, primaryType, parametersType, gridIndex);

            internal Identity ForGrid(Type primaryType, int gridIndex) =>
                new Identity(sql, commandType, connectionString, primaryType, parametersType, 0, gridIndex);

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex) =>
                (otherTypes == null || otherTypes.Length == 0)
                ? new Identity(sql, commandType, connectionString, primaryType, parametersType, 0, gridIndex)
                : new IdentityWithTypes(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);

            /// <summary>
            /// Create an identity for use with DynamicParameters, internal use only.
            /// </summary>
            /// <param name="type">The parameters type to create an <see cref="Identity"/> for.</param>
            /// <returns></returns>
            public Identity ForDynamicParameters(Type type) =>
                new Identity(sql, commandType, connectionString, this.type, type, 0, -1);

            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, 0, 0) { /* base call */ }

            private protected Identity(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, int otherTypesHash, int gridIndex)
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
                    hashCode = (hashCode * 23) + commandType.GetHashCode();
                    hashCode = (hashCode * 23) + gridIndex.GetHashCode();
                    hashCode = (hashCode * 23) + (sql?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 23) + (type?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 23) + otherTypesHash;
                    hashCode = (hashCode * 23) + (connectionString == null ? 0 : connectionStringComparer.GetHashCode(connectionString));
                    hashCode = (hashCode * 23) + (parametersType?.GetHashCode() ?? 0);
                }
            }

            /// <summary>
            /// Whether this <see cref="Identity"/> equals another.
            /// </summary>
            /// <param name="obj">The other <see cref="object"/> to compare to.</param>
            public override bool Equals(object obj) => Equals(obj as Identity);

            /// <summary>
            /// The raw SQL command.
            /// </summary>
            public readonly string sql;

            /// <summary>
            /// The SQL command type.
            /// </summary>
            public readonly CommandType? commandType;

            /// <summary>
            /// The hash code of this Identity.
            /// </summary>
            public readonly int hashCode;

            /// <summary>
            /// The grid index (position in the reader) of this Identity.
            /// </summary>
            public readonly int gridIndex;

            /// <summary>
            /// This <see cref="Type"/> of this Identity.
            /// </summary>
            public readonly Type type;

            /// <summary>
            /// The connection string for this Identity.
            /// </summary>
            public readonly string connectionString;

            /// <summary>
            /// The type of the parameters object for this Identity.
            /// </summary>
            public readonly Type parametersType;

            /// <summary>
            /// Gets the hash code for this identity.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode() => hashCode;

            /// <summary>
            /// See object.ToString()
            /// </summary>
            public override string ToString() => sql;

            /// <summary>
            /// Compare 2 Identity objects
            /// </summary>
            /// <param name="other">The other <see cref="Identity"/> object to compare.</param>
            /// <returns>Whether the two are equal</returns>
            public bool Equals(Identity other)
            {
                if (ReferenceEquals(this, other)) return true;
                if (ReferenceEquals(other, null)) return false;

                int typeCount;
                return gridIndex == other.gridIndex
                    && type == other.type
                    && sql == other.sql
                    && commandType == other.commandType
                    && connectionStringComparer.Equals(connectionString, other.connectionString)
                    && parametersType == other.parametersType
                    && (typeCount = TypeCount) == other.TypeCount
                    && (typeCount == 0 || TypesEqual(this, other, typeCount));
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static bool TypesEqual(Identity x, Identity y, int count)
            {
                if (y.TypeCount != count) return false;
                for(int i = 0; i < count; i++)
                {
                    if (x.GetType(i) != y.GetType(i))
                        return false;
                }
                return true;
            }
        }
    }
}
