namespace BonniePDF.Examples.Typography.Alignment;

/// <summary>
/// Resolves the Liberation Sans TrueType fonts bundled next to this example. The
/// project copies the fonts into a <c>fonts/</c> folder in the build output so
/// the example embeds real TTF files from disk on any platform.
/// </summary>
internal static class BundledFont
{
    private static string Resolve(string file) =>
        Path.Combine(AppContext.BaseDirectory, "fonts", file);

    public static string SansPath { get; } = Resolve("LiberationSans-Regular.ttf");
    public static string SansBoldPath { get; } = Resolve("LiberationSans-Bold.ttf");
}
