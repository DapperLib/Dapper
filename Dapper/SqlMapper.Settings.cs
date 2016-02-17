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


            /// <summary>
            /// Should list expansions be padded with null-valued parrameters, to prevent qurey-plan saturation? For example,
            /// an 'in @foo' expansion with 7, 8 or 9 values will be sent as a list of 10 values, with 3, 2 or 1 of them null.
            /// The padding size is relative to the size of the list; "next 10" under 150, "next 50" under 500,
            /// "next 100" under 1500, etc.
            /// </summary>
            /// <remarks>
            /// Caution: this should be treated with care if your DB provider (or the specific configuration) allows for null
            /// equality (aka "ansi nulls off"), as this may change the intent of your query; as such, this is disabled by 
            /// default and must be enabled.
            /// </remarks>
            public static bool PadListExpansions { get; set; }
        }
    }
}
