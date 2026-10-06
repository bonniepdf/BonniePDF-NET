using System;
using System.Collections.Generic;
using System.IO;
using BonniePDF.Rendering.Svg.Fonts;

namespace BonniePDF.Drawing;

/// <summary>
/// Supplies the SVG importer with the fonts registered with BonniePDF.
/// </summary>
/// <remarks>
/// A family name resolves as it would for a paragraph - registered custom families,
/// the standard families and their aliases (<c>Arial</c> and <c>sans-serif</c> to
/// Helvetica, and so on), with the Liberation faces behind the standard families
/// supplying the outlines - and then as the name of a font registered with
/// <see cref="FontManager"/>. A name that resolves to nothing returns null, so the
/// importer moves on to the next family in the list: unlike a paragraph, imported SVG
/// never falls back to a font nobody chose.
/// </remarks>
internal sealed class BonniePdfFontProvider : IFontProvider
{
    /// <summary>Registered subfamilies to try for a style, best match first.</summary>
    private static readonly Dictionary<string, string[]> Subfamilies = new(StringComparer.Ordinal)
    {
        ["regular"] = ["regular"],
        ["bold"] = ["bold", "regular"],
        ["italic"] = ["italic", "regular"],
        ["bolditalic"] = ["bolditalic", "bold", "italic", "regular"],
    };

    private readonly Dictionary<byte[], IFontFace?> _faces = new(ReferenceEqualityComparer.Instance);

    public IFontFace? Resolve(string family, int weight, bool italic)
    {
        byte[]? bytes = BytesFor(family, weight, italic);
        if (bytes is null) return null;
        if (!_faces.TryGetValue(bytes, out IFontFace? face))
        {
            face = Parse(bytes, family);
            _faces[bytes] = face;
        }

        return face;
    }

    private static byte[]? BytesFor(string family, int weight, bool italic)
    {
        string? resolved = FontFamilyResolver.ResolveKnown(family, (FontWeight)weight, italic);
        if (resolved is not null)
        {
            return FontMetrics.GetDocumentFontBytes(resolved) ?? FontManager.GetBytesForStandardFont(resolved) ?? ReadFile(resolved);
        }

        bool bold = weight >= 600;
        string style = bold && italic ? "bolditalic" : bold ? "bold" : italic ? "italic" : "regular";
        foreach (string subfamily in Subfamilies[style])
        {
            byte[]? registered = FontManager.GetBytesForFamily(family.Trim(), subfamily);
            if (registered is not null) return registered;
        }

        return null;
    }

    private static IFontFace? Parse(byte[] bytes, string family)
    {
        try
        {
            return new TrueTypeFace(bytes, family);
        }
#pragma warning disable CA1031 // A font the importer cannot outline is skipped like an absent one.
        catch (Exception)
#pragma warning restore CA1031
        {
            // A font the importer cannot outline (CFF, say) is skipped like one that is
            // absent, and the importer reports the text if nothing else covers it.
            return null;
        }
    }

    private static byte[]? ReadFile(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
