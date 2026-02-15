using System;
using System.Collections.Generic;
using System.Text;

namespace BonniePDF.Metadata;

/// <summary>
/// Generates a minimal valid sRGB ICC profile for PDF/A-1b compliance.
/// This profile contains the minimum required tags for an RGB color profile.
/// </summary>
internal static class SRgbIccProfile
{
    /// <summary>
    /// Generates a minimal sRGB ICC v2 profile.
    /// </summary>
    public static byte[] Generate()
    {
        // We build a minimal ICC v2.1 profile with:
        // - Profile header (128 bytes)
        // - Tag table
        // - Required tags: profileDescriptionTag, copyrightTag, mediaWhitePointTag,
        //   redMatrixColumnTag, greenMatrixColumnTag, blueMatrixColumnTag,
        //   redTRCTag, greenTRCTag, blueTRCTag

        List<byte> profile = new();

        // We'll build the tag data first, then construct header + tag table
        // sRGB primaries (D65 adapted, from IEC 61966-2.1)
        // XYZ values are in s15Fixed16Number format (value * 65536)

        // Media white point (D50, as required by ICC spec)
        byte[] whitePointData = BuildXyzTag(0.9505f, 1.0000f, 1.0890f);

        // Red matrix column (sRGB to XYZ D50)
        byte[] redMatrixData = BuildXyzTag(0.4360f, 0.2225f, 0.0139f);

        // Green matrix column
        byte[] greenMatrixData = BuildXyzTag(0.3851f, 0.7169f, 0.0971f);

        // Blue matrix column
        byte[] blueMatrixData = BuildXyzTag(0.1431f, 0.0606f, 0.7141f);

        // TRC (Tone Reproduction Curve) - use gamma 2.2 approximation
        // curveType with single gamma value
        byte[] trcData = BuildCurveTag(2.2f);

        // Description tag
        byte[] descData = BuildTextDescriptionTag("sRGB IEC61966-2.1");

        // Copyright tag
        byte[] copyrightData = BuildTextTag("Public Domain");

        // Now compute offsets. Layout:
        // [0..127] = header (128 bytes)
        // [128..] = tag table (9 tags * 12 bytes each + 4 bytes count = 112 bytes)
        // [240..] = tag data

        int tagCount = 9;
        int tagTableSize = 4 + tagCount * 12; // 4 for count + 12 per entry
        int dataOffset = 128 + tagTableSize;

        // Pad to 4-byte boundary
        dataOffset = Align4(dataOffset);

        // Assign offsets for each tag data block
        int descOffset = dataOffset;
        int copyrightOffset = descOffset + Align4(descData.Length);
        int whitePointOffset = copyrightOffset + Align4(copyrightData.Length);
        int redMatrixOffset = whitePointOffset + Align4(whitePointData.Length);
        int greenMatrixOffset = redMatrixOffset + Align4(redMatrixData.Length);
        int blueMatrixOffset = greenMatrixOffset + Align4(greenMatrixData.Length);
        int redTrcOffset = blueMatrixOffset + Align4(blueMatrixData.Length);
        // Green and Blue TRC share the same data as Red TRC (identical gamma)
        int greenTrcOffset = redTrcOffset;
        int blueTrcOffset = redTrcOffset;

        int totalSize = redTrcOffset + Align4(trcData.Length);

        // Build header (128 bytes)
        byte[] header = new byte[128];
        WriteUInt32BE(header, 0, (uint)totalSize);      // Profile size
        WriteBytes(header, 4, "acsp");                    // Preferred CMM type (not specified)
        WriteUInt32BE(header, 8, 0x02100000);            // Version 2.1.0
        WriteBytes(header, 12, "mntr");                   // Device class: Monitor
        WriteBytes(header, 16, "RGB ");                   // Color space: RGB
        WriteBytes(header, 20, "XYZ ");                   // PCS: XYZ
        // Date/time (bytes 24-35): 2000-01-01 00:00:00
        WriteUInt16BE(header, 24, 2000);                  // Year
        WriteUInt16BE(header, 26, 1);                     // Month
        WriteUInt16BE(header, 28, 1);                     // Day
        WriteBytes(header, 36, "acsp");                   // Profile file signature
        WriteBytes(header, 40, "NONE");                   // Primary platform
        // Profile flags, device manufacturer, device model: zeros
        // Rendering intent: 0 = Perceptual
        // PCS illuminant (D50): X=0.9642, Y=1.0, Z=0.8249 in s15Fixed16
        WriteS15Fixed16(header, 68, 0.9642f);
        WriteS15Fixed16(header, 72, 1.0000f);
        WriteS15Fixed16(header, 76, 0.8249f);

        profile.AddRange(header);

        // Build tag table
        byte[] tagTable = new byte[tagTableSize];
        WriteUInt32BE(tagTable, 0, (uint)tagCount);

        int idx = 4;
        // desc
        WriteTagEntry(tagTable, idx, "desc", descOffset, descData.Length); idx += 12;
        // cprt
        WriteTagEntry(tagTable, idx, "cprt", copyrightOffset, copyrightData.Length); idx += 12;
        // wtpt
        WriteTagEntry(tagTable, idx, "wtpt", whitePointOffset, whitePointData.Length); idx += 12;
        // rXYZ
        WriteTagEntry(tagTable, idx, "rXYZ", redMatrixOffset, redMatrixData.Length); idx += 12;
        // gXYZ
        WriteTagEntry(tagTable, idx, "gXYZ", greenMatrixOffset, greenMatrixData.Length); idx += 12;
        // bXYZ
        WriteTagEntry(tagTable, idx, "bXYZ", blueMatrixOffset, blueMatrixData.Length); idx += 12;
        // rTRC
        WriteTagEntry(tagTable, idx, "rTRC", redTrcOffset, trcData.Length); idx += 12;
        // gTRC
        WriteTagEntry(tagTable, idx, "gTRC", greenTrcOffset, trcData.Length); idx += 12;
        // bTRC
        WriteTagEntry(tagTable, idx, "bTRC", blueTrcOffset, trcData.Length);

        profile.AddRange(tagTable);

        // Pad to data offset
        while (profile.Count < dataOffset)
            profile.Add(0);

        // Write tag data blocks
        WriteAlignedBlock(profile, descOffset, descData);
        WriteAlignedBlock(profile, copyrightOffset, copyrightData);
        WriteAlignedBlock(profile, whitePointOffset, whitePointData);
        WriteAlignedBlock(profile, redMatrixOffset, redMatrixData);
        WriteAlignedBlock(profile, greenMatrixOffset, greenMatrixData);
        WriteAlignedBlock(profile, blueMatrixOffset, blueMatrixData);
        WriteAlignedBlock(profile, redTrcOffset, trcData);

        // Ensure total size matches
        while (profile.Count < totalSize)
            profile.Add(0);

        return profile.ToArray();
    }

