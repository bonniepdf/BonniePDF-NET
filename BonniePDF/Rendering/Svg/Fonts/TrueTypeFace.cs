// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace BonniePDF.Rendering.Svg.Fonts;

/// <summary>
/// An <see cref="IFontFace"/> read from a TrueType (<c>glyf</c>-outline) font file.
/// </summary>
/// <remarks>
/// <para>
/// The reference implementation of the font seam, written from the public OpenType
/// specification. It reads only what outlining text needs: <c>head</c>, <c>maxp</c>,
/// <c>hhea</c>, <c>hmtx</c>, <c>cmap</c> (formats 4 and 12), <c>loca</c> and
/// <c>glyf</c>, including composite glyphs, plus pair kerning from <c>GPOS</c> or
/// <c>kern</c> (see <see cref="Fonts.Kerning"/>). CFF-flavoured OpenType fonts are
/// refused rather than half-read.
/// </para>
/// <para>
/// No hinting, and no shaping beyond pair kerning: glyphs are drawn at the scale the
/// document asks for, so hinting would move points away from the outline the design
/// intends.
/// </para>
/// </remarks>
internal sealed class TrueTypeFace : IFontFace
{
    private const int MaxCompositeDepth = 16;

    // Composite glyph flags.
    private const int Arg1And2AreWords = 0x0001;
    private const int ArgsAreXyValues = 0x0002;
    private const int WeHaveAScale = 0x0008;
    private const int MoreComponents = 0x0020;
    private const int WeHaveAnXAndYScale = 0x0040;
    private const int WeHaveATwoByTwo = 0x0080;
    private const int ScaledComponentOffset = 0x0800;

    // Simple glyph flags.
    private const int OnCurve = 0x01;
    private const int XShort = 0x02;
    private const int YShort = 0x04;
    private const int Repeat = 0x08;
    private const int XSameOrPositive = 0x10;
    private const int YSameOrPositive = 0x20;

    private readonly byte[] _data;
    private readonly int _numGlyphs;
    private readonly int _numberOfHMetrics;
    private readonly int _hmtx;
    private readonly int _glyf;
    private readonly uint[] _glyphOffsets;
    private readonly Func<int, int> _cmapLookup;
    private readonly Kerning _kerning;
    private readonly Dictionary<int, IReadOnlyList<GlyphSegment>> _outlines = [];

    /// <summary>Reads a TrueType font.</summary>
    /// <param name="bytes">The font file.</param>
    /// <param name="family">The family name, for diagnostics.</param>
    public TrueTypeFace(byte[] bytes, string family)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        _data = bytes;
        Family = family;

        Dictionary<string, int> tables = ReadTableDirectory();
        if (tables.ContainsKey("CFF ") || tables.ContainsKey("CFF2"))
        {
            throw new NotSupportedException($"{family}: CFF outlines are not supported; only TrueType (glyf) fonts are");
        }

        int Table(string tag) => tables.TryGetValue(tag, out int offset)
            ? offset
            : throw new InvalidOperationException($"{family}: missing required '{tag}' table");

        int head = Table("head");
        UnitsPerEm = U16(head + 18);
        bool longLoca = I16(head + 50) == 1;

        _numGlyphs = U16(Table("maxp") + 4);
        _numberOfHMetrics = U16(Table("hhea") + 34);
        _hmtx = Table("hmtx");
        _glyf = Table("glyf");

        int loca = Table("loca");
        _glyphOffsets = new uint[_numGlyphs + 1];
        for (int i = 0; i <= _numGlyphs; i++)
        {
            _glyphOffsets[i] = longLoca ? U32(loca + i * 4) : (uint)(U16(loca + i * 2) * 2);
        }

