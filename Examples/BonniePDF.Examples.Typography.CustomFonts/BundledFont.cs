namespace BonniePDF.Examples.Typography.CustomFonts;

/// <summary>
/// Resolves the TrueType fonts bundled next to this example. The project copies
/// a small set of Google Noto fonts (OFL 1.1) into a <c>fonts/</c> folder in the
/// build output, so the example embeds real, deterministic TTFs with broad
/// Unicode coverage rather than hunting for machine fonts like Arial - which
/// keeps it cross-platform and lets it run unchanged in CI on Linux.
///
/// The font split mirrors the TypeScript sibling example:
///   • NotoSans          - Latin, Greek, currency, Latin-1 symbols
///   • NotoSansMath      - arrows (U+2190..) and mathematical operators (U+2200..)
///   • NotoSansSymbols2  - dingbats and miscellaneous symbols (U+2600..U+27FF)
///   • NotoEmoji         - supplementary-plane emoji (U+1F000+), monochrome outlines
/// </summary>
internal static class BundledFont
{
    private static string Resolve(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fonts", file);

    public static string SansPath { get; } = Resolve("NotoSans-Regular.ttf");
    public static string MathPath { get; } = Resolve("NotoSansMath-Regular.ttf");
    public static string SymbolsPath { get; } = Resolve("NotoSansSymbols2-Regular.ttf");
    public static string EmojiPath { get; } = Resolve("NotoEmoji-Regular.ttf");
}
