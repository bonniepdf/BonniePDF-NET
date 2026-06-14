namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

/// <summary>
/// Resolves the TrueType fonts bundled next to this example. The project copies
/// NotoSans (Latin/Greek/symbols) and NotoEmoji (monochrome emoji outlines) -
/// both Google Noto, OFL 1.1 - into a <c>fonts/</c> folder in the build output,
/// so the example embeds real, deterministic TTFs rather than hunting for machine
/// fonts like Segoe UI Emoji. This keeps it cross-platform and lets it run
/// unchanged in CI on Linux.
///
/// NotoEmoji is the single-weight, non-colour variant: its glyphs are plain
/// outlines (no COLR/CBDT tables), so they embed cleanly and satisfy PDF/UA-1.
/// </summary>
internal static class BundledFont
{
    private static string Resolve(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fonts", file);

    public static string SansPath { get; } = Resolve("NotoSans-Regular.ttf");
    public static string EmojiPath { get; } = Resolve("NotoEmoji-Regular.ttf");
}