        _cmapLookup = ReadCmap(Table("cmap"));
        _kerning = Fonts.Kerning.Read(
            _data,
            tables.TryGetValue("GPOS", out int gpos) ? gpos : null,
            tables.TryGetValue("kern", out int kern) ? kern : null);
    }

    /// <inheritdoc />
    public string Family { get; }

    /// <inheritdoc />
    public int UnitsPerEm { get; }

    /// <inheritdoc />
    public int GlyphIndex(int codePoint)
    {
        int glyph = _cmapLookup(codePoint);
        return glyph < _numGlyphs ? glyph : 0;
    }

    /// <inheritdoc />
    public double AdvanceWidth(int glyph)
    {
        // Glyphs past the last long metric share its advance - monospaced tails.
        int index = Math.Min(glyph, _numberOfHMetrics - 1);
        return U16(_hmtx + index * 4);
    }

    /// <inheritdoc />
    public double Kerning(int left, int right) => _kerning.Adjustment(left, right);

    /// <inheritdoc />
    public IReadOnlyList<GlyphSegment> Outline(int glyph)
    {
        if (!_outlines.TryGetValue(glyph, out IReadOnlyList<GlyphSegment>? cached))
        {
            cached = ContoursToSegments(GlyphContours(glyph, 0));
            _outlines[glyph] = cached;
        }
        return cached;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Point(double X, double Y, bool On);

    private int U8(int offset) => _data[offset];
    private int I8(int offset) => (sbyte)_data[offset];
    private int U16(int offset) => BinaryPrimitives.ReadUInt16BigEndian(_data.AsSpan(offset));
    private int I16(int offset) => BinaryPrimitives.ReadInt16BigEndian(_data.AsSpan(offset));
    private uint U32(int offset) => BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(offset));
    private double F2Dot14(int offset) => I16(offset) / 16384.0;

    private Dictionary<string, int> ReadTableDirectory()
    {
        var tables = new Dictionary<string, int>(StringComparer.Ordinal);
        int count = U16(4);
        for (int i = 0; i < count; i++)
        {
            int record = 12 + i * 16;
            string tag = new([(char)_data[record], (char)_data[record + 1], (char)_data[record + 2], (char)_data[record + 3]]);
            tables[tag] = (int)U32(record + 8);
        }
        return tables;
    }

    /// <summary>
    /// Chooses the best Unicode subtable and returns a lookup over it. Format 12 (full
    /// Unicode) is preferred when present, since format 4 cannot address code points
    /// past the Basic Multilingual Plane.
    /// </summary>
    private Func<int, int> ReadCmap(int cmap)
    {
        int count = U16(cmap + 2);
        int? format4 = null;
        int? format12 = null;

        for (int i = 0; i < count; i++)
        {
            int record = cmap + 4 + i * 8;
            int platform = U16(record);
            int encoding = U16(record + 2);
            int subtable = cmap + (int)U32(record + 4);
            bool unicode = platform == 0 || (platform == 3 && (encoding == 1 || encoding == 10));
            if (!unicode) continue;

            int format = U16(subtable);
            if (format == 12) format12 ??= subtable;
            else if (format == 4) format4 ??= subtable;
        }

        if (format12 is { } f12) return codePoint => LookupFormat12(f12, codePoint);
        if (format4 is { } f4) return codePoint => LookupFormat4(f4, codePoint);
        throw new InvalidOperationException($"{Family}: no Unicode cmap subtable in format 4 or 12");
    }

    private int LookupFormat4(int subtable, int codePoint)
    {
        if (codePoint > 0xFFFF) return 0;
        int segCount = U16(subtable + 6) / 2;
        int endCodes = subtable + 14;
        int startCodes = endCodes + segCount * 2 + 2;
        int idDeltas = startCodes + segCount * 2;
        int idRangeOffsets = idDeltas + segCount * 2;

        for (int i = 0; i < segCount; i++)
        {
            if (U16(endCodes + i * 2) < codePoint) continue;
            int startCode = U16(startCodes + i * 2);
            if (startCode > codePoint) return 0;

            int idDelta = I16(idDeltas + i * 2);
            int rangeOffsetAt = idRangeOffsets + i * 2;
            int idRangeOffset = U16(rangeOffsetAt);
            if (idRangeOffset == 0) return (codePoint + idDelta) & 0xFFFF;

            int glyph = U16(rangeOffsetAt + idRangeOffset + (codePoint - startCode) * 2);
            return glyph == 0 ? 0 : (glyph + idDelta) & 0xFFFF;
        }
        return 0;
    }

    private int LookupFormat12(int subtable, int codePoint)
    {
        uint groups = U32(subtable + 12);
        for (uint i = 0; i < groups; i++)
        {
            int group = subtable + 16 + (int)i * 12;
            uint startChar = U32(group);
            if ((uint)codePoint < startChar) return 0;
            if ((uint)codePoint <= U32(group + 4)) return (int)(U32(group + 8) + ((uint)codePoint - startChar));
        }
        return 0;
    }

    /// <summary>The glyph's contours as points, composites resolved.</summary>
    private List<List<Point>> GlyphContours(int glyph, int depth)
    {
        if (glyph < 0 || glyph >= _numGlyphs) return [];
        int start = _glyf + (int)_glyphOffsets[glyph];
        int end = _glyf + (int)_glyphOffsets[glyph + 1];
        if (end <= start) return [];

        int contourCount = I16(start);
        return contourCount >= 0 ? ReadSimpleGlyph(start, contourCount) : ReadCompositeGlyph(start, depth);
    }

    private List<List<Point>> ReadCompositeGlyph(int start, int depth)
    {
        // A composite that nests more deeply than any real font does is a cycle in a
        // damaged file, not a design; stop rather than recurse without end.
        if (depth >= MaxCompositeDepth) return [];

        var contours = new List<List<Point>>();
        int offset = start + 10;
        int flags;

        do
        {
            flags = U16(offset);
            int component = U16(offset + 2);
            offset += 4;

            int arg1;
            int arg2;
            if ((flags & Arg1And2AreWords) != 0)
            {
                arg1 = I16(offset);
                arg2 = I16(offset + 2);
                offset += 4;
            }
            else if ((flags & ArgsAreXyValues) != 0)
            {
                arg1 = I8(offset);
                arg2 = I8(offset + 1);
                offset += 2;
            }
            else
            {
                // Point numbers are unsigned.
                arg1 = U8(offset);
                arg2 = U8(offset + 1);
                offset += 2;
            }

            double a = 1, b = 0, c = 0, d = 1;
            if ((flags & WeHaveAScale) != 0)
            {
                a = d = F2Dot14(offset);
                offset += 2;
            }
            else if ((flags & WeHaveAnXAndYScale) != 0)
            {
                a = F2Dot14(offset);
                d = F2Dot14(offset + 2);
                offset += 4;
            }
            else if ((flags & WeHaveATwoByTwo) != 0)
            {
                a = F2Dot14(offset);
                b = F2Dot14(offset + 2);
                c = F2Dot14(offset + 4);
                d = F2Dot14(offset + 6);
                offset += 8;
            }

            List<List<Point>> transformed = GlyphContours(component, depth + 1)
                .Select(contour => contour
                    .Select(p => new Point(a * p.X + c * p.Y, b * p.X + d * p.Y, p.On))
                    .ToList())
                .ToList();

            double dx;
            double dy;
            if ((flags & ArgsAreXyValues) != 0)
            {
                dx = arg1;
                dy = arg2;
                // Offsets are unscaled unless the font asks otherwise; this is the
                // Microsoft reading, which the flag exists to make explicit.
                if ((flags & ScaledComponentOffset) != 0)
                {
                    double sx = a * dx + c * dy;
                    double sy = b * dx + d * dy;
                    dx = sx;
                    dy = sy;
                }
            }
            else
            {
                // Point matching: move the component so its point arg2 lands on point
                // arg1 of the glyph assembled so far. Both are counted across contours.
                List<Point> parent = contours.SelectMany(contour => contour).ToList();
                List<Point> child = transformed.SelectMany(contour => contour).ToList();
                bool matched = arg1 < parent.Count && arg2 < child.Count;
                dx = matched ? parent[arg1].X - child[arg2].X : 0;
                dy = matched ? parent[arg1].Y - child[arg2].Y : 0;
            }

            foreach (List<Point> contour in transformed)
            {
                contours.Add(contour.Select(p => new Point(p.X + dx, p.Y + dy, p.On)).ToList());
            }
        }
        while ((flags & MoreComponents) != 0);

        return contours;
    }

    private List<List<Point>> ReadSimpleGlyph(int start, int contourCount)
    {
        int offset = start + 10;
        var endPoints = new List<int>(contourCount);
        for (int i = 0; i < contourCount; i++)
        {
            endPoints.Add(U16(offset));
            offset += 2;
        }
        int pointCount = contourCount == 0 ? 0 : endPoints[^1] + 1;

        int instructionLength = U16(offset);
        offset += 2 + instructionLength;

        var flags = new List<int>(pointCount);
        while (flags.Count < pointCount)
        {
            int flag = U8(offset++);
            flags.Add(flag);
            if ((flag & Repeat) != 0)
            {
                int repeats = U8(offset++);
                for (int r = 0; r < repeats && flags.Count < pointCount; r++) flags.Add(flag);
            }
        }

        var xs = new List<int>(pointCount);
        int x = 0;
        foreach (int flag in flags)
        {
            if ((flag & XShort) != 0)
            {
                int delta = U8(offset++);
                x += (flag & XSameOrPositive) != 0 ? delta : -delta;
            }
            else if ((flag & XSameOrPositive) == 0)
            {
                x += I16(offset);
                offset += 2;
            }
            xs.Add(x);
        }

        var ys = new List<int>(pointCount);
        int y = 0;
        foreach (int flag in flags)
        {
            if ((flag & YShort) != 0)
            {
                int delta = U8(offset++);
                y += (flag & YSameOrPositive) != 0 ? delta : -delta;
            }
            else if ((flag & YSameOrPositive) == 0)
            {
                y += I16(offset);
                offset += 2;
            }
            ys.Add(y);
        }

        var contours = new List<List<Point>>(contourCount);
        int first = 0;
        foreach (int last in endPoints)
        {
            var contour = new List<Point>(last - first + 1);
            for (int i = first; i <= last; i++) contour.Add(new Point(xs[i], ys[i], (flags[i] & OnCurve) != 0));
            contours.Add(contour);
            first = last + 1;
        }
        return contours;
    }

    /// <summary>Turns TrueType contours into segments.</summary>
    /// <remarks>
    /// Two consecutive off-curve points imply an on-curve point midway between them,
    /// so those are inserted first; after that every off-curve point is a quadratic
    /// control point between two on-curve neighbours, and the contour is started at an
    /// on-curve point. A contour of only off-curve points is legal and still works: the
    /// implied midpoints supply its on-curve points.
    /// </remarks>
    private static List<GlyphSegment> ContoursToSegments(List<List<Point>> contours)
    {
        var segments = new List<GlyphSegment>();

        foreach (List<Point> contour in contours)
        {
            if (contour.Count < 2) continue;

            var expanded = new List<Point>(contour.Count * 2);
            for (int i = 0; i < contour.Count; i++)
            {
                Point current = contour[i];
                Point next = contour[(i + 1) % contour.Count];
                expanded.Add(current);
                if (!current.On && !next.On)
                {
                    expanded.Add(new Point((current.X + next.X) / 2, (current.Y + next.Y) / 2, true));
                }
            }

            int startIndex = expanded.FindIndex(p => p.On);
            if (startIndex == -1) continue;
            List<Point> points = [.. expanded.Skip(startIndex), .. expanded.Take(startIndex)];

            segments.Add(new GlyphSegment(GlyphSegmentKind.Move, points[0].X, points[0].Y));
            int index = 1;
            while (index < points.Count)
            {
                Point point = points[index];
                if (point.On)
                {
                    segments.Add(new GlyphSegment(GlyphSegmentKind.Line, point.X, point.Y));
                    index += 1;
                }
                else
                {
                    // The contour is cyclic, so the control point after the last
                    // on-curve point closes onto the start.
                    Point end = points[(index + 1) % points.Count];
                    segments.Add(new GlyphSegment(GlyphSegmentKind.Quad, end.X, end.Y, point.X, point.Y));
                    index += 2;
                }
            }
            segments.Add(new GlyphSegment(GlyphSegmentKind.Close));
        }

        return segments;
    }
}
