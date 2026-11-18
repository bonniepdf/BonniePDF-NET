namespace BonniePDF.Tests;

/// <summary>
/// The match timeout every test regular expression uses.
/// </summary>
/// <remarks>
/// Patterns run over generated PDF and SVG text, so a pathological backtrack would hang a test
/// run rather than fail it. The limit is generous so that a slow CI machine never trips it on a
/// healthy match.
/// </remarks>
internal static class RegexTimeout
{
    /// <summary>The match timeout, in milliseconds.</summary>
    internal const int Milliseconds = 10_000;
}
