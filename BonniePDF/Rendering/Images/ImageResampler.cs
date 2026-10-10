using System;

namespace BonniePDF;

/// <summary>
/// Reduces an over-sampled image to the resolution its printed size can use.
/// Pure integer arithmetic throughout: the output is a function of the source
/// samples and the two sizes alone, so the same image reduces to the same bytes
/// in every implementation of BonniePDF and on every machine.
/// </summary>
internal static class ImageResampler
{
    /// <summary>Points-to-inches denominator, in milli-points: 72 pt per inch x 1000.</summary>
    private const long MilliPointsPerInch = 72000;

    /// <summary>
    /// The pixel length that keeps a drawn length at or under <paramref name="dpi"/> pixels
    /// per inch, never above the source and never below one pixel.
    /// </summary>
    internal static int TargetLength(long drawnMilliPoints, int dpi, int sourceLength)
    {
        long target = Math.Max(1L, drawnMilliPoints * dpi / MilliPointsPerInch);
        return (int)Math.Min(sourceLength, target);
    }

    /// <summary>
    /// True when the source carries at least 1.5 times the target's pixels. Below that the
    /// saving is under a third of the raw pixels and the pixels are left alone. Decided on
    /// pixel counts only, never on compressed size, which differs between implementations.
    /// </summary>
    internal static bool ShouldResample(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight) =>
        2L * sourceWidth * sourceHeight >= 3L * targetWidth * targetHeight;

    /// <summary>
    /// Whether the image is of a kind the resampler handles exactly: an 8-bit grey or RGB PNG,
    /// with or without alpha. JPEG, indexed and 16-bit images are passed through unchanged.
    /// Returns a short reason when it is not, for the diagnostic.
    /// </summary>
    internal static bool IsEligible(ImageInfo image, out string reason)
    {
        if (image.Format != ImageFormat.Png) { reason = "not a PNG"; return false; }
        if (image.BitsPerComponent != 8) { reason = $"{image.BitsPerComponent}-bit samples"; return false; }
        if (image.ColorSpace is not (ImageColorSpace.DeviceGray or ImageColorSpace.DeviceRGB))
        {
            reason = "indexed or CMYK colour"; return false;
        }
        long pixels = (long)image.Width * image.Height;
        if (image.Data.Length != pixels * image.Components ||
            (image.HasAlpha && image.AlphaData.Length != pixels))
        {
            reason = "sample data does not match the declared size"; return false;
        }
        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Returns a copy of <paramref name="source"/> at the target size. Everything other than the
    /// pixels - colour space, ICC profile, orientation - is carried over unchanged.
    /// </summary>
    internal static ImageInfo Resample(ImageInfo source, int targetWidth, int targetHeight)
    {
        using var performanceScope = PerformanceEvents.Measure(PerformanceStage.ImageResizing);
        byte[] color = Resample(source.Data, source.HasAlpha ? source.AlphaData : null,
            source.Width, source.Height, source.Components, targetWidth, targetHeight, out byte[]? alpha);

        return new ImageInfo
        {
            Data = color,
            AlphaData = alpha ?? [],
            // The original file bytes describe the source pixels, not these.
            OriginalData = [],
            Width = targetWidth,
            Height = targetHeight,
            Format = source.Format,
            ColorSpace = source.ColorSpace,
            BitsPerComponent = source.BitsPerComponent,
            Components = source.Components,
            HasAlpha = source.HasAlpha,
            Palette = source.Palette,
            IccProfile = source.IccProfile,
            InvertCmykColors = source.InvertCmykColors,
            Orientation = source.Orientation,
        };
    }

    /// <summary>
    /// The exact area average. <paramref name="color"/> holds <paramref name="components"/>
    /// interleaved samples per pixel; <paramref name="alpha"/>, if present, is a separate plane.
    /// With alpha the average is premultiplied: a transparent pixel contributes nothing to the
    /// colour of its neighbours, and a colour with no visible contribution is 0.
    /// </summary>
    internal static byte[] Resample(byte[] color, byte[]? alpha, int sourceWidth, int sourceHeight,
        int components, int targetWidth, int targetHeight, out byte[]? alphaOut)
    {
        // Constant opaque alpha cancels out of the premultiplied average exactly. Keep the
        // alpha plane (and therefore the PDF representation), but avoid weighting it per pixel.
        if (alpha != null && alpha.AsSpan().IndexOfAnyExcept((byte)255) < 0)
        {
            byte[] opaqueColor = Resample(color, null, sourceWidth, sourceHeight, components,
                targetWidth, targetHeight, out _);
            alphaOut = new byte[checked(targetWidth * targetHeight)];
            alphaOut.AsSpan().Fill(255);
            return opaqueColor;
        }

        int cc = components;
        byte[] outColor = new byte[checked(targetWidth * targetHeight * cc)];
        alphaOut = alpha == null ? null : new byte[targetWidth * targetHeight];

        (int[] xFirst, int[] xCount, int[] xOffset, int[] xWeight) = Overlaps(sourceWidth, targetWidth);
        (int[] yFirst, int[] yCount, int[] yOffset, int[] yWeight) = Overlaps(sourceHeight, targetHeight);
        long total = (long)sourceWidth * sourceHeight;

        // Per destination row: the vertical accumulation of the horizontally weighted rows.
        long[] accColor = new long[targetWidth * cc];
        long[] accAlpha = alpha == null ? [] : new long[targetWidth];
        long[] rowColor = new long[targetWidth * cc];
        long[] rowAlpha = alpha == null ? [] : new long[targetWidth];

        // A source row on the boundary of two destination rows is weighted horizontally once, not twice.
        int rowInBuffer = -1;

        for (int y = 0; y < targetHeight; y++)
        {
            Array.Clear(accColor);
            Array.Clear(accAlpha);

            for (int k = 0; k < yCount[y]; k++)
            {
                int sy = yFirst[y] + k;
                long oy = yWeight[yOffset[y] + k];

                if (sy != rowInBuffer)
                {
                    HorizontalRow(color, alpha, sy * sourceWidth, cc, targetWidth, xFirst, xCount, xOffset, xWeight,
                        rowColor, rowAlpha);
                    rowInBuffer = sy;
                }

                for (int i = 0; i < accColor.Length; i++) accColor[i] += oy * rowColor[i];
                for (int i = 0; i < accAlpha.Length; i++) accAlpha[i] += oy * rowAlpha[i];
            }

            for (int x = 0; x < targetWidth; x++)
            {
                int o = y * targetWidth + x;
                if (alpha == null)
                {
                    for (int c = 0; c < cc; c++)
                        outColor[o * cc + c] = (byte)RoundHalfUp(accColor[x * cc + c], total);
                }
                else
                {
                    long a = accAlpha[x];
                    alphaOut![o] = (byte)RoundHalfUp(a, total);
                    for (int c = 0; c < cc; c++)
                        outColor[o * cc + c] = a == 0 ? (byte)0 : (byte)RoundHalfUp(accColor[x * cc + c], a);
                }
            }
        }

        return outColor;
    }

    /// <summary>
    /// Weights one source row horizontally into <paramref name="rowColor"/> (and <paramref name="rowAlpha"/>):
    /// each destination column is the sum of the source samples it overlaps times their overlap. With alpha
    /// the colour is weighted by alpha as well, which makes the average premultiplied.
    /// </summary>
    private static void HorizontalRow(byte[] color, byte[]? alpha, int rowStart, int cc, int targetWidth,
        int[] xFirst, int[] xCount, int[] xOffset, int[] xWeight, long[] rowColor, long[] rowAlpha)
    {
        if (alpha == null)
        {
            if (cc == 3)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    long r = 0, g = 0, b = 0;
                    int p = (rowStart + xFirst[x]) * 3;
                    int w = xOffset[x];
                    for (int end = w + xCount[x]; w < end; w++, p += 3)
                    {
                        long ox = xWeight[w];
                        r += ox * color[p];
                        g += ox * color[p + 1];
                        b += ox * color[p + 2];
                    }

                    rowColor[x * 3] = r;
                    rowColor[x * 3 + 1] = g;
                    rowColor[x * 3 + 2] = b;
                }

                return;
            }

            for (int x = 0; x < targetWidth; x++)
            {
                for (int c = 0; c < cc; c++) rowColor[x * cc + c] = 0;
                int p = (rowStart + xFirst[x]) * cc;
                int w = xOffset[x];
                for (int end = w + xCount[x]; w < end; w++, p += cc)
                {
                    long ox = xWeight[w];
                    for (int c = 0; c < cc; c++) rowColor[x * cc + c] += ox * color[p + c];
                }
            }

            return;
        }

