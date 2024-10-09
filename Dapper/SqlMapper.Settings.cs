using System;
using System.Data;
using System.Threading;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Permits specifying certain SqlMapper values globally.
        /// </summary>
        public static class Settings
        {
            // disable single row/result by default; prevents errors AFTER the select being detected properly
            private const CommandBehavior DefaultAllowedCommandBehaviors = ~(CommandBehavior.SingleResult | CommandBehavior.SingleRow);
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
                    { // some providers just allow these, so: try again without them and stop issuing them
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
                ApplyNullValues = PadListExpansions = UseIncrementalPseudoPositionalParameterNames = false;
                AllowedCommandBehaviors = DefaultAllowedCommandBehaviors;
                FetchSize = InListStringSplitCount = -1;
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
            /// operation if there are this many elements or more. Note that this feature requires SQL Server 2016 / compatibility level 130 (or above).
            /// </summary>
            public static int InListStringSplitCount { get; set; } = -1;

            /// <summary>
            /// If set, pseudo-positional parameters (i.e. ?foo?) are passed using auto-generated incremental names, i.e. "1", "2", "3"
            /// instead of the original name; for most scenarios, this is ignored since the name is redundant, but "snowflake" requires this.
            /// </summary>
            public static bool UseIncrementalPseudoPositionalParameterNames { get; set; }

            /// <summary>
            /// If assigned a non-negative value, then that value is applied to any commands <c>FetchSize</c> property, if it exists;
            /// see https://docs.oracle.com/en/database/oracle/oracle-database/18/odpnt/CommandFetchSize.html; note that this value
            /// can only be set globally - it is not intended for frequent/contextual changing.
            /// </summary>
            public static long FetchSize
            {
                get => Volatile.Read(ref s_FetchSize);
                set
                {
                    if (Volatile.Read(ref s_FetchSize) != value)
                    {
                        Volatile.Write(ref s_FetchSize, value);
                        CommandDefinition.ResetCommandInitCache(); // if this setting is useful: we've invalidated things
                    }
                }
            }

            /// <summary>
            /// Indicates whether single-character parameter tokens (<c>?</c> etc) will be detected and used where possible;
            /// this feature is not recommended and will be disabled by default in future versions;
            /// where possible, prefer named parameters (<c>@yourParam</c> etc) or Dapper's "pseudo-positional" parameters (<c>?yourParam? etc</c>).
            /// </summary>
            public static bool SupportLegacyParameterTokens { get; set; } = true;

            private static long s_FetchSize = -1;
        }
    }
}
