using System;
using System.Data;

#if DOTNET5_2
using IDbConnection = System.Data.Common.DbConnection;
#endif
namespace Dapper
{
    /// <summary>
    /// Handles variances in features per DBMS
    /// </summary>
    class FeatureSupport
    {
        private static readonly FeatureSupport
            @default = new FeatureSupport(false),
            postgres = new FeatureSupport(true);

        /// <summary>
        /// Gets the feature set based on the passed connection
        /// </summary>
        public static FeatureSupport Get(IDbConnection connection)
        {
            string name = connection?.GetType().Name;
            if (string.Equals(name, "npgsqlconnection", StringComparison.OrdinalIgnoreCase)) return postgres;
            return @default;
        }
        private FeatureSupport(bool arrays)
        {
            Arrays = arrays;
        }
        /// <summary>
        /// True if the db supports array columns e.g. Postgresql
        /// </summary>
        public bool Arrays { get; }
    }

}
