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
                IgnoreDuplicatedColumns = false;
            }

            /// <summary>
            /// Specifies the default Command Timeout for all Queries
            /// </summary>
            public static int? CommandTimeout { get; set; }

            private static bool ignoreDuplicatedColumns;
            /// <summary>
            /// If a column name is duplicated, the duplicates are skipped (by default, all are processed, so the last column wins)
            /// </summary>
            /// <remarks>This setting should be set once at the start of the application; it is not intended to be toggled per-query</remarks>
            public static bool IgnoreDuplicatedColumns {
                get { return ignoreDuplicatedColumns; }
                set
                {
                    if (value != ignoreDuplicatedColumns)
                    {
                        ignoreDuplicatedColumns = value;
                        PurgeQueryCache();
                    }
                }
            }
        }
    }
}
