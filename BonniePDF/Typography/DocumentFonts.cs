using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>One font file in a document, identified by family, weight and italic.</summary>
public sealed class DocumentFontFace
{
    internal DocumentFontFace(string family, int weight, bool italic, string fontName, byte[] bytes, TtfFont font)
    {
        Family = family;
        Weight = weight;
        Italic = italic;
        FontName = fontName;
        Bytes = bytes;
        Font = font;
    }

    /// <summary>Family name as given (compared ignoring case and surrounding white space).</summary>
    public string Family { get; }

    /// <summary>Weight, 1-1000.</summary>
    public int Weight { get; }

    /// <summary>Whether the face is italic or oblique.</summary>
    public bool Italic { get; }

    /// <summary>
    /// A font name that selects this face wherever an element takes one (<c>FontName</c>, a
    /// watermark, a table of contents). Opaque, valid for the document that made it only, and
    /// never written to the output.
    /// </summary>
    public string FontName { get; }

    /// <summary>The font file.</summary>
    public byte[] Bytes { get; }

    /// <summary>The parsed font.</summary>
    internal TtfFont Font { get; }
}

/// <summary>
/// The fonts that belong to one document.
/// </summary>
/// <remarks>
/// A collection of faces: any number of families, with any number of weights and styles each.
/// Nothing added here is visible to another document, and nothing global changes. A family named
/// here is resolved before the global registrations and the standard fonts.
/// </remarks>
public sealed class DocumentFonts
{
    /// <summary>Prefix of every document font name. Opaque; see <see cref="DocumentFontFace.FontName"/>.</summary>
    internal const string FontNamePrefix = "virtual:docfont/";

    private static int _nextSerial;

    private readonly int _serial = Interlocked.Increment(ref _nextSerial);
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    // Insertion-ordered; replacing an identity keeps its place.
    private readonly List<string> _order = [];
    private readonly Dictionary<string, DocumentFontFace> _byIdentity = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Family, int Weight, bool Italic), DocumentFontFace?> _resolved = new();
    private int _version;

    /// <summary>True for a font name made by a <see cref="DocumentFonts"/>.</summary>
    internal static bool IsDocumentFontName(string name) =>
        name.StartsWith(FontNamePrefix, StringComparison.Ordinal);

    /// <summary>The faces, in registration order.</summary>
    public IReadOnlyList<DocumentFontFace> Faces
    {
        get
        {
            lock (_gate) return _order.Select(key => _byIdentity[key]).ToList();
        }
    }

    /// <summary>True when the collection holds no face.</summary>
    public bool IsEmpty
    {
        get
        {
            lock (_gate) return _order.Count == 0;
        }
    }

    /// <summary>
    /// Adds a TrueType font. A face with the same family, weight and italic replaces the earlier
    /// one and keeps its place. Anything not given is read from the font.
    /// </summary>
    /// <param name="bytes">The font file.</param>
    /// <param name="family">Family name. Defaults to the font's own.</param>
    /// <param name="weight">
    /// Weight, 1-1000; any such value may be cast to <see cref="FontWeight"/>. Defaults to the
    /// font's OS/2 weight class, or the weight its subfamily names.
    /// </param>
    /// <param name="italic">Whether the face is italic or oblique. Defaults to the font's own flags.</param>
    /// <exception cref="ArgumentException">
    /// The bytes are not a readable font, the family is empty, or the weight is outside 1-1000.
    /// </exception>
    /// <example>
    /// <code>
    /// var doc = new Document();
    /// doc.Fonts.Add(interRegularBytes, family: "Inter");
    /// doc.Fonts.Add(interBoldBytes, family: "Inter", weight: FontWeight.Bold);
    /// doc.Add(new TextBlock("Set in Inter") { FontFamily = "Inter" });
    /// </code>
    /// </example>
    public DocumentFontFace Add(byte[] bytes, string? family = null, FontWeight? weight = null, bool? italic = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        TtfFont font;
        try
        {
            // The same parse the PDF writer uses for these bytes, so measuring and writing share
            // one font and its shaping cache, and a service that keeps its font arrays parses once.
            font = TtfFont.LoadFromBytesCached(bytes);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"The bytes are not a readable TrueType font: {ex.Message}", nameof(bytes), ex);
        }

        string name = (family ?? font.FontFamily).Trim();
        if (name.Length == 0) throw new ArgumentException("A document font needs a family name.", nameof(family));
        int resolvedWeight = weight is { } w ? (int)w : DetectWeight(font);
        if (resolvedWeight is < 1 or > 1000)
            throw new ArgumentException($"Font weight must be between 1 and 1000; got {resolvedWeight}.", nameof(weight));
        bool resolvedItalic = italic ?? DetectItalic(font);

        string identity = string.Create(CultureInfo.InvariantCulture, $"{FamilyKey(name)}:{resolvedWeight}:{(resolvedItalic ? 'i' : 'n')}");
        var face = new DocumentFontFace(name, resolvedWeight, resolvedItalic,
            string.Create(CultureInfo.InvariantCulture, $"{FontNamePrefix}{_serial}/{identity}"), bytes, font);
        lock (_gate)
        {
            if (!_byIdentity.ContainsKey(identity)) _order.Add(identity);
            _byIdentity[identity] = face;
            _resolved.Clear();
            _version++;
        }
        return face;
    }

    /// <summary>
    /// The face for a family, weight and italic, or null when the document has no such family.
    /// Style is matched first, then weight in the CSS font-matching order.
    /// </summary>
    public DocumentFontFace? Resolve(string family, FontWeight weight, bool italic)
    {
        // Layout resolves the same few families for every line it measures and draws.
        var memoKey = (family, (int)weight, italic);
        int version;
        lock (_gate)
        {
            if (_resolved.TryGetValue(memoKey, out DocumentFontFace? known)) return known;
            version = _version;
        }
        DocumentFontFace? face = ResolveUncached(family, weight, italic);
        lock (_gate)
        {
            if (version == _version) _resolved[memoKey] = face;
        }
        return face;
    }

    private DocumentFontFace? ResolveUncached(string family, FontWeight weight, bool italic)
    {
        string key = FamilyKey(family);
        List<DocumentFontFace> candidates = Faces.Where(f => string.Equals(FamilyKey(f.Family), key, StringComparison.Ordinal)).ToList();
        return FaceMatching.Match(candidates, (int)weight, italic, f => f.Weight, f => f.Italic);
    }

    /// <summary>The face made by <see cref="Add"/> that has this font name, or null.</summary>
    internal DocumentFontFace? FaceForFontName(string fontName)
    {
        lock (_gate) return _byIdentity.Values.FirstOrDefault(f => string.Equals(f.FontName, fontName, StringComparison.Ordinal));
    }

    /// <summary>The faces as glyph-fallback candidates, in registration order.</summary>
    internal IReadOnlyList<FallbackFont> FallbackFonts() =>
        Faces.Select(f => new FallbackFont { Key = f.FontName, Bytes = f.Bytes, Font = f.Font }).ToList();

    private static string FamilyKey(string family) => family.Trim().ToLowerInvariant();

    private static int DetectWeight(TtfFont font) =>
        font.WeightClass is { } weightClass ? weightClass : FaceMatching.SubfamilyWeight(font.FontSubfamily);

    private static bool DetectItalic(TtfFont font) => font.SelectionItalic || FaceMatching.SubfamilyItalic(font.FontSubfamily);
}
