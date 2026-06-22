using System;
using System.IO;
using System.Reflection;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Tests;

/// <summary>
/// Provides a real TrueType font file for tests in a cross-platform way.
///
/// The font is materialized from the bundled Liberation family (embedded in the
/// BonniePDF assembly) to a temp file once per test run. This replaces the old
/// hard-coded <c>C:\Windows\Fonts\arial.ttf</c> path, which only existed on
/// Windows and made the TTF tests fail everywhere else. Liberation Sans is
/// metric-compatible with Arial, so the assertions the Arial-based tests made
/// (proportional widths, non-symbolic flag, ~0 italic angle, …) still hold.
/// </summary>
internal static class TestFonts
{
    private static readonly Lazy<string> SansRegular = new(() => Extract("LiberationSans-Regular.ttf"));

    /// <summary>Path to a bundled Liberation Sans Regular TTF on disk.</summary>
    public static string SansRegularPath => SansRegular.Value;

    private static string Extract(string fileSuffix)
    {
        Assembly asm = typeof(TtfFont).Assembly;
        string resourceName = Array.Find(
                                  asm.GetManifestResourceNames(),
                                  n => n.EndsWith(fileSuffix, StringComparison.OrdinalIgnoreCase))
                              ?? throw new InvalidOperationException(
                                  $"Embedded font resource ending in '{fileSuffix}' was not found in {asm.GetName().Name}.");

        string path = Path.Combine(Path.GetTempPath(), $"bonniepdf-test-{fileSuffix}");
        using Stream stream = asm.GetManifestResourceStream(resourceName)!;
        using FileStream fs = File.Create(path);
        stream.CopyTo(fs);
        return path;
    }
}
