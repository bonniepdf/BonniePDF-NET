using BonniePDF.Typography.Engine;

namespace BonniePDF.Tests.Support;

/// <summary>Reads a packed glyph run as records, so assertions can compare whole glyphs.</summary>
internal static class GlyphRecords
{
    internal static GlyphInfo[] Records(this GlyphRun run)
    {
        var records = new GlyphInfo[run.Count];
        for (int i = 0; i < records.Length; i++) records[i] = run.Info(i);
        return records;
    }
}