        for (int x = 0; x < targetWidth; x++)
        {
            long a = 0;
            int p = rowStart + xFirst[x];
            int w = xOffset[x];
            if (cc == 3)
            {
                long r = 0, g = 0, b = 0;
                for (int end = w + xCount[x]; w < end; w++, p++)
                {
                    long oa = xWeight[w] * alpha[p];
                    a += oa;
                    r += oa * color[p * 3];
                    g += oa * color[p * 3 + 1];
                    b += oa * color[p * 3 + 2];
                }

                rowColor[x * 3] = r;
                rowColor[x * 3 + 1] = g;
                rowColor[x * 3 + 2] = b;
            }
            else
            {
                for (int c = 0; c < cc; c++) rowColor[x * cc + c] = 0;
                for (int end = w + xCount[x]; w < end; w++, p++)
                {
                    long oa = xWeight[w] * alpha[p];
                    a += oa;
                    for (int c = 0; c < cc; c++) rowColor[x * cc + c] += oa * color[p * cc + c];
                }
            }

            rowAlpha[x] = a;
        }
    }

    /// <summary>round(n / d) with halves rounding up, for non-negative n and positive d.</summary>
    private static long RoundHalfUp(long n, long d) => (2 * n + d) / (2 * d);

    /// <summary>
    /// For each target index d, the first source index that overlaps it, how many source
    /// indices do, and the integer overlap weight of each. Output pixel d spans
    /// [d*S, (d+1)*S) and source pixel i spans [i*T, (i+1)*T), both in units of 1/T of a
    /// source pixel, so the weights of one target index sum to S.
    /// </summary>
    private static (int[] First, int[] Count, int[] Offset, int[] Weights) Overlaps(int sourceLength, int targetLength)
    {
        long s = sourceLength, t = targetLength;
        int[] first = new int[targetLength];
        int[] count = new int[targetLength];
        int[] offset = new int[targetLength];
        var weights = new System.Collections.Generic.List<int>(sourceLength + targetLength);

        for (int d = 0; d < targetLength; d++)
        {
            long lo = d * s, hi = (d + 1) * s;
            int i0 = (int)(lo / t);
            int i1 = (int)((hi + t - 1) / t) - 1;
            first[d] = i0;
            count[d] = i1 - i0 + 1;
            offset[d] = weights.Count;
            for (int i = i0; i <= i1; i++)
            {
                long overlap = Math.Min(hi, (i + 1) * t) - Math.Max(lo, i * t);
                weights.Add((int)overlap);
            }
        }

        return (first, count, offset, weights.ToArray());
    }
}
