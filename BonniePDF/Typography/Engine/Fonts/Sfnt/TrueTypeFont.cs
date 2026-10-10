// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class TrueTypeFont
    {

        private readonly byte[] bytes;
        private readonly Dictionary<string, FontTable> tables;
        private readonly Dictionary<int, DecodedOutline> cache = new Dictionary<int, DecodedOutline>();
        private readonly OutlineReadLimits outlineLimits;
        private int cachedPoints;
        private readonly int[] locations;
        private readonly int metricCount;
        private readonly CharacterMap characterMap;
        internal readonly FontMetrics Metrics;
        internal readonly FontNames Names;
        internal readonly LegacyKerning Kerning;
        internal readonly int FaceCount, FaceIndex;
        public int UnitsPerEm
        {
            get; private set;
        }
        public int Ascender
        {
            get; private set;
        }
        public int Descender
        {
            get; private set;
        }
        public int GlyphCount
        {
            get; private set;
        }
        public bool HasTable(string tag)
        {
            return tables.ContainsKey(tag);
        }
        public byte[] GetTableData(string tag)
        {
            FontTable FontTable;
            if (!tables.TryGetValue(tag, out FontTable))
                return null;
            var data = new byte[FontTable.Length];
            Buffer.BlockCopy(bytes, FontTable.Offset, data, 0, data.Length);
            return data;
        }

        private void Check(int p, int length)
        {
            if (p < 0 || length < 0 || p > bytes.Length - length)
                throw new InvalidDataException("Truncated or invalid font data.");
        }
        private int U16(int p)
        {
            Check(p, 2);
            return (bytes[p] << 8) | bytes[p + 1];
        }
        private int I16(int p)
        {
            return (short)U16(p);
        }
        private uint U32(int p)
        {
            Check(p, 4);
            return ((uint)bytes[p] << 24) | ((uint)bytes[p + 1] << 16) |
                   ((uint)bytes[p + 2] << 8) | bytes[p + 3];
        }
        private int Offset32(int p)
        {
            uint value = U32(p);
            if (value > int.MaxValue)
                throw new InvalidDataException("Font offset exceeds the supported range.");
            return (int)value;
        }
        private string Tag(int p)
        {
            Check(p, 4);
            return System.Text.Encoding.ASCII.GetString(bytes, p, 4);
        }
        private int TableOffset(string tag, int minimum = 0)
        {
            FontTable FontTable;
            if (!tables.TryGetValue(tag, out FontTable) || FontTable.Length < minimum)
                throw new InvalidDataException("Missing or truncated '" + tag + "' FontTable.");
            return FontTable.Offset;
        }

        public TrueTypeFont(byte[] fontBytes) : this(fontBytes, new OutlineReadLimits())
        {
        }

        public TrueTypeFont(byte[] fontBytes, OutlineReadLimits outlineLimits) : this(fontBytes, outlineLimits, 0)
        {
        }

        internal TrueTypeFont(byte[] fontBytes, OutlineReadLimits outlineLimits, int faceIndex)
        {
            if (fontBytes == null)
                throw new ArgumentNullException("fontBytes");
            if (outlineLimits == null)
                throw new ArgumentNullException("outlineLimits");
            this.outlineLimits = outlineLimits;
            bytes = (byte[])fontBytes.Clone();
            var directory = new SfntDirectory(bytes, faceIndex);
            tables = directory.Tables;
            FaceCount = directory.FaceCount;
            FaceIndex = faceIndex;
            if (tables.ContainsKey("fvar"))
                throw new NotSupportedException("Variable fonts are unsupported; use a static font.");
            int head = TableOffset("head", 54), hhea = TableOffset("hhea", 36);
            UnitsPerEm = U16(head + 18);
            if (UnitsPerEm < 16 || UnitsPerEm > 16384)
                throw new InvalidDataException("Invalid unitsPerEm.");
            Ascender = I16(hhea + 4);
            Descender = I16(hhea + 6);
            metricCount = U16(hhea + 34);
            GlyphCount = U16(TableOffset("maxp", 6) + 4);
            if (GlyphCount == 0 || metricCount == 0 || metricCount > GlyphCount)
                throw new InvalidDataException("Invalid horizontal metrics.");
            TableOffset("hmtx", metricCount * 4 + (GlyphCount - metricCount) * 2);
            locations = new int[GlyphCount + 1];
            if (tables.ContainsKey("glyf"))
            {
                int format = I16(head + 50);
                if (format != 0 && format != 1)
                    throw new InvalidDataException("Unsupported loca format.");
                int loca = TableOffset("loca", (GlyphCount + 1) * (format == 0 ? 2 : 4));
                int glyf = TableOffset("glyf");
                for (int i = 0; i < locations.Length; i++)
                {
                    int relative = format == 0 ? U16(loca + i * 2) * 2 : Offset32(loca + i * 4);
                    if (relative > tables["glyf"].Length || (i > 0 && glyf + relative < locations[i - 1]))
                        throw new InvalidDataException("Invalid glyph locations.");
                    locations[i] = glyf + relative;
                }
            }
            else if (!tables.ContainsKey("CBDT"))
                throw new NotSupportedException("Font must contain TrueType outlines or CBDT colour bitmaps.");
            characterMap = new CharacterMap(GetTableData("cmap"), GlyphCount);
            Metrics = new FontMetrics(GetTableData("head"), GetTableData("hhea"), GetTableData("OS/2"), GetTableData("post"));
            Names = new FontNames(GetTableData("name"));
            Kerning = new LegacyKerning(GetTableData("kern"), GlyphCount);
        }

        public int GlyphIndex(int codePoint) { return characterMap.Map(codePoint); }
        internal int MapVariation(int scalar, int selector, out bool consumed) { return characterMap.MapVariation(scalar, selector, out consumed); }

        private void ValidateGlyph(int id)
        {
            if (id < 0 || id >= GlyphCount)
                throw new InvalidDataException("Invalid glyph ID.");
        }
        public int AdvanceWidth(int id)
        {
            ValidateGlyph(id);
            return U16(TableOffset("hmtx") + Math.Min(id, metricCount - 1) * 4);
        }
        public int LeftSideBearing(int id)
        {
            ValidateGlyph(id);
            int metrics = TableOffset("hmtx");
            return I16(id < metricCount ? metrics + id * 4 + 2 : metrics + metricCount * 4 + (id - metricCount) * 2);
        }




        public Glyph ReadGlyph(int id)
        {
            Glyph source = ReadGlyph(id, 0, new OutlineReadContext(outlineLimits)).Glyph;
            var result = new Glyph { XMin = source.XMin, YMin = source.YMin, XMax = source.XMax, YMax = source.YMax };
            foreach (var contour in source.Contours)
                result.Contours.Add(new List<FontPoint>(contour));
            return result;
        }
        private DecodedOutline ReadGlyph(int id, int depth, OutlineReadContext context)
        {
            ValidateGlyph(id);
            if (depth > outlineLimits.MaxDepth)
                throw new InvalidDataException("Outline depth limit exceeded.");
            DecodedOutline cached;
            if (cache.TryGetValue(id, out cached))
            {
                if (depth + cached.Depth > outlineLimits.MaxDepth)
                    throw new InvalidDataException("Outline depth limit exceeded.");
                context.Charge(cached.Operations, cached.Components);
                return cached;
            }
            if (!context.ActiveGlyphs.Add(id))
                throw new InvalidDataException("Composite glyph cycle detected.");
            long operationsBefore = context.Operations;
            int componentsBefore = context.Components;
            var result = new DecodedOutline { Glyph = new Glyph() };
            try
            {
                if (locations[id] != locations[id + 1])
                {
                    var reader = new GlyphDataReader(bytes, locations[id], locations[id + 1]);
                    int contourCount = reader.SignedWord();
                    result.Glyph.XMin = reader.SignedWord();
                    result.Glyph.YMin = reader.SignedWord();
                    result.Glyph.XMax = reader.SignedWord();
                    result.Glyph.YMax = reader.SignedWord();
                    if (contourCount > 0)
                        result.Points = ReadSimple(reader, contourCount, result.Glyph, context);
                    else if (contourCount < 0)
                        ReadComposite(reader, result, depth, context);
                }
                result.Operations = context.Operations - operationsBefore;
                result.Components = context.Components - componentsBefore;
                // Bound retained geometry as well as individual reads. Eviction changes
                // performance only; cached logical costs keep limits deterministic.
                if (result.Points <= 262144)
                {
                    if (cache.Count >= 256 || cachedPoints > 262144 - result.Points)
                    {
                        cache.Clear();
                        cachedPoints = 0;
                    }
                    cache.Add(id, result);
                    cachedPoints += result.Points;
                }
                return result;
            }
            finally
            {
                context.ActiveGlyphs.Remove(id);
            }
        }

        private static int ReadSimple(GlyphDataReader GlyphDataReader, int count, Glyph glyph, OutlineReadContext context)
        {
            context.CheckSize(0, count);
            var ends = new int[count];
            for (int i = 0; i < count; i++)
            {
                ends[i] = GlyphDataReader.Word();
                if (i > 0 && ends[i] <= ends[i - 1])
                    throw new InvalidDataException("Invalid contour endpoints.");
            }
            GlyphDataReader.Skip(GlyphDataReader.Word()); // Ignore TrueType hinting instructions for this first version.
            int pointCount = ends[count - 1] + 1;
            context.CheckSize(pointCount, count);
            context.Charge(pointCount + count);
            var flags = new int[pointCount];
            for (int i = 0; i < pointCount;)
            {
                int flag = GlyphDataReader.Byte(), repeat = (flag & 8) != 0 ? GlyphDataReader.Byte() : 0;
                if ((flag & 0x80) != 0)
                    throw new InvalidDataException("Reserved simple glyph flag.");
                if (i + repeat >= pointCount)
                    throw new InvalidDataException("Invalid repeated point flags.");
                for (int j = 0; j <= repeat; j++)
                    flags[i++] = flag;
            }
            var xs = ReadCoordinates(GlyphDataReader, flags, 2, 16);
            var ys = ReadCoordinates(GlyphDataReader, flags, 4, 32);
            int first = 0;
            foreach (int last in ends)
            {
                var contour = new List<FontPoint>();
                for (; first <= last; first++)
                    contour.Add(new FontPoint(xs[first], ys[first], (flags[first] & 1) != 0));
                glyph.Contours.Add(contour);
            }
            return pointCount;
        }
        private static int[] ReadCoordinates(GlyphDataReader GlyphDataReader, int[] flags, int shortBit, int sameBit)
        {
            var values = new int[flags.Length];
            int value = 0;
            for (int i = 0; i < flags.Length; i++)
            {
                int flag = flags[i];
                if ((flag & shortBit) != 0)
                    value += GlyphDataReader.Byte() * ((flag & sameBit) != 0 ? 1 : -1);
                else if ((flag & sameBit) == 0)
                    value += GlyphDataReader.SignedWord();
                values[i] = value;
            }
            return values;
        }

        private void ReadComposite(GlyphDataReader GlyphDataReader, DecodedOutline result, int depth, OutlineReadContext context)
        {
            int flags;
            bool hasInstructions = false;
            Glyph glyph = result.Glyph;
            do
            {
                context.Charge(1, 1);
                flags = GlyphDataReader.Word();
                if ((flags & 0xe010) != 0)
                    throw new InvalidDataException("Reserved composite glyph flags.");
                int transforms = flags & (8 | 64 | 128);
                if (transforms != 0 && transforms != 8 && transforms != 64 && transforms != 128)
                    throw new InvalidDataException("Conflicting composite transform flags.");
                hasInstructions |= (flags & 256) != 0;
                int childId = GlyphDataReader.Word();
                bool words = (flags & 1) != 0, xy = (flags & 2) != 0;
                if (!xy && glyph.Contours.Count == 0)
                    throw new InvalidDataException("Composite point attachment requires a preceding outline.");
                int arg1 = ReadArgument(GlyphDataReader, words, xy), arg2 = ReadArgument(GlyphDataReader, words, xy);
                double a = 1, b = 0, c = 0, d = 1;
                if ((flags & 8) != 0)
                    a = d = GlyphDataReader.Fixed();
                else if ((flags & 64) != 0)
                {
                    a = GlyphDataReader.Fixed();
                    d = GlyphDataReader.Fixed();
                }
                else if ((flags & 128) != 0)
                {
                    a = GlyphDataReader.Fixed();
                    b = GlyphDataReader.Fixed();
                    c = GlyphDataReader.Fixed();
                    d = GlyphDataReader.Fixed();
                }
                DecodedOutline childOutline = ReadGlyph(childId, depth + 1, context);
                context.CheckSize((long)result.Points + childOutline.Points,
                    (long)glyph.Contours.Count + childOutline.Glyph.Contours.Count);
                context.Charge(childOutline.Points * 2L + childOutline.Glyph.Contours.Count);
                result.Points += childOutline.Points;
                result.Depth = Math.Max(result.Depth, childOutline.Depth + 1);
                var child = new List<List<FontPoint>>();
                foreach (var contour in childOutline.Glyph.Contours)
                {
                    var transformed = new List<FontPoint>();
                    foreach (var pt in contour)
                        transformed.Add(new FontPoint(a * pt.X + c * pt.Y, b * pt.X + d * pt.Y, pt.OnCurve));
                    child.Add(transformed);
                }
                double dx = arg1, dy = arg2;
                if (!xy)
                {
                    FontPoint parentPoint = PointAt(glyph.Contours, arg1, context), childPoint = PointAt(child, arg2, context);
                    dx = parentPoint.X - childPoint.X;
                    dy = parentPoint.Y - childPoint.Y;
                }
                else if ((flags & 0x1800) == 0x800)
                {
                    dx = a * arg1 + c * arg2;
                    dy = b * arg1 + d * arg2;
                }
                // For unspecified offset scaling, choose the modern unscaled convention.
                // ROUND_XY_TO_GRID and USE_MY_METRICS are not interpreted in this unhinted prototype.
                foreach (var contour in child)
                {
                    for (int i = 0; i < contour.Count; i++)
                    {
                        var pt = contour[i];
                        pt.X += dx;
                        pt.Y += dy;
                        contour[i] = pt;
                    }
                    glyph.Contours.Add(contour);
                }
            } while ((flags & 32) != 0);
            if (hasInstructions)
                GlyphDataReader.Skip(GlyphDataReader.Word());
        }
        private static int ReadArgument(GlyphDataReader GlyphDataReader, bool words, bool signed)
        {
            return words ? (signed ? GlyphDataReader.SignedWord() : GlyphDataReader.Word()) : (signed ? GlyphDataReader.SignedByte() : GlyphDataReader.Byte());
        }
        private static FontPoint PointAt(List<List<FontPoint>> contours, int index, OutlineReadContext context)
        {
            foreach (var contour in contours)
            {
                context.Charge(1);
                if (index < contour.Count)
                    return contour[index];
                index -= contour.Count;
            }
            throw new InvalidDataException("Invalid composite point attachment.");
        }
    }
}
