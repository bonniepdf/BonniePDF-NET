// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BonniePDF.Typography.Engine
{
    internal sealed class FontFace
    {
        internal readonly ShapingLimits Limits;
        internal readonly TrueTypeFont Font;
        internal readonly string Name;
        internal readonly GlyphSubstitution Substitution;
        internal readonly GlyphPositioning Positioning;
        internal readonly BitmapStrikes Bitmaps;
        private readonly ColourLayers colourLayers;
        private readonly Dictionary<ShapeOptions, ShapingProgram> programs = new Dictionary<ShapeOptions, ShapingProgram>();
        private ShapingProgram defaultProgram;
        internal ShapingProgram Program(ShapeOptions options)
        {
            if (options == null)
                return defaultProgram ?? (defaultProgram = new ShapingProgram(Substitution, Positioning));
            ShapingProgram program;
            if (programs.TryGetValue(options, out program)) return program;
            var limits = options.Limits ?? Limits;
            program = new ShapingProgram(new GlyphSubstitution(Font.GetTableData("GSUB"), Font.GlyphCount, Font.GetTableData("GDEF"), limits, options),
                new GlyphPositioning(Font.GetTableData("GPOS"), Font.GetTableData("GDEF"), limits, options));
            // Cache caller-owned immutable option instances, with a strict face bound.
            if (programs.Count < 16) programs.Add(options, program);
            return program;
        }
        internal FontMetrics Metrics { get { return Font.Metrics; } }
        internal FontNames Names { get { return Font.Names; } }
        internal int FaceCount { get { return Font.FaceCount; } }
        internal int FaceIndex { get { return Font.FaceIndex; } }
        internal int UnitsPerEm { get { return Font.UnitsPerEm; } }
        internal int GlyphCount { get { return Font.GlyphCount; } }
        internal int MapGlyph(int scalar) { return Font.GlyphIndex(scalar); }
        internal int AdvanceWidth(int glyph) { return Font.AdvanceWidth(glyph); }
        internal int LeftSideBearing(int glyph) { return Font.LeftSideBearing(glyph); }
        internal Glyph ReadGlyph(int glyph) { return Font.ReadGlyph(glyph); }
        internal byte[] GetTableData(string tag) { return Font.GetTableData(tag); }
        internal bool Covers(int scalar) { return Font.GlyphIndex(scalar) != 0; }
        internal static int CountFaces(byte[] bytes) { return SfntDirectory.Count(bytes); }
        internal int GetPairKerning(int leftGlyph, int rightGlyph)
        {
            if (leftGlyph < 0 || leftGlyph >= Font.GlyphCount) throw new ArgumentOutOfRangeException("leftGlyph");
            if (rightGlyph < 0 || rightGlyph >= Font.GlyphCount) throw new ArgumentOutOfRangeException("rightGlyph");
            return Positioning.HasKerning ? Positioning.PairKerning(leftGlyph, rightGlyph) : Font.Kerning.Get(leftGlyph, rightGlyph);
        }
        internal bool HasColour
        {
            get
            {
                return colourLayers != null || Bitmaps != null;
            }
        }
        internal FontFace(byte[] fontBytes, ShapingLimits limits = null) : this(fontBytes, 0, limits)
        {
        }

        internal FontFace(byte[] fontBytes, int faceIndex, ShapingLimits limits = null)
            : this(new TrueTypeFont(fontBytes, (limits ?? new ShapingLimits()).Outlines, faceIndex), limits)
        {
        }

        internal FontFace(TrueTypeFont font, ShapingLimits limits = null)
        {
            if (font == null) throw new ArgumentNullException("font");
            Limits = limits ?? new ShapingLimits();
            Font = font;
            Name = Names.Family ?? "Unnamed font";
            var colrBytes = Font.GetTableData("COLR");
            if (colrBytes != null)
                colourLayers = new ColourLayers(colrBytes, Font.GetTableData("CPAL"), Font.GlyphCount, Limits.Colours);
            if (Font.HasTable("SVG ") && colourLayers == null && !Font.HasTable("CBDT"))
                throw new NotSupportedException("SVG-only colour fonts are unsupported.");
            if (Font.HasTable("CBDT") || Font.HasTable("CBLC"))
                Bitmaps = new BitmapStrikes(Font.GetTableData("CBLC"), Font.GetTableData("CBDT"), GlyphCount, Limits.Colours);
            if (Font.HasTable("sbix"))
                throw new NotSupportedException("sbix bitmap fonts are unsupported; use CBDT.");
            Substitution = new GlyphSubstitution(Font.GetTableData("GSUB"), Font.GlyphCount, Font.GetTableData("GDEF"), Limits);
            Positioning = new GlyphPositioning(Font.GetTableData("GPOS"), Font.GetTableData("GDEF"), Limits);
        }
        internal int MapVariation(int scalar, int selector)
        {
            bool consumed;
            return Font.MapVariation(scalar, selector, out consumed);
        }

        internal int MapVariation(int scalar, int selector, out bool consumed)
        {
            return Font.MapVariation(scalar, selector, out consumed);
        }

        internal int PaletteCount { get { return colourLayers == null ? 0 : colourLayers.PaletteCount; } }
        internal IReadOnlyList<PaletteColour> Palette(int palette)
        {
            if (colourLayers == null) throw new ArgumentOutOfRangeException("palette");
            return colourLayers.Palette(palette);
        }
        internal IReadOnlyList<ColourLayerData> Layers(int glyph, int palette = 0, PaletteColour? foreground = null)
        {
            if (glyph < 0 || glyph >= GlyphCount) throw new ArgumentOutOfRangeException("glyph");
            if (colourLayers != null) return colourLayers.Read(glyph, palette, foreground ?? new PaletteColour(0, 0, 0, 255));
            if (palette != 0) throw new ArgumentOutOfRangeException("palette");
            return Array.AsReadOnly(System.Array.Empty<ColourLayerData>());
        }
        internal BitmapGlyphData BitmapGlyph(int glyph, int ppem)
        {
            if (glyph < 0 || glyph >= GlyphCount) throw new ArgumentOutOfRangeException("glyph");
            if (ppem < 1 || ppem > 65535) throw new ArgumentOutOfRangeException("ppem");
            return Bitmaps == null ? null : Bitmaps.Read(glyph, ppem);
        }
    }
}
