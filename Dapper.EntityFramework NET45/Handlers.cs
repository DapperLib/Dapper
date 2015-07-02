namespace Dapper.EntityFramework
{
    /// <summary>
    /// Acts on behalf of all type-handlers in this package
    /// </summary>
    public static class Handlers
    {
        /// <summary>
        /// Register all type-handlers in this package
        /// </summary>
        public static void Register()
        {
            SqlMapper.AddTypeHandler(DbGeographyHandler.Default);
            SqlMapper.AddTypeHandler(DbGeometryHandler.Default);
        }
    }
}
