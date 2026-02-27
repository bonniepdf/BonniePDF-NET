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
    // --- Meaningful Constants ---

    private static class IccSizes
    {
        public const int HeaderSize = 128;
        public const int TagTableEntrySize = 12;
        public const int TagTableHeaderSize = 4;
    }

    private static class IccSignatures
    {
        public const string ProfileSignature = "acsp";
        public const string DeviceClassMonitor = "mntr";
        public const string ColorSpaceRgb = "RGB ";
        public const string PcsXyz = "XYZ ";
        public const string PlatformNone = "NONE";

        // Tag Signatures
        public const string Desc = "desc";
        public const string Copyright = "cprt";
        public const string WhitePoint = "wtpt";
        public const string RedMatrix = "rXYZ";
        public const string GreenMatrix = "gXYZ";
        public const string BlueMatrix = "bXYZ";
        public const string RedTrc = "rTRC";
        public const string GreenTrc = "gTRC";
        public const string BlueTrc = "bTRC";

        // Data Type Signatures
        public const string TypeXyz = "XYZ ";
        public const string TypeCurve = "curv";
        public const string TypeText = "text";
    }

    private static class SrgbColorimetry
    {
        // Media white point (D50)
        public const float WhiteX = 0.9505f;
        public const float WhiteY = 1.0000f;
        public const float WhiteZ = 1.0890f;

        // D50 PCS Illuminant (for header)
        public const float PcsIlluminantX = 0.9642f;
        public const float PcsIlluminantY = 1.0000f;
        public const float PcsIlluminantZ = 0.8249f;

        // Matrix Columns
        public const float RedX = 0.4360f, RedY = 0.2225f, RedZ = 0.0139f;
        public const float GreenX = 0.3851f, GreenY = 0.7169f, GreenZ = 0.0971f;
        public const float BlueX = 0.1431f, BlueY = 0.0606f, BlueZ = 0.7141f;

        public const float Gamma = 2.2f;
    }

    private static class FixedPointMath
    {
        public const float S15Fixed16Multiplier = 65536.0f;
        public const float U8Fixed8Multiplier = 256.0f;
    }

    /// <summary>
    /// Generates a minimal sRGB ICC v2 profile.
    /// </summary>
    public static byte[] Generate()
    {
        List<byte> profile = new();

        // Generate Tag Data Blocks
        byte[] whitePointData = BuildXyzTag(SrgbColorimetry.WhiteX, SrgbColorimetry.WhiteY, SrgbColorimetry.WhiteZ);
        byte[] redMatrixData = BuildXyzTag(SrgbColorimetry.RedX, SrgbColorimetry.RedY, SrgbColorimetry.RedZ);
        byte[] greenMatrixData = BuildXyzTag(SrgbColorimetry.GreenX, SrgbColorimetry.GreenY, SrgbColorimetry.GreenZ);
        byte[] blueMatrixData = BuildXyzTag(SrgbColorimetry.BlueX, SrgbColorimetry.BlueY, SrgbColorimetry.BlueZ);
        byte[] trcData = BuildCurveTag(SrgbColorimetry.Gamma);
        byte[] descData = BuildTextDescriptionTag("sRGB IEC61966-2.1");
        byte[] copyrightData = BuildTextTag("Public Domain");

        // Layout Calculations
        const int tagCount = 9;
        int tagTableSize = IccSizes.TagTableHeaderSize + (tagCount * IccSizes.TagTableEntrySize);
        int dataOffset = Align4(IccSizes.HeaderSize + tagTableSize);

        // Assign offsets for each tag data block
        int descOffset = dataOffset;
        int copyrightOffset = descOffset + Align4(descData.Length);
        int whitePointOffset = copyrightOffset + Align4(copyrightData.Length);
        int redMatrixOffset = whitePointOffset + Align4(whitePointData.Length);
        int greenMatrixOffset = redMatrixOffset + Align4(redMatrixData.Length);
        int blueMatrixOffset = greenMatrixOffset + Align4(greenMatrixData.Length);
        int redTrcOffset = blueMatrixOffset + Align4(blueMatrixData.Length);
        int greenTrcOffset = redTrcOffset; // Shared TRC data
        int blueTrcOffset = redTrcOffset;  // Shared TRC data

        int totalSize = redTrcOffset + Align4(trcData.Length);

        // Build header (128 bytes)
        byte[] header = new byte[IccSizes.HeaderSize];
        WriteUInt32BE(header, 0, (uint)totalSize);
        WriteBytes(header, 4, IccSignatures.ProfileSignature);
        WriteUInt32BE(header, 8, 0x02100000); // Version 2.1.0
        WriteBytes(header, 12, IccSignatures.DeviceClassMonitor);
        WriteBytes(header, 16, IccSignatures.ColorSpaceRgb);
        WriteBytes(header, 20, IccSignatures.PcsXyz);

        // Date/time: 2000-01-01 00:00:00
        WriteUInt16BE(header, 24, 2000);
        WriteUInt16BE(header, 26, 1);
        WriteUInt16BE(header, 28, 1);

        WriteBytes(header, 36, IccSignatures.ProfileSignature);
        WriteBytes(header, 40, IccSignatures.PlatformNone);

        // PCS illuminant (D50)
        WriteS15Fixed16(header, 68, SrgbColorimetry.PcsIlluminantX);
        WriteS15Fixed16(header, 72, SrgbColorimetry.PcsIlluminantY);
        WriteS15Fixed16(header, 76, SrgbColorimetry.PcsIlluminantZ);

        profile.AddRange(header);

        // Build tag table
        byte[] tagTable = new byte[tagTableSize];
        WriteUInt32BE(tagTable, 0, tagCount);

        int idx = 4;
        WriteTagEntry(tagTable, ref idx, IccSignatures.Desc, descOffset, descData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.Copyright, copyrightOffset, copyrightData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.WhitePoint, whitePointOffset, whitePointData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.RedMatrix, redMatrixOffset, redMatrixData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.GreenMatrix, greenMatrixOffset, greenMatrixData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.BlueMatrix, blueMatrixOffset, blueMatrixData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.RedTrc, redTrcOffset, trcData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.GreenTrc, greenTrcOffset, trcData.Length);
        WriteTagEntry(tagTable, ref idx, IccSignatures.BlueTrc, blueTrcOffset, trcData.Length);

        profile.AddRange(tagTable);

        // Pad to data offset and write blocks
        PadToOffset(profile, dataOffset);
        WriteAlignedBlock(profile, descOffset, descData);
        WriteAlignedBlock(profile, copyrightOffset, copyrightData);
        WriteAlignedBlock(profile, whitePointOffset, whitePointData);
        WriteAlignedBlock(profile, redMatrixOffset, redMatrixData);
        WriteAlignedBlock(profile, greenMatrixOffset, greenMatrixData);
        WriteAlignedBlock(profile, blueMatrixOffset, blueMatrixData);
        WriteAlignedBlock(profile, redTrcOffset, trcData);

        PadToOffset(profile, totalSize);

        return profile.ToArray();
    }

    // --- Helper Methods ---

    private static void PadToOffset(List<byte> profile, int targetOffset)
    {
        while (profile.Count < targetOffset)
            profile.Add(0);
    }

    private static void WriteAlignedBlock(List<byte> profile, int offset, byte[] data)
    {
        PadToOffset(profile, offset + Align4(data.Length));
        for (int i = 0; i < data.Length; i++)
            profile[offset + i] = data[i];
    }

    private static byte[] BuildXyzTag(float x, float y, float z)
    {
        byte[] data = new byte[20];
        WriteBytes(data, 0, IccSignatures.TypeXyz);
        WriteS15Fixed16(data, 8, x);
        WriteS15Fixed16(data, 12, y);
        WriteS15Fixed16(data, 16, z);
        return data;
    }

    private static byte[] BuildCurveTag(float gamma)
    {
        byte[] data = new byte[12];
        WriteBytes(data, 0, IccSignatures.TypeCurve);
        WriteUInt32BE(data, 8, 1); // Count = 1

        byte[] result = new byte[14];
        Buffer.BlockCopy(data, 0, result, 0, 12);

        int gammaFixed = (int)(gamma * FixedPointMath.U8Fixed8Multiplier);
        result[12] = (byte)(gammaFixed >> 8);
        result[13] = (byte)(gammaFixed & 0xFF);
        return result;
    }

    private static byte[] BuildTextDescriptionTag(string text)
    {
        byte[] textBytes = Encoding.ASCII.GetBytes(text);
        int size = 12 + textBytes.Length + 1 + 4 + 4 + 2 + 1 + 67;
        byte[] data = new byte[size];

        WriteBytes(data, 0, IccSignatures.Desc);
        WriteUInt32BE(data, 8, (uint)(textBytes.Length + 1));
        Buffer.BlockCopy(textBytes, 0, data, 12, textBytes.Length);
        return data;
    }

    private static byte[] BuildTextTag(string text)
    {
        byte[] textBytes = Encoding.ASCII.GetBytes(text);
        byte[] data = new byte[8 + textBytes.Length + 1];

        WriteBytes(data, 0, IccSignatures.TypeText);
        Buffer.BlockCopy(textBytes, 0, data, 8, textBytes.Length);
        return data;
    }

    // Notice 'offset' is passed by ref so we don't have to manually track `idx += 12`
    private static void WriteTagEntry(byte[] table, ref int offset, string sig, int dataOffset, int dataSize)
    {
        WriteBytes(table, offset, sig);
        WriteUInt32BE(table, offset + 4, (uint)dataOffset);
        WriteUInt32BE(table, offset + 8, (uint)dataSize);
        offset += IccSizes.TagTableEntrySize;
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
        int fixedValue = (int)(value * FixedPointMath.S15Fixed16Multiplier);
        WriteUInt32BE(data, offset, (uint)fixedValue);
    }

    private static void WriteBytes(byte[] data, int offset, string ascii)
    {
        for (int i = 0; i < ascii.Length && i + offset < data.Length; i++)
            data[offset + i] = (byte)ascii[i];
    }

    private static int Align4(int value) => (value + 3) & ~3;
}