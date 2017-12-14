using System;
using System.Data;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Permits specifying certain SqlMapper values globally.
        /// </summary>
        public static class Settings
        {
            // disable single result by default; prevents errors AFTER the select being detected properly
            private const CommandBehavior DefaultAllowedCommandBehaviors = ~CommandBehavior.SingleResult;
            internal static CommandBehavior AllowedCommandBehaviors { get; private set; } = DefaultAllowedCommandBehaviors;
            private static void SetAllowedCommandBehaviors(CommandBehavior behavior, bool enabled)
            {
                if (enabled) AllowedCommandBehaviors |= behavior;
                else AllowedCommandBehaviors &= ~behavior;
            }
            /// <summary>
            /// Gets or sets whether Dapper should use the CommandBehavior.SingleResult optimization
            /// </summary>
            /// <remarks>Note that a consequence of enabling this option is that errors that happen <b>after</b> the first select may not be reported</remarks>
            public static bool UseSingleResultOptimization
            {
                get { return (AllowedCommandBehaviors & CommandBehavior.SingleResult) != 0; }
                set { SetAllowedCommandBehaviors(CommandBehavior.SingleResult, value); }
            }
            /// <summary>
            /// Gets or sets whether Dapper should use the CommandBehavior.SingleRow optimization
            /// </summary>
            /// <remarks>Note that on some DB providers this optimization can have adverse performance impact</remarks>
            public static bool UseSingleRowOptimization
            {
                get { return (AllowedCommandBehaviors & CommandBehavior.SingleRow) != 0; }
                set { SetAllowedCommandBehaviors(CommandBehavior.SingleRow, value); }
            }

            internal static bool DisableCommandBehaviorOptimizations(CommandBehavior behavior, Exception ex)
            {
                if (AllowedCommandBehaviors == DefaultAllowedCommandBehaviors
                    && (behavior & (CommandBehavior.SingleResult | CommandBehavior.SingleRow)) != 0)
                {
                    if (ex.Message.Contains(nameof(CommandBehavior.SingleResult))
                        || ex.Message.Contains(nameof(CommandBehavior.SingleRow)))
                    { // some providers just just allow these, so: try again without them and stop issuing them
                        SetAllowedCommandBehaviors(CommandBehavior.SingleResult | CommandBehavior.SingleRow, false);
                        return true;
                    }
                }
                return false;
            }

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
            /// Should list expansions be padded with null-valued parameters, to prevent query-plan saturation? For example,
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
            /// <summary>
            /// If set (non-negative), when performing in-list expansions of integer types ("where id in @ids", etc), switch to a string_split based
            /// operation if there are more than this many elements. Note that this feautre requires SQL Server 2016 / compatibility level 130 (or above).
            /// </summary>
            public static int InListStringSplitCount { get; set; } = -1;
        }
    }
}
