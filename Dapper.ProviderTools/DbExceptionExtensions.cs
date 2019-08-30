using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
#nullable enable
namespace Dapper.ProviderTools
{
    /// <summary>
    /// Helper utilties for working with database exceptions
    /// </summary>
    public static class DbExceptionExtensions
    {
        /// <summary>
        /// Indicates whether the provided exception has an integer Number property with the supplied value
        /// </summary>
        public static bool IsNumber(this DbException exception, int number)
            => exception != null && ByTypeHelpers.Get(exception.GetType()).IsNumber(exception, number);

        
        private sealed class ByTypeHelpers
        {
            private static readonly ConcurrentDictionary<Type, ByTypeHelpers> s_byType
                = new ConcurrentDictionary<Type, ByTypeHelpers>();
            private readonly Func<DbException, int>? _getNumber;

            public bool IsNumber(DbException exception, int number)
                => _getNumber != null && _getNumber(exception) == number;

            public static ByTypeHelpers Get(Type type)
            {
                if (!s_byType.TryGetValue(type, out var value))
                {
                    s_byType[type] = value = new ByTypeHelpers(type);
                }
                return value;
            }

            private ByTypeHelpers(Type type)
            {
                _getNumber = TryGetInstanceProperty<int>("Number", type);
            }

            private static Func<DbException, T>? TryGetInstanceProperty<T>(string name, Type type)
            {
                try
                {
                    var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null || !prop.CanRead) return null;
                    if (prop.PropertyType != typeof(T)) return null;

                    var p = Expression.Parameter(typeof(DbException), "exception");
                    var body = Expression.Property(Expression.Convert(p, type), prop);
                    var lambda = Expression.Lambda<Func<DbException, T>>(body, p);
                    return lambda.Compile();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
