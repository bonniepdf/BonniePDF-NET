// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ColourLayers
    {
        private readonly BigEndianData colr, cpal;
        private readonly int glyphCount, bases, records, layers, layerCount, colourRecords, colourCount, entries, v1List;
        private readonly ColourReadLimits limits;
        private long operations;
        internal readonly int PaletteCount;

        private void Tick()
        {
            if (++operations > limits.MaxOperations) throw new InvalidDataException("Colour table operation limit exceeded.");
        }

        internal ColourLayers(byte[] colrBytes, byte[] cpalBytes, int glyphCount, ColourReadLimits limits)
        {
            this.glyphCount = glyphCount; this.limits = limits;
            if (cpalBytes == null) throw new InvalidDataException("COLR requires a CPAL palette.");
            colr = new BigEndianData(colrBytes); cpal = new BigEndianData(cpalBytes);
            colr.Check(0, 14); cpal.Check(0, 12);
            int version = colr.U16(0), paletteVersion = cpal.U16(0);
            if (version > 1 || (version == 1 && colr.U16(2) == 0))
                throw new NotSupportedException("COLRv1 paint graphs are unsupported. Use COLRv0 compatibility layers.");
            if (paletteVersion > 1) throw new NotSupportedException("Unsupported CPAL version.");
            entries = cpal.U16(2); PaletteCount = cpal.U16(4); colourCount = cpal.U16(6); colourRecords = cpal.Offset32(8);
            if (entries == 0 || PaletteCount == 0) throw new InvalidDataException("Invalid CPAL palette.");
            if (entries > limits.MaxRecords || PaletteCount > limits.MaxRecords || colourCount > limits.MaxRecords)
                throw new InvalidDataException("CPAL record limit exceeded.");
            int header = checked(12 + PaletteCount * 2 + (paletteVersion == 1 ? 12 : 0));
            cpal.Check(0, header);
            if (colourRecords < header) throw new InvalidDataException("CPAL colour records overlap the header.");
            cpal.Check(colourRecords, colourCount * 4);
            for (int i = 0; i < PaletteCount; i++)
            {
                Tick();
                if (cpal.U16(12 + i * 2) + entries > colourCount) throw new InvalidDataException("CPAL palette range is invalid.");
            }
            if (paletteVersion == 1)
                for (int i = 0; i < 3; i++)
                {
                    int offset = cpal.Offset32(12 + PaletteCount * 2 + i * 4);
                    if (offset == 0) continue;
                    if (offset < header) throw new InvalidDataException("CPAL metadata overlaps the header.");
                    cpal.Check(offset, (i == 2 ? entries : PaletteCount) * (i == 0 ? 4 : 2));
                }
            bases = colr.U16(2); records = colr.Offset32(4); layers = colr.Offset32(8); layerCount = colr.U16(12);
            if (bases > limits.MaxRecords || layerCount > limits.MaxRecords) throw new InvalidDataException("COLR record limit exceeded.");
            int colrHeader = version == 0 ? 14 : 34;
            colr.Check(0, colrHeader);
            if ((bases != 0 && records < colrHeader) || (layerCount != 0 && layers < colrHeader))
                throw new InvalidDataException("COLR records overlap the header.");
            colr.Check(records, bases * 6); colr.Check(layers, layerCount * 4);
            int previous = -1;
            for (int i = 0; i < bases; i++)
            {
                Tick(); int p = records + i * 6, id = colr.U16(p), count = colr.U16(p + 4);
                if (id <= previous || id >= glyphCount) throw new InvalidDataException("Invalid COLR base glyph order or glyph ID.");
                if (colr.U16(p + 2) + count > layerCount) throw new InvalidDataException("COLR layer range is invalid.");
                previous = id;
            }
            for (int i = 0; i < layerCount; i++)
            {
                Tick(); int p = layers + i * 4, index = colr.U16(p + 2);
                if (colr.U16(p) >= glyphCount) throw new InvalidDataException("COLR references an invalid glyph.");
                if (index != 65535 && index >= entries) throw new InvalidDataException("COLR palette index is invalid.");
            }
            if (version == 1)
            {
                v1List = colr.Offset32(14);
                if (v1List != 0)
                {
                    if (v1List < colrHeader) throw new InvalidDataException("COLRv1 base list overlaps the header.");
                    int count = colr.Offset32(v1List);
                    if (count > limits.MaxRecords) throw new InvalidDataException("COLRv1 record limit exceeded.");
                    colr.Check(v1List + 4, checked(count * 6)); previous = -1;
                    for (int i = 0; i < count; i++)
                    {
                        Tick(); int p = v1List + 4 + i * 6, id = colr.U16(p);
                        if (id <= previous || id >= glyphCount) throw new InvalidDataException("Invalid COLRv1 base glyph order or glyph ID.");
                        int paint = colr.Offset32(p + 2);
                        if (paint < 4 + count * 6) throw new InvalidDataException("COLRv1 paint overlaps the base list.");
                        colr.Check(checked(v1List + paint), 1);
                        previous = id;
                    }
                }
            }
        }

        private void ValidatePalette(int palette)
        {
            if (palette < 0 || palette >= PaletteCount) throw new ArgumentOutOfRangeException("palette");
        }

        private PaletteColour Colour(int palette, int entry)
        {
            int p = colourRecords + (cpal.U16(12 + palette * 2) + entry) * 4;
            return new PaletteColour((byte)cpal.U8(p + 2), (byte)cpal.U8(p + 1), (byte)cpal.U8(p), (byte)cpal.U8(p + 3));
        }

        internal IReadOnlyList<PaletteColour> Palette(int palette)
        {
            ValidatePalette(palette); operations = 0;
            var result = new PaletteColour[entries];
            for (int i = 0; i < entries; i++) { Tick(); result[i] = Colour(palette, i); }
            return Array.AsReadOnly(result);
        }

        private int Find(int offset, int count, int glyph)
        {
            int low = 0, high = count - 1;
            while (low <= high)
            {
                Tick(); int middle = low + (high - low) / 2, p = offset + middle * 6, id = colr.U16(p);
                if (glyph < id) high = middle - 1;
                else if (glyph > id) low = middle + 1;
                else return p;
            }
            return -1;
        }

        internal IReadOnlyList<ColourLayerData> Read(int glyph, int palette, PaletteColour foreground)
        {
            if (glyph < 0 || glyph >= glyphCount) throw new ArgumentOutOfRangeException("glyph");
            ValidatePalette(palette); operations = 0;
            int record = Find(records, bases, glyph);
            if (record < 0)
            {
                if (v1List != 0 && Find(v1List + 4, colr.Offset32(v1List), glyph) >= 0)
                    throw new NotSupportedException("This glyph requires a COLRv1 paint graph and has no COLRv0 compatibility layers.");
                return Array.AsReadOnly(System.Array.Empty<ColourLayerData>());
            }
            int first = colr.U16(record + 2), count = colr.U16(record + 4);
            if (count > limits.MaxLayers) throw new InvalidDataException("COLR layer limit exceeded.");
            var result = new ColourLayerData[count];
            for (int i = 0; i < count; i++)
            {
                Tick(); int p = layers + (first + i) * 4, index = colr.U16(p + 2);
                result[i] = new ColourLayerData(colr.U16(p), index, index == 65535 ? foreground : Colour(palette, index));
            }
            return Array.AsReadOnly(result);
        }
    }
}
