using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace BonniePDF.Rendering.Pdf;

/// <summary>One gradient stop, pre-resolved to straight RGBA components in [0, 1].</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ShadingStop(float Offset, float R, float G, float B, float A);

/// <summary>
/// A request to emit a native PDF gradient shading — axial (<c>/ShadingType 2</c>,
/// linear) or radial (<c>/ShadingType 3</c>).
/// </summary>
public sealed class ShadingRequest
{
    /// <summary><c>2</c> = axial (linear), <c>3</c> = radial.</summary>
    public int ShadingType { get; init; }

    /// <summary><c>[x0 y0 x1 y1]</c> (axial) or <c>[x0 y0 r0 x1 y1 r1]</c> (radial), in shading space.</summary>
    public float[] Coords { get; init; }

    /// <summary>Stops sorted by offset and padded so the first is at 0 and the last at 1.</summary>
    public IReadOnlyList<ShadingStop> Stops { get; init; }

    /// <summary>PDF <c>/Extend</c> — pad the start/end colour beyond the axis/radii (SVG "pad").</summary>
    public (bool Start, bool End) Extend { get; init; }

    /// <summary>Bounding box of the painted region in shading space, for the mask form.</summary>
    public float[] Bbox { get; init; }

    /// <summary>True when any stop is translucent, i.e. a soft mask is required.</summary>
    public bool HasAlpha { get; init; }
}

/// <summary>
/// Builds inline PDF gradient shading dictionaries — axial (<c>/ShadingType 2</c>,
/// linear) and radial (<c>/ShadingType 3</c>).
///
/// A gradient is emitted as a real shading dictionary whose colour is driven by a
/// PDF function built from the gradient stops. This is the analytic, single-layer
/// equivalent of SVG's <c>&lt;linearGradient&gt;</c> / <c>&lt;radialGradient&gt;</c> — the
/// viewer evaluates one exact blend per pixel, so it matches the SVG export instead
/// of the dimmer, viewer-dependent result of approximating the gradient with many
/// stacked translucent strips or discs (whose tiny per-layer alphas round toward
/// zero when a viewer composites them in 8-bit).
///
/// Transparency (stops with alpha &lt; 1) is handled by a parallel grayscale shading
/// used as a luminosity soft mask (see <c>PdfResourceRegistry</c>): the colour shading
/// carries the RGB, the alpha shading carries the opacity as a gray level
/// (white = opaque, black = transparent).
/// </summary>
public static class GradientShading
{
    /// <summary>Inline shading dictionary carrying the gradient's RGB colour.</summary>
    public static string BuildColorShadingDict(ShadingRequest req)
    {
        string fn = BuildStopFunction(req.Stops, s => new[] { s.R, s.G, s.B });
        return $"<< /ShadingType {req.ShadingType} /ColorSpace /DeviceRGB /Coords {Arr(req.Coords)} " +
               $"/Domain [0 1] /Extend [{Bool(req.Extend.Start)} {Bool(req.Extend.End)}] /Function {fn} >>";
    }

    /// <summary>
    /// Inline shading dictionary whose single grayscale channel encodes the gradient's
    /// alpha (white = opaque). Painted inside the luminosity soft-mask group so the
    /// colour shading is revealed in proportion to each stop's opacity.
    /// </summary>
    public static string BuildAlphaShadingDict(ShadingRequest req)
    {
        string fn = BuildStopFunction(req.Stops, s => new[] { s.A });
        return $"<< /ShadingType {req.ShadingType} /ColorSpace /DeviceGray /Coords {Arr(req.Coords)} " +
               $"/Domain [0 1] /Extend [{Bool(req.Extend.Start)} {Bool(req.Extend.End)}] /Function {fn} >>";
    }

    /// <summary>
    /// Builds an inline PDF function dict mapping the parametric domain [0, 1] to the
    /// stop components returned by <paramref name="components"/>. Uses a single Type-2
    /// (exponential) function for two stops, or a Type-3 stitching function across the
    /// segments.
    /// </summary>
    private static string BuildStopFunction(IReadOnlyList<ShadingStop> stops, Func<ShadingStop, float[]> components)
    {
        string Type2(ShadingStop a, ShadingStop b) =>
            $"<< /FunctionType 2 /Domain [0 1] /C0 {Arr(components(a))} /C1 {Arr(components(b))} /N 1 >>";

        if (stops.Count == 2) return Type2(stops[0], stops[1]);

        List<string> functions = new();
        List<float> bounds = new();
        List<float> encode = new();
        for (int i = 0; i < stops.Count - 1; i++)
        {
            functions.Add(Type2(stops[i], stops[i + 1]));
            encode.Add(0);
            encode.Add(1);
            if (i > 0) bounds.Add(stops[i].Offset);
        }
        return $"<< /FunctionType 3 /Domain [0 1] /Functions [{string.Join(' ', functions)}] " +
               $"/Bounds {Arr(bounds)} /Encode {Arr(encode)} >>";
    }

    private static string Bool(bool v) => v ? "true" : "false";

    private static string Arr(IEnumerable<float> values) => "[" + string.Join(' ', values.Select(Num)) + "]";

    /// <summary>Formats a number compactly for PDF output (no exponent, trimmed zeros).</summary>
    private static string Num(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return "0";
        string s = v.ToString("0.#####", CultureInfo.InvariantCulture);
        return s.Length == 0 ? "0" : s;
    }
}
