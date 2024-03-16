using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Dapper;

internal static partial class CompiledRegex
{
#if DEBUG && NET7_0_OR_GREATER // enables colorization in IDE
    [StringSyntax("Regex")]
#endif
    private const string
        WhitespaceOrReservedPattern = @"[\s;/\-+*]|^vacuum$|^commit$|^rollback$|^revert$",
        LegacyParameterPattern = @"(?<![\p{L}\p{N}@_])[?@:](?![\p{L}\p{N}@_])", // look for ? / @ / : *by itself* - see SupportLegacyParameterTokens
        LiteralTokensPattern = @"(?<![\p{L}\p{N}_])\{=([\p{L}\p{N}_]+)\}", // look for {=abc} to inject member abc as a literal
        PseudoPositionalPattern = @"\?([\p{L}_][\p{L}\p{N}_]*)\?"; // look for ?abc? for the purpose of subst back to ? using member abc


#if NET7_0_OR_GREATER // use regex code generator (this doesn't work for down-level, even if you define the attribute manually)
    [GeneratedRegex(LegacyParameterPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex LegacyParameterGen();

    [GeneratedRegex(LiteralTokensPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex LiteralTokensGen();

    [GeneratedRegex(PseudoPositionalPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex PseudoPositionalGen();

    [GeneratedRegex(WhitespaceOrReservedPattern, RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex WhitespaceOrReservedGen();

    internal static Regex LegacyParameter => LegacyParameterGen();
    internal static Regex LiteralTokens => LiteralTokensGen();
    internal static Regex PseudoPositional => PseudoPositionalGen();
    internal static Regex WhitespaceOrReserved => WhitespaceOrReservedGen();
#else
    internal static Regex LegacyParameter { get; }
        = new(LegacyParameterPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    internal static Regex LiteralTokens { get; }
        = new(LiteralTokensPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    internal static Regex PseudoPositional { get; }
    = new(PseudoPositionalPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    internal static Regex WhitespaceOrReserved { get; }
        = new(WhitespaceOrReservedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
#endif
}
