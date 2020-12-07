namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Dummy type for excluding from multi-map
        /// </summary>
        private sealed class DontMap { /* hiding constructor */ }
    }
}
