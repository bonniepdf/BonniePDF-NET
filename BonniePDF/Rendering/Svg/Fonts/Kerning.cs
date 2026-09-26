// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF.Rendering.Svg.Fonts;

/// <summary>Pair kerning, read from the public OpenType specification.</summary>
/// <remarks>
/// <para>
/// Kerning is the one piece of shaping Latin text cannot do without: both reference
/// renderers apply it, and without it a word like "AVAWATYo" is visibly loose. It is
/// read from GPOS <c>kern</c> pair adjustments (lookup type 2, formats 1 and 2, also
/// inside type-9 extension lookups), falling back to a legacy <c>kern</c> table
/// (format 0) only when GPOS has no <c>kern</c> feature - the same precedence
/// HarfBuzz applies.
/// </para>
/// <para>
/// Deliberately narrow. Lookups come from the <c>latn</c> script's default language
/// system, else <c>DFLT</c>: a font whose Greek or Cyrillic kerning differs from its
/// Latin kerning is kerned as Latin. Only the first glyph's XAdvance is applied;
/// placement adjustments and the second glyph's value record move a glyph without
/// moving the pen, are rare in kerning, and are ignored. Lookup flags are not
/// honoured: a pair is two adjacent glyphs.
/// </para>
/// <para>Mirrors ts/src/Fonts/Kerning.ts.</para>
/// </remarks>
internal sealed class Kerning
{
    private static readonly Kerning None = new([], null, [], null);

    private readonly byte[] _data;
    private readonly List<List<int>>? _pairLookups;
    private readonly List<int> _legacySubtables;
    private readonly Dictionary<long, int>? _cache;

    private Kerning(byte[] data, List<List<int>>? pairLookups, List<int> legacySubtables, Dictionary<long, int>? cache)
    {
        _data = data;
        _pairLookups = pairLookups;
        _legacySubtables = legacySubtables;
        _cache = cache;
    }

    /// <summary>Reads a face's kerning from its GPOS or kern table offsets, when present.</summary>
    public static Kerning Read(byte[] data, int? gpos, int? kern)
    {
        List<List<int>>? subtables = gpos is { } g ? GposPairSubtables(data, g) : null;
        if (subtables is not null)
        {
            return subtables.Count == 0 ? None : new Kerning(data, subtables, [], []);
        }
        if (kern is { } k)
        {
            List<int> legacy = LegacySubtables(data, k);
            return legacy.Count == 0 ? None : new Kerning(data, null, legacy, null);
        }
        return None;
    }

    /// <summary>The pen adjustment between two glyphs, in font units.</summary>
    public int Adjustment(int left, int right)
    {
        if (_pairLookups is not null)
        {
            long key = (long)left * 65536 + right;
            if (_cache!.TryGetValue(key, out int cached)) return cached;
            int value = 0;
            // One adjustment per lookup: the first subtable that covers the pair.
            foreach (List<int> lookup in _pairLookups)
            {
                foreach (int subtable in lookup)
                {
                    if (PairAdjustment(subtable, left, right) is { } adjustment)
                    {
                        value += adjustment;
                        break;
                    }
                }
            }
            _cache[key] = value;
            return value;
        }
        return _legacySubtables.Count > 0 ? LegacyAdjustment(left, right) : 0;
    }

