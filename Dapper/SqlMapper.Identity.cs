﻿using System;
using System.ComponentModel;
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

            internal Identity(string sql, CommandType? commandType, string connectionString, Type type, Type? parametersType, int gridIndex = 0)
                : base(sql, commandType, connectionString, type, parametersType, s_typeHash, gridIndex)
            {}
            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type? parametersType, int gridIndex = 0)
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
            internal override Type GetType(int index) => index switch {
                0 => typeof(TFirst),
                1 => typeof(TSecond),
                2 => typeof(TThird),
                3 => typeof(TFourth),
                4 => typeof(TFifth),
                5 => typeof(TSixth),
                6 => typeof(TSeventh),
                _ => base.GetType(index),
            };
        }
        internal sealed class IdentityWithTypes : Identity
        {
            private readonly Type[] _types;

            internal IdentityWithTypes(string sql, CommandType? commandType, string connectionString, Type type, Type? parametersType, Type[] otherTypes, int gridIndex = 0)
                : base(sql, commandType, connectionString, type, parametersType, HashTypes(otherTypes), gridIndex)
            {
                _types = otherTypes ?? Type.EmptyTypes;
            }
            internal IdentityWithTypes(string sql, CommandType? commandType, IDbConnection connection, Type type, Type? parametersType, Type[] otherTypes, int gridIndex = 0)
                : base(sql, commandType, connection.ConnectionString, type, parametersType, HashTypes(otherTypes), gridIndex)
            {
                _types = otherTypes ?? Type.EmptyTypes;
            }

            internal override int TypeCount => _types.Length;

            internal override Type GetType(int index) => _types[index];

            static int HashTypes(Type[] types)
            {
                var hashCode = 0;
                if (types is not null)
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

#pragma warning disable CS0618 // Type or member is obsolete
            internal Identity ForGrid<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(Type primaryType, int gridIndex) =>
                new Identity<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(sql, commandType, connectionString, primaryType, parametersType, gridIndex);

            internal Identity ForGrid(Type primaryType, int gridIndex) =>
                new Identity(sql, commandType, connectionString, primaryType, parametersType, 0, gridIndex);

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex) =>
                (otherTypes is null || otherTypes.Length == 0)
                ? new Identity(sql, commandType, connectionString, primaryType, parametersType, 0, gridIndex)
                : new IdentityWithTypes(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);

            /// <summary>
            /// Create an identity for use with DynamicParameters, internal use only.
            /// </summary>
            /// <param name="type">The parameters type to create an <see cref="Identity"/> for.</param>
            /// <returns></returns>
            public Identity ForDynamicParameters(Type type) =>
                new Identity(sql, commandType, connectionString, this.type, type, 0, -1);
#pragma warning restore CS0618 // Type or member is obsolete

            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type? type, Type? parametersType)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, 0, 0) { /* base call */ }

            private protected Identity(string sql, CommandType? commandType, string connectionString, Type? type, Type? parametersType, int otherTypesHash, int gridIndex)
            {
#pragma warning disable CS0618 // Type or member is obsolete
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
                    hashCode = (hashCode * 23) + (connectionString is null ? 0 : connectionStringComparer.GetHashCode(connectionString));
                    hashCode = (hashCode * 23) + (parametersType?.GetHashCode() ?? 0);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            /// <summary>
            /// Whether this <see cref="Identity"/> equals another.
            /// </summary>
            /// <param name="obj">The other <see cref="object"/> to compare to.</param>
            public override bool Equals(object? obj) => Equals(obj as Identity);

            /// <summary>
            /// The raw SQL command.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(Sql) + ". This API may be removed at a later date.")]
            public readonly string sql;

            /// <summary>
            /// The raw SQL command.
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public string Sql => sql;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// The SQL command type.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(CommandType) + ". This API may be removed at a later date.")]
            public readonly CommandType? commandType;

            /// <summary>
            /// The SQL command type.
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public CommandType? CommandType => commandType;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// The hash code of this Identity.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(GetHashCode) + ". This API may be removed at a later date.")]
            public readonly int hashCode;

            /// <summary>
            /// The grid index (position in the reader) of this Identity.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(GridIndex) + ". This API may be removed at a later date.")]
            public readonly int gridIndex;

            /// <summary>
            /// The grid index (position in the reader) of this Identity.
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public int GridIndex => gridIndex;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// The <see cref="Type"/> of this Identity.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(Type) + ". This API may be removed at a later date.")]
            public readonly Type? type;

            /// <summary>
            /// The <see cref="Type"/> of this Identity.
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public Type? Type => type;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// The connection string for this Identity.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("This API may be removed at a later date.")]
            public readonly string connectionString;

            /// <summary>
            /// The type of the parameters object for this Identity.
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(ParametersType) + ". This API may be removed at a later date.")]
            public readonly Type? parametersType;

            /// <summary>
            /// The type of the parameters object for this Identity.
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public Type? ParametersType => parametersType;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// Gets the hash code for this identity.
            /// </summary>
            /// <returns></returns>
#pragma warning disable CS0618 // Type or member is obsolete
            public override int GetHashCode() => hashCode;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// See object.ToString()
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public override string ToString() => sql;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// Compare 2 Identity objects
            /// </summary>
            /// <param name="other">The other <see cref="Identity"/> object to compare.</param>
            /// <returns>Whether the two are equal</returns>
            public bool Equals(Identity? other)
            {
                if (ReferenceEquals(this, other)) return true;
                if (other is null) return false;

                int typeCount;
#pragma warning disable CS0618 // Type or member is obsolete
                return gridIndex == other.gridIndex
                    && type == other.type
                    && sql == other.sql
                    && commandType == other.commandType
                    && connectionStringComparer.Equals(connectionString, other.connectionString)
                    && parametersType == other.parametersType
                    && (typeCount = TypeCount) == other.TypeCount
                    && (typeCount == 0 || TypesEqual(this, other, typeCount));
#pragma warning restore CS0618 // Type or member is obsolete
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
