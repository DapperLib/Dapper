namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Extends IDynamicParameters providing by-name lookup of parameter values
        /// </summary>
        public interface IParameterLookup : IDynamicParameters
        {
            /// <summary>
            /// Get the value of the specified parameter (return null if not found)
            /// </summary>
            /// <param name="name">The name of the parameter to get.</param>
            object this[string name] { get; }
        }
    }
}