    private static void WriteAlignedBlock(List<byte> profile, int offset, byte[] data)
    {
        // Ensure profile is large enough
        while (profile.Count < offset + Align4(data.Length))
            profile.Add(0);

        // Write data at offset
        for (int i = 0; i < data.Length; i++)
            profile[offset + i] = data[i];
    }

    private static byte[] BuildXyzTag(float x, float y, float z)
    {
        byte[] data = new byte[20];
        WriteBytes(data, 0, "XYZ ");  // Type signature
        // Reserved: 4 bytes zero
        WriteS15Fixed16(data, 8, x);
        WriteS15Fixed16(data, 12, y);
        WriteS15Fixed16(data, 16, z);
        return data;
    }

    private static byte[] BuildCurveTag(float gamma)
    {
        byte[] data = new byte[12];
        WriteBytes(data, 0, "curv");  // Type signature
        // Reserved: 4 bytes zero
        WriteUInt32BE(data, 8, 1);    // Count = 1 (single gamma value)
        // Extend to include the u8Fixed8Number gamma
        byte[] result = new byte[14];
        Buffer.BlockCopy(data, 0, result, 0, 12);
        // u8Fixed8Number: integer part in high byte, fraction in low byte
        int gammaFixed = (int)(gamma * 256.0f);
        result[12] = (byte)(gammaFixed >> 8);
        result[13] = (byte)(gammaFixed & 0xFF);
        return result;
    }

    private static byte[] BuildTextDescriptionTag(string text)
    {
        byte[] textBytes = Encoding.ASCII.GetBytes(text);
        // desc type: 'desc' + reserved(4) + ASCII count(4) + ASCII string + null + padding
        int size = 12 + textBytes.Length + 1;
        // Add Unicode and ScriptCode placeholders (minimum)
        size += 4 + 4 + 2 + 1 + 67; // Unicode count + Unicode string (empty) + ScriptCode
        byte[] data = new byte[size];
        WriteBytes(data, 0, "desc");
        // Reserved: 4 bytes zero
        WriteUInt32BE(data, 8, (uint)(textBytes.Length + 1));
        Buffer.BlockCopy(textBytes, 0, data, 12, textBytes.Length);
        // null terminator at data[12 + textBytes.Length] is already 0
        // Unicode count = 0 at offset 12 + textBytes.Length + 1
        // ScriptCode count = 0 at end
        return data;
    }

    private static byte[] BuildTextTag(string text)
    {
        byte[] textBytes = Encoding.ASCII.GetBytes(text);
        byte[] data = new byte[8 + textBytes.Length + 1];
        WriteBytes(data, 0, "text");
        // Reserved: 4 bytes zero
        Buffer.BlockCopy(textBytes, 0, data, 8, textBytes.Length);
        return data;
    }

    private static void WriteTagEntry(byte[] table, int offset, string sig, int dataOffset, int dataSize)
    {
        WriteBytes(table, offset, sig);
        WriteUInt32BE(table, offset + 4, (uint)dataOffset);
        WriteUInt32BE(table, offset + 8, (uint)dataSize);
    }

    private static void WriteUInt32BE(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteUInt16BE(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static void WriteS15Fixed16(byte[] data, int offset, float value)
    {
        int fixedValue = (int)(value * 65536.0f);
        WriteUInt32BE(data, offset, (uint)fixedValue);
    }

    private static void WriteBytes(byte[] data, int offset, string ascii)
    {
        for (int i = 0; i < ascii.Length && i + offset < data.Length; i++)
            data[offset + i] = (byte)ascii[i];
    }

    private static int Align4(int value)
    {
        return (value + 3) & ~3;
    }
}
