using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF; // AppendLf - platform-independent '\n' (see StringBuilderExtensions)

namespace BonniePDF.Typography.TrueType;

/// <summary>
/// Generates a ToUnicode CMap for text extraction support in PDFs.
/// Maps glyph IDs to Unicode code points, enabling copy/paste functionality.
/// Supports the full Unicode range including supplementary planes (emoji, CJK Extension B+).
/// </summary>
internal class ToUnicodeCMapBuilder
{
    private readonly List<(ushort glyphId, string unicode)> _mappings = new();

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

        AddSourceMapping(glyphId,char.ConvertFromUtf32(unicode));
    }

    internal void AddSourceMapping(ushort glyphId,string unicode)
    {
        if(glyphId!=0 && unicode.Length>0) _mappings.Add((glyphId,unicode));
    }

    /// <summary>
    /// Adds multiple mappings from a codepoint-to-glyph dictionary
    /// </summary>
    public void AddMappings(IDictionary<int, ushort> codepointToGlyph)
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
        AppendHeader(sb);

        // Sort mappings by glyph ID for consistent output
        List<(ushort glyphId, string unicode)> sorted = _mappings
            .OrderBy(m => m.glyphId)
            .DistinctBy(m=>m.glyphId)
            .ToList();

        // Write bfchar entries in chunks of 100 (PDF limit per block)
        int count = sorted.Count;
        for (int i = 0; i < count; i += 100)
        {
            int chunkSize = Math.Min(100, count - i);
            sb.AppendLf($"{chunkSize} beginbfchar");

            for (int j = 0; j < chunkSize; j++)
            {
                (ushort gid, string unicode) = sorted[i + j];
                sb.AppendLf($"<{gid:X4}> {FormatUnicode(unicode)}");
            }

            sb.AppendLf("endbfchar");
        }

        // CMap footer
        sb.AppendLf("endcmap");
        sb.AppendLf("CMapName currentdict /CMap defineresource pop");
        sb.AppendLf("end");
        sb.AppendLf("end");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a Unicode codepoint for the ToUnicode CMap.
    /// BMP codepoints use 4-digit hex; supplementary codepoints use UTF-16 surrogate pairs.
    /// </summary>
    private static string FormatUnicode(string text)
    {
        var hex=new StringBuilder("<"); foreach(char c in text) hex.Append(((int)c).ToString("X4",System.Globalization.CultureInfo.InvariantCulture)); return hex.Append('>').ToString();
    }

    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLf("/CIDInit /ProcSet findresource begin");
        sb.AppendLf("12 dict begin");
        sb.AppendLf("begincmap");
        sb.AppendLf("/CIDSystemInfo <<");
        sb.AppendLf("  /Registry (Adobe)");
        sb.AppendLf("  /Ordering (UCS)");
        sb.AppendLf("  /Supplement 0");
        sb.AppendLf(">> def");
        sb.AppendLf("/CMapName /Adobe-Identity-UCS def");
        sb.AppendLf("/CMapType 2 def");
        sb.AppendLf("1 begincodespacerange");
        sb.AppendLf("<0000> <FFFF>");
        sb.AppendLf("endcodespacerange");
    }

    private static string BuildEmptyCMap()
    {
        StringBuilder sb = new();

        AppendHeader(sb);
        sb.AppendLf("endcmap");
        sb.AppendLf("CMapName currentdict /CMap defineresource pop");
        sb.AppendLf("end");
        sb.AppendLf("end");

        return sb.ToString();
    }
}
