using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BonniePDF.Typography;

/// <summary>
/// Generates a ToUnicode CMap for text extraction support in PDFs.
/// Maps glyph IDs to Unicode code points, enabling copy/paste functionality.
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
    public void AddMapping(ushort glyphId, char unicode)
    {
        // Skip .notdef (GID 0) as it doesn't map to a real character
        if (glyphId == 0) return;

        _mappings.Add((glyphId, unicode));
    }

    /// <summary>
    /// Adds multiple mappings from a character-to-glyph dictionary
    /// </summary>
    public void AddMappings(Dictionary<char, ushort> charToGlyph)
    {
        foreach ((char c, ushort gid) in charToGlyph) AddMapping(gid, c);
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
                sb.AppendLine($"<{gid:X4}> <{unicode:X4}>");
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