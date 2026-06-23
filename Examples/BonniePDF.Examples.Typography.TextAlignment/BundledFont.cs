namespace BonniePDF.Examples.Typography.TextAlignment;

/// <summary>
/// Resolves the NotoSans TrueType font bundled next to this example. The project
/// copies the font into a <c>fonts/</c> folder in the build output so the
/// Liberation-style example embeds a real TTF from disk on any platform.
/// </summary>
internal static class BundledFont
{
    private static string Resolve(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fonts", file);

    public static string SansPath { get; } = Resolve("NotoSans-Regular.ttf");
}
