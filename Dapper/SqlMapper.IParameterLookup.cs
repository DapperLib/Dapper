namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Extends IDynamicParameters providing by-name lookup of parameter values
        /// </summary>
        public interface IParameterLookup : IDynamicParameters
        {
            /// <summary>
            /// Get the value of the specified parameter (return null if not found)
            /// </summary>
            object this[string name] { get; }
        }
    }
}
