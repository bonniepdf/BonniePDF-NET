using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace BonniePDF;

/// <summary>Exact channel separation with guarded acceleration and bounded span accesses.</summary>
internal static class PngPixelOperations
{
    // This loop runs for every row of the first PNG too. Optimise it immediately:
    // Tier0 otherwise retains span helper calls for every group of four pixels.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void SplitRgbaRow(ReadOnlySpan<byte> rgba, Span<byte> color, Span<byte> alpha)
    {
        int pixels = rgba.Length / 4;
        if (rgba.Length % 4 != 0 || alpha.Length != pixels || color.Length != pixels * 3)
            throw new ArgumentException("RGBA input and channel output lengths must describe the same pixels.");

        int x = 0;
        if (BitConverter.IsLittleEndian && (Ssse3.IsSupported || AdvSimd.Arm64.IsSupported))
        {
            var colorIndices = Vector128.Create((byte)0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 255, 255, 255, 255);
            var alphaIndices = Vector128.Create((byte)3, 7, 11, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
            for (; x <= pixels - 4; x += 4)
            {
                var input = MemoryMarshal.Read<Vector128<byte>>(rgba.Slice(x * 4, 16));
                var rgb = Vector128.Shuffle(input, colorIndices);
                ulong first = rgb.AsUInt64().GetElement(0);
                uint last = rgb.AsUInt32().GetElement(2);
                uint mask = Vector128.Shuffle(input, alphaIndices).AsUInt32().GetElement(0);
                MemoryMarshal.Write(color.Slice(x * 3, 8), in first);
                MemoryMarshal.Write(color.Slice(x * 3 + 8, 4), in last);
                MemoryMarshal.Write(alpha.Slice(x, 4), in mask);
            }
        }

        for (; x < pixels; x++)
        {
            color[x * 3] = rgba[x * 4];
            color[x * 3 + 1] = rgba[x * 4 + 1];
            color[x * 3 + 2] = rgba[x * 4 + 2];
            alpha[x] = rgba[x * 4 + 3];
        }
    }
}
