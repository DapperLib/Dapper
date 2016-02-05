namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Permits specifying certain SqlMapper values globally.
        /// </summary>
        public static class Settings
        {
            static Settings()
            {
                SetDefaults();
            }

            /// <summary>
            /// Resets all Settings to their default values
            /// </summary>
            public static void SetDefaults()
            {
                CommandTimeout = null;
                ApplyNullValues = false;
            }

            /// <summary>
            /// Specifies the default Command Timeout for all Queries
            /// </summary>
            public static int? CommandTimeout { get; set; }

            /// <summary>
            /// Indicates whether nulls in data are silently ignored (default) vs actively applied and assigned to members
            /// </summary>
            public static bool ApplyNullValues { get; set; }
        }
    }
}
