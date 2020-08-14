using System;
using System.Data;

namespace Dapper
{
    /// <summary>
    /// FeatureSupport Wrapper 
    /// </summary>
    public static class FeatureSupportWrapper
    {
        /// <summary>
        /// The function to get a database connection type from the given <see cref="IDbConnection"/>.
        /// </summary>
        /// <param name="connection">The connection to get a database type name from.</param>
        public delegate Type GetDatabaseConnectionTypeDelegate(IDbConnection connection);

        /// <summary>
        /// Specifies a custom callback that detects the database type instead of relying on the default strategy (the name of the connection type object).
        /// Please note that this callback is global and will be used by all the calls that require a database specific adapter.
        /// </summary>
         public static GetDatabaseConnectionTypeDelegate GetDbConnectionType;
    }
}
