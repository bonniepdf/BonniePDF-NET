using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BonniePDF.Typography;

/// <summary>
/// Generates a ToUnicode CMap for text extraction support in PDFs.
/// Maps glyph IDs to Unicode code points, enabling copy/paste functionality.
/// Supports the full Unicode range including supplementary planes (emoji, CJK Extension B+).
/// </summary>
public class ToUnicodeCMapBuilder
{
    private readonly List<(ushort glyphId, int unicode)> _mappings = new();

    /// <summary>
    /// Gets the number of mappings added
    /// </summary>
    public int MappingCount => _mappings.Count;

    /// <summary>
    /// Adds a single glyph ID to Unicode mapping
    /// </summary>
    public void AddMapping(ushort glyphId, int unicode)
    {
        // Skip .notdef (GID 0) as it doesn't map to a real character
        if (glyphId == 0) return;

        _mappings.Add((glyphId, unicode));
    }

    /// <summary>
    /// Adds multiple mappings from a codepoint-to-glyph dictionary
    /// </summary>
    public void AddMappings(Dictionary<int, ushort> codepointToGlyph)
    {
        foreach ((int cp, ushort gid) in codepointToGlyph) AddMapping(gid, cp);
    }

    /// <summary>
    /// Generates the CMap program as a string
    /// </summary>
    public string Build()
    {
        if (_mappings.Count == 0)
            return BuildEmptyCMap();

        StringBuilder sb = new();

        // CMap header
        sb.AppendLine("/CIDInit /ProcSet findresource begin");
        sb.AppendLine("12 dict begin");
        sb.AppendLine("begincmap");
        sb.AppendLine("/CIDSystemInfo <<");
        sb.AppendLine("  /Registry (Adobe)");
        sb.AppendLine("  /Ordering (UCS)");
        sb.AppendLine("  /Supplement 0");
        sb.AppendLine(">> def");
        sb.AppendLine("/CMapName /Adobe-Identity-UCS def");
        sb.AppendLine("/CMapType 2 def");
        sb.AppendLine("1 begincodespacerange");
        sb.AppendLine("<0000> <FFFF>");
        sb.AppendLine("endcodespacerange");

        // Sort mappings by glyph ID for consistent output
        List<(ushort glyphId, int unicode)> sorted = _mappings
            .OrderBy(m => m.glyphId)
            .Distinct()
            .ToList();

        // Write bfchar entries in chunks of 100 (PDF limit per block)
        int count = sorted.Count;
        for (int i = 0; i < count; i += 100)
        {
            int chunkSize = Math.Min(100, count - i);
            sb.AppendLine($"{chunkSize} beginbfchar");

            for (int j = 0; j < chunkSize; j++)
            {
                (ushort gid, int unicode) = sorted[i + j];
                sb.AppendLine($"<{gid:X4}> {FormatUnicode(unicode)}");
            }

            sb.AppendLine("endbfchar");
        }

        // CMap footer
        sb.AppendLine("endcmap");
        sb.AppendLine("CMapName currentdict /CMap defineresource pop");
        sb.AppendLine("end");
        sb.AppendLine("end");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a Unicode codepoint for the ToUnicode CMap.
    /// BMP codepoints use 4-digit hex; supplementary codepoints use UTF-16 surrogate pairs.
    /// </summary>
    private static string FormatUnicode(int codepoint)
    {
        if (codepoint <= 0xFFFF)
            return $"<{codepoint:X4}>";

        // Supplementary plane: encode as UTF-16 surrogate pair
        int offset = codepoint - 0x10000;
        int highSurrogate = 0xD800 + (offset >> 10);
        int lowSurrogate = 0xDC00 + (offset & 0x3FF);
        return $"<{highSurrogate:X4}{lowSurrogate:X4}>";
    }

    private string BuildEmptyCMap()
    {
        StringBuilder sb = new();

        sb.AppendLine("/CIDInit /ProcSet findresource begin");
        sb.AppendLine("12 dict begin");
        sb.AppendLine("begincmap");
        sb.AppendLine("/CIDSystemInfo <<");
        sb.AppendLine("  /Registry (Adobe)");
        sb.AppendLine("  /Ordering (UCS)");
        sb.AppendLine("  /Supplement 0");
        sb.AppendLine(">> def");
        sb.AppendLine("/CMapName /Adobe-Identity-UCS def");
        sb.AppendLine("/CMapType 2 def");
        sb.AppendLine("1 begincodespacerange");
        sb.AppendLine("<0000> <FFFF>");
        sb.AppendLine("endcodespacerange");
        sb.AppendLine("endcmap");
        sb.AppendLine("CMapName currentdict /CMap defineresource pop");
        sb.AppendLine("end");
        sb.AppendLine("end");

        return sb.ToString();
    }
}
