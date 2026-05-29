namespace BonniePDF.Configuration;

internal class InternalConfiguration
{
    internal static bool EnableCompression { get; set; } = true;

    /// <summary>
    /// When <c>false</c>, the resource registry skips the OS-level
    /// system-font fallback (<c>ResolveStandardFontToTtf</c>) so tests
    /// can reliably simulate an environment where no TTF is available
    /// for a given standard font name. Production code always leaves
    /// this <c>true</c>.
    /// </summary>
    internal static bool AllowSystemFontFallback { get; set; } = true;
}