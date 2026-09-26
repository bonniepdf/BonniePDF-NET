// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using BonniePDF.Rendering.Svg.Geometry;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>A parsed <c>viewBox</c>.</summary>
/// <param name="MinX">Left edge in user units.</param>
/// <param name="MinY">Top edge in user units.</param>
/// <param name="Width">Width in user units; always positive.</param>
/// <param name="Height">Height in user units; always positive.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct ViewBox(double MinX, double MinY, double Width, double Height);

/// <summary>How a viewBox is fitted to its viewport.</summary>
internal enum AspectScaling
{
    /// <summary>Fit the whole viewBox inside the viewport, letterboxing.</summary>
    Meet,

    /// <summary>Fill the viewport, cropping.</summary>
    Slice
}

/// <summary>A parsed <c>preserveAspectRatio</c>.</summary>
/// <param name="Align">The alignment keyword, or <c>none</c> to stretch.</param>
/// <param name="Scaling">Whether to meet or slice.</param>
internal readonly record struct PreserveAspectRatio(string Align, AspectScaling Scaling)
{
    /// <summary>The spec default, <c>xMidYMid meet</c>.</summary>
    public static readonly PreserveAspectRatio Default = new("xMidYMid", AspectScaling.Meet);
}

/// <summary>Parses viewBox attributes and computes the viewport transform.</summary>
internal static class ViewBoxParser
{
    private static readonly char[] Separators = [' ', ',', '\t', '\n', '\r', '\f'];
    private static readonly char[] Whitespace = [' ', '\t', '\n', '\r', '\f'];

    private static readonly string[] ValidAlign =
    [
        "none", "xMinYMin", "xMidYMin", "xMaxYMin",
        "xMinYMid", "xMidYMid", "xMaxYMid",
        "xMinYMax", "xMidYMax", "xMaxYMax"
    ];

    /// <summary>Parses a <c>viewBox</c>, or null when absent or unusable.</summary>
    public static ViewBox? ParseViewBox(string input)
    {
        string[] parts = input.Trim().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return null;

        var values = new double[4];
        for (int i = 0; i < 4; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])
                || !double.IsFinite(values[i]))
            {
                return null;
            }
        }

        // A zero or negative extent disables rendering per the spec; treat it as
        // absent so the caller falls back to the viewport.
        if (values[2] <= 0 || values[3] <= 0) return null;
        return new ViewBox(values[0], values[1], values[2], values[3]);
    }

    /// <summary>Parses a <c>preserveAspectRatio</c>, falling back to the spec default.</summary>
    public static PreserveAspectRatio ParsePreserveAspectRatio(string input)
    {
        string[] parts = input.Trim().Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
        // The optional `defer` prefix only applies to <image> and is ignored here.
        IEnumerable<string> tokens = parts.Length > 0 && string.Equals(parts[0], "defer", StringComparison.Ordinal) ? parts.Skip(1) : parts;
        string[] list = tokens.ToArray();

        string align = list.Length > 0 ? list[0] : "xMidYMid";
        AspectScaling scaling = list.Length > 1 && string.Equals(list[1], "slice", StringComparison.Ordinal) ? AspectScaling.Slice : AspectScaling.Meet;
        return new PreserveAspectRatio(ValidAlign.Contains(align) ? align : "xMidYMid", scaling);
    }

    /// <summary>
    /// The matrix mapping viewBox user space onto a target rectangle, per
    /// SVG 1.1 §7.8.
    /// </summary>
    public static Matrix Transform(ViewBox? viewBox, double targetWidth, double targetHeight, PreserveAspectRatio? preserve = null)
    {
        if (viewBox is not { } box) return Matrix.Identity;
        PreserveAspectRatio aspect = preserve ?? PreserveAspectRatio.Default;

        double scaleX = targetWidth / box.Width;
        double scaleY = targetHeight / box.Height;

        if (!string.Equals(aspect.Align, "none", StringComparison.Ordinal))
        {
            double uniform = aspect.Scaling == AspectScaling.Slice ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
            scaleX = uniform;
            scaleY = uniform;
        }

        double slackX = targetWidth - box.Width * scaleX;
        double slackY = targetHeight - box.Height * scaleY;

        double translateX = 0;
        double translateY = 0;
        if (!string.Equals(aspect.Align, "none", StringComparison.Ordinal))
        {
            if (aspect.Align.Contains("xMid", StringComparison.Ordinal)) translateX = slackX / 2;
            else if (aspect.Align.Contains("xMax", StringComparison.Ordinal)) translateX = slackX;

            if (aspect.Align.Contains("YMid", StringComparison.Ordinal)) translateY = slackY / 2;
            else if (aspect.Align.Contains("YMax", StringComparison.Ordinal)) translateY = slackY;
        }

        // Move the viewBox origin to zero, then scale, then place.
        return Matrix.Multiply(
            Matrix.Multiply(Matrix.Translation(translateX, translateY), Matrix.Scaling(scaleX, scaleY)),
            Matrix.Translation(-box.MinX, -box.MinY));
    }
}
