namespace BonniePDF.Examples.Typography.CustomFonts;

/// <summary>
/// Resolves the TrueType font bundled next to this example. The project copies
/// Liberation Sans (metric-compatible with Arial) into a <c>fonts/</c> folder in
/// the build output, so the example embeds a real, deterministic TTF rather than
/// hunting for a machine font like Arial - which keeps it cross-platform and lets
/// it run unchanged in CI on Linux.
/// </summary>
internal static class BundledFont
{
    public static string SansPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "fonts", "LiberationSans-Regular.ttf");
}
