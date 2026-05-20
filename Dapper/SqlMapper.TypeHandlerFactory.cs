using System;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Creates <see cref="ITypeHandler"/> instances on demand for types it claims via
        /// <see cref="CanHandle"/>.
        /// </summary>
        /// <remarks>
        /// Register a factory with <see cref="AddTypeHandlerFactory"/>. When Dapper needs a handler for a
        /// type and no direct handler has been registered, it queries each factory in registration order.
        /// The first factory whose <see cref="CanHandle"/> returns <see langword="true"/> is asked to
        /// <see cref="Create"/> the handler. The created handler is then cached in
        /// <see cref="typeHandlers"/> (and in <see cref="TypeHandlerCache{T}"/> for IL-emitted paths),
        /// so the factory is only consulted once per type.
        /// </remarks>
        public abstract class TypeHandlerFactory
        {
            /// <summary>
            /// Returns <see langword="true"/> if this factory can provide a handler for
            /// <paramref name="type"/>.
            /// </summary>
            public abstract bool CanHandle(Type type);

            /// <summary>
            /// Creates an <see cref="ITypeHandler"/> for <paramref name="type"/>.
            /// Only called after <see cref="CanHandle"/> returned <see langword="true"/> for the same type.
            /// </summary>
            public abstract ITypeHandler Create(Type type);
        }
    }
}
