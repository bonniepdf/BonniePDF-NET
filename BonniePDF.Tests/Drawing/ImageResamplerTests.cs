using Xunit;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// The image resampler and the target-size rule, against the shared conformance data for
/// image resolution capping. The pixel cases are pure functions of (source samples, sizes):
/// no layout and no PDF. Mirrors core/tests/drawing/image-resampler.test.ts.
/// </summary>
public class ImageResamplerTests
{
    public static TheoryData<string, int, int, long, long, int, int, int, bool> TargetDimensionCases => new()
    {
        { "the invoice logo, 175pt wide at the 288 default", 2658, 512, 175000L, 33708L, 288, 700, 134, true },
        { "a small image already below the cap is not upscaled", 100, 50, 200000L, 100000L, 288, 100, 50, false },
        { "exactly on the cap: 72pt at 300 dpi is 300px", 300, 300, 72000L, 72000L, 300, 300, 300, false },
        { "a 12-megapixel photo in a 3in x 2in box at 288", 4000, 3000, 216000L, 144000L, 288, 864, 576, true },
        { "a sliver: the drawn size rounds down to a floor of 1px", 5000, 5000, 100L, 100L, 288, 1, 1, true },
        { "a 1.44x pixel oversample is inside the 1.5x tolerance, so it is left alone", 1200, 960, 250000L, 200000L, 288, 1000, 800, false },
        { "a 1.56x pixel oversample is past the tolerance, so it is resampled", 1250, 1000, 250000L, 200000L, 288, 1000, 800, true },
        { "anisotropic fill: each axis is capped on its own", 2000, 400, 100000L, 100000L, 288, 400, 400, true },
    };

    [Theory]
    [MemberData(nameof(TargetDimensionCases))]
    public void TargetSize_FollowsTheRule(string id, int sourceWidth, int sourceHeight, long drawnWidthMilliPt,
        long drawnHeightMilliPt, int dpi, int targetWidth, int targetHeight, bool resample)
    {
        int tw = ImageResampler.TargetLength(drawnWidthMilliPt, dpi, sourceWidth);
        int th = ImageResampler.TargetLength(drawnHeightMilliPt, dpi, sourceHeight);

        Assert.True(tw == targetWidth && th == targetHeight, $"{id}: expected {targetWidth}x{targetHeight}, got {tw}x{th}");
        Assert.Equal(resample, ImageResampler.ShouldResample(sourceWidth, sourceHeight, tw, th));
    }