    /// <summary>
    /// The PairPos subtables of each <c>kern</c> lookup, in lookup-list order; empty
    /// when the feature exists but has nothing usable, and null when GPOS has no
    /// <c>kern</c> feature for the script at all - which is what lets a legacy
    /// <c>kern</c> table apply.
    /// </summary>
    private static List<List<int>>? GposPairSubtables(byte[] data, int gpos)
    {
        int scriptList = gpos + U16(data, gpos + 4);
        int featureList = gpos + U16(data, gpos + 6);
        int lookupList = gpos + U16(data, gpos + 8);

        int? langSys = DefaultLangSys(data, scriptList, "latn") ?? DefaultLangSys(data, scriptList, "DFLT");
        if (langSys is not { } ls) return null;

        var lookups = new SortedSet<int>();
        bool found = false;
        int featureCount = U16(data, ls + 4);
        for (int i = 0; i < featureCount; i++)
        {
            int featureIndex = U16(data, ls + 6 + i * 2);
            int record = featureList + 2 + featureIndex * 6;
            if (!string.Equals(Tag(data, record), "kern", StringComparison.Ordinal)) continue;
            found = true;
            int feature = featureList + U16(data, record + 4);
            int lookupCount = U16(data, feature + 2);
            for (int j = 0; j < lookupCount; j++) lookups.Add(U16(data, feature + 4 + j * 2));
        }
        if (!found) return null;

        var result = new List<List<int>>();
        foreach (int index in lookups)
        {
            int lookup = lookupList + U16(data, lookupList + 2 + index * 2);
            int type = U16(data, lookup);
            int count = U16(data, lookup + 4);
            var subtables = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int subtable = lookup + U16(data, lookup + 6 + i * 2);
                int subtableType = type;
                if (type == 9)
                {
                    subtableType = U16(data, subtable + 2);
                    subtable += (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(subtable + 4));
                }
                if (subtableType == 2) subtables.Add(subtable);
            }
            if (subtables.Count > 0) result.Add(subtables);
        }
        return result;
    }

    private static int? DefaultLangSys(byte[] data, int scriptList, string script)
    {
        int count = U16(data, scriptList);
        for (int i = 0; i < count; i++)
        {
            int record = scriptList + 2 + i * 6;
            if (!string.Equals(Tag(data, record), script, StringComparison.Ordinal)) continue;
            int table = scriptList + U16(data, record + 4);
            int offset = U16(data, table);
            return offset == 0 ? null : table + offset;
        }
        return null;
    }

    /// <summary>The first glyph's XAdvance adjustment from one PairPos subtable, or null if it does not cover the pair.</summary>
    private int? PairAdjustment(int subtable, int left, int right)
    {
        int format = U16(_data, subtable);
        int coverageIndex = Coverage(subtable + U16(_data, subtable + 2), left);
        if (coverageIndex < 0) return null;
        int format1 = U16(_data, subtable + 4);
        int format2 = U16(_data, subtable + 6);
        int size1 = ValueRecordSize(format1);
        int size2 = ValueRecordSize(format2);

        if (format == 1)
        {
            int pairSet = subtable + U16(_data, subtable + 10 + coverageIndex * 2);
            int count = U16(_data, pairSet);
            int recordSize = 2 + size1 + size2;
            // PairValueRecords are sorted by second glyph.
            int low = 0;
            int high = count - 1;
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                int record = pairSet + 2 + middle * recordSize;
                int second = U16(_data, record);
                if (second == right) return XAdvance(record + 2, format1);
                if (second < right) low = middle + 1;
                else high = middle - 1;
            }
            return null;
        }

        if (format == 2)
        {
            int class1 = ClassOf(subtable + U16(_data, subtable + 8), left);
            int class2 = ClassOf(subtable + U16(_data, subtable + 10), right);
            int class1Count = U16(_data, subtable + 12);
            int class2Count = U16(_data, subtable + 14);
            if (class1 >= class1Count || class2 >= class2Count) return null;
            int record = subtable + 16 + (class1 * class2Count + class2) * (size1 + size2);
            return XAdvance(record, format1);
        }
        return null;
    }

    /// <summary>A ValueRecord's size: two bytes per bit set in the low byte of its format.</summary>
    private static int ValueRecordSize(int format)
    {
        int size = 0;
        for (int bit = 0; bit < 8; bit++)
        {
            if ((format & (1 << bit)) != 0) size += 2;
        }
        return size;
    }

    /// <summary>The XAdvance field of a ValueRecord, which follows XPlacement and YPlacement when present.</summary>
    private int XAdvance(int record, int format)
    {
        if ((format & 0x0004) == 0) return 0;
        int offset = record;
        if ((format & 0x0001) != 0) offset += 2;
        if ((format & 0x0002) != 0) offset += 2;
        return I16(_data, offset);
    }

    /// <summary>A glyph's index in a Coverage table, or -1.</summary>
    private int Coverage(int table, int glyph)
    {
        int format = U16(_data, table);
        int count = U16(_data, table + 2);
        int low = 0;
        int high = count - 1;
        if (format == 1)
        {
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                int value = U16(_data, table + 4 + middle * 2);
                if (value == glyph) return middle;
                if (value < glyph) low = middle + 1;
                else high = middle - 1;
            }
        }
        else if (format == 2)
        {
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                int record = table + 4 + middle * 6;
                int start = U16(_data, record);
                int end = U16(_data, record + 2);
                if (glyph < start) high = middle - 1;
                else if (glyph > end) low = middle + 1;
                else return U16(_data, record + 4) + glyph - start;
            }
        }
        return -1;
    }

    /// <summary>A glyph's class in a ClassDef table; 0 when unlisted.</summary>
    private int ClassOf(int table, int glyph)
    {
        int format = U16(_data, table);
        if (format == 1)
        {
            int start = U16(_data, table + 2);
            int count = U16(_data, table + 4);
            return glyph >= start && glyph < start + count ? U16(_data, table + 6 + (glyph - start) * 2) : 0;
        }
        if (format == 2)
        {
            int count = U16(_data, table + 2);
            int low = 0;
            int high = count - 1;
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                int record = table + 4 + middle * 6;
                int start = U16(_data, record);
                int end = U16(_data, record + 2);
                if (glyph < start) high = middle - 1;
                else if (glyph > end) low = middle + 1;
                else return U16(_data, record + 4);
            }
        }
        return 0;
    }

    /// <summary>A legacy <c>kern</c> table's horizontal format-0 subtables.</summary>
    private static List<int> LegacySubtables(byte[] data, int kern)
    {
        var subtables = new List<int>();
        int count = U16(data, kern + 2);
        int offset = kern + 4;
        for (int i = 0; i < count; i++)
        {
            int length = U16(data, offset + 2);
            int coverageBits = U16(data, offset + 4);
            // Format 0, horizontal, kerning values rather than minimums, not cross-stream.
            if (coverageBits >> 8 == 0 && (coverageBits & 0x0007) == 0x0001) subtables.Add(offset + 6);
            offset += length;
        }
        return subtables;
    }

    /// <summary>The legacy table's adjustment, summed over its subtables.</summary>
    private int LegacyAdjustment(int left, int right)
    {
        long key = (long)left * 65536 + right;
        int total = 0;
        foreach (int table in _legacySubtables)
        {
            int pairs = U16(_data, table);
            int low = 0;
            int high = pairs - 1;
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                int record = table + 8 + middle * 6;
                long pair = (long)U16(_data, record) * 65536 + U16(_data, record + 2);
                if (pair == key)
                {
                    total += I16(_data, record + 4);
                    break;
                }
                if (pair < key) low = middle + 1;
                else high = middle - 1;
            }
        }
        return total;
    }

    private static int U16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));

    private static int I16(byte[] data, int offset) => BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset));

    private static string Tag(byte[] data, int offset) =>
        new(data.AsSpan(offset, 4).ToArray().Select(value => (char)value).ToArray());
}