    public static TheoryData<string, int, int, int, int, int, byte[], byte[]> PixelCases => new()
    {
        { "grey-4x4-to-2x2-exact", 4, 4, 1, 2, 2,
          new byte[] { 0, 60, 120, 180, 17, 77, 137, 197, 34, 94, 154, 214, 51, 111, 171, 231 },
          new byte[] { 39, 159, 73, 193 } },
        { "grey-5x3-to-2x2-fractional", 5, 3, 1, 2, 2,
          new byte[] { 0, 50, 100, 150, 200, 71, 121, 171, 221, 15, 142, 192, 242, 36, 86 },
          new byte[] { 64, 150, 158, 108 } },
        { "grey-7x7-to-3x2-anisotropic", 7, 7, 1, 3, 2,
          new byte[] { 0, 37, 74, 111, 148, 185, 222, 91, 128, 165, 202, 239, 20, 57, 182, 219, 0, 37, 74, 111, 148, 17, 54, 91, 128, 165, 202, 239, 108, 145, 182, 219, 0, 37, 74, 199, 236, 17, 54, 91, 128, 165, 34, 71, 108, 145, 182, 219, 0 },
          new byte[] { 96, 118, 140, 116, 117, 118 } },
        { "rgb-6x4-to-4x3", 6, 4, 3, 4, 3,
          new byte[] { 0, 97, 194, 41, 138, 235, 82, 179, 20, 123, 220, 61, 164, 5, 102, 205, 46, 143, 23, 120, 217, 64, 161, 2, 105, 202, 43, 146, 243, 84, 187, 28, 125, 228, 69, 166, 46, 143, 240, 87, 184, 25, 128, 225, 66, 169, 10, 107, 210, 51, 148, 251, 92, 189, 69, 166, 7, 110, 207, 48, 151, 248, 89, 192, 33, 130, 233, 74, 171, 18, 115, 212 },
          new byte[] { 19, 116, 192, 74, 171, 76, 142, 154, 80, 197, 38, 135, 48, 145, 157, 103, 200, 41, 171, 98, 109, 226, 67, 164, 77, 174, 58, 132, 229, 70, 200, 41, 138, 127, 96, 193 } },
        { "grey-alpha-4x4-to-2x2", 4, 4, 2, 2, 2,
          new byte[] { 9, 0, 79, 85, 149, 170, 219, 0, 9, 85, 79, 170, 149, 0, 219, 0, 9, 170, 79, 0, 149, 0, 219, 85, 9, 0, 79, 0, 149, 85, 219, 0 },
          new byte[] { 62, 85, 149, 43, 9, 43, 184, 43 } },
        { "rgba-5x5-to-2x2-premultiplied", 5, 5, 4, 2, 2,
          new byte[] { 200, 140, 80, 0, 205, 145, 85, 0, 210, 150, 90, 60, 215, 155, 95, 60, 220, 160, 100, 60, 200, 140, 80, 0, 205, 145, 85, 0, 210, 150, 90, 100, 215, 155, 95, 100, 220, 160, 100, 100, 200, 140, 80, 0, 205, 145, 85, 0, 210, 150, 90, 140, 215, 155, 95, 140, 220, 160, 100, 140, 200, 140, 80, 0, 205, 145, 85, 0, 210, 150, 90, 180, 215, 155, 95, 180, 220, 160, 100, 180, 200, 140, 80, 0, 205, 145, 85, 0, 210, 150, 90, 220, 215, 155, 95, 220, 220, 160, 100, 220 },
          new byte[] { 210, 150, 90, 18, 216, 156, 96, 92, 210, 150, 90, 38, 216, 156, 96, 188 } },
        { "rgba-3x3-to-1x1-transparent-block", 3, 3, 4, 1, 1,
          new byte[] { 255, 128, 0, 0, 255, 128, 0, 0, 255, 128, 0, 0, 255, 128, 0, 0, 255, 128, 0, 255, 255, 128, 0, 0, 255, 128, 0, 0, 255, 128, 0, 0, 255, 128, 0, 0 },
          new byte[] { 255, 128, 0, 28 } },
        { "rgba-8x2-to-3x1", 8, 2, 4, 3, 1,
          new byte[] { 0, 43, 86, 255, 29, 72, 115, 224, 58, 101, 144, 193, 87, 130, 173, 162, 116, 159, 202, 131, 145, 188, 231, 100, 174, 217, 4, 69, 203, 246, 33, 38, 61, 104, 147, 246, 90, 133, 176, 215, 119, 162, 205, 184, 148, 191, 234, 153, 177, 220, 7, 122, 206, 249, 36, 91, 235, 22, 65, 60, 8, 51, 94, 29 },
          new byte[] { 53, 96, 139, 223, 126, 169, 161, 142, 174, 169, 80, 61 } },
        { "rgba-6x4-opaque-to-4x3", 6, 4, 4, 4, 3,
          new byte[] { 0, 97, 194, 255, 41, 138, 235, 255, 82, 179, 20, 255, 123, 220, 61, 255, 164, 5, 102, 255, 205, 46, 143, 255, 23, 120, 217, 255, 64, 161, 2, 255, 105, 202, 43, 255, 146, 243, 84, 255, 187, 28, 125, 255, 228, 69, 166, 255, 46, 143, 240, 255, 87, 184, 25, 255, 128, 225, 66, 255, 169, 10, 107, 255, 210, 51, 148, 255, 251, 92, 189, 255, 69, 166, 7, 255, 110, 207, 48, 255, 151, 248, 89, 255, 192, 33, 130, 255, 233, 74, 171, 255, 18, 115, 212, 255 },
          new byte[] { 19, 116, 192, 255, 74, 171, 76, 255, 142, 154, 80, 255, 197, 38, 135, 255, 48, 145, 157, 255, 103, 200, 41, 255, 171, 98, 109, 255, 226, 67, 164, 255, 77, 174, 58, 255, 132, 229, 70, 255, 200, 41, 138, 255, 127, 96, 193, 255 } },
        { "rgba-2x1-last-alpha-254-to-1x1", 2, 1, 4, 1, 1,
          new byte[] { 0, 0, 0, 255, 255, 255, 255, 254 },
          new byte[] { 127, 127, 127, 255 } },
    };

    [Theory]
    [MemberData(nameof(PixelCases))]
    public void Resample_MatchesTheExactAreaAverage(string id, int sw, int sh, int channels, int tw, int th,
        byte[] source, byte[] expect)
    {
        bool hasAlpha = channels is 2 or 4;
        int cc = hasAlpha ? channels - 1 : channels;

        // The fixture interleaves alpha as the last channel; the resampler takes it as its own plane.
        byte[] color = new byte[sw * sh * cc];
        byte[]? alpha = hasAlpha ? new byte[sw * sh] : null;
        for (int p = 0; p < sw * sh; p++)
        {
            for (int c = 0; c < cc; c++) color[p * cc + c] = source[p * channels + c];
            if (hasAlpha) alpha![p] = source[p * channels + cc];
        }

        byte[] outColor = ImageResampler.Resample(color, alpha, sw, sh, cc, tw, th, out byte[]? outAlpha);

        byte[] actual = new byte[tw * th * channels];
        for (int p = 0; p < tw * th; p++)
        {
            for (int c = 0; c < cc; c++) actual[p * channels + c] = outColor[p * cc + c];
            if (hasAlpha) actual[p * channels + cc] = outAlpha![p];
        }

        Assert.True(expect.SequenceEqual(actual), $"{id}: expected [{string.Join(',', expect)}], got [{string.Join(',', actual)}]");
    }
}
