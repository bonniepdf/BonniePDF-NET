// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Svg.Fonts;
using BonniePDF.Rendering.Svg.Ir;
using BonniePDF.Rendering.Svg.Parsers;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg;

/// <summary>Thrown when a document uses features outside the supported profile.</summary>
/// <remarks>
/// Public, unlike the rest of the engine: it is what a caller of the <c>Svg</c> element
/// catches. Its message names options by what they do, not by any one API's spelling.
/// </remarks>
public sealed class SvgUnsupportedFeatureException : Exception
{
    /// <summary>Creates the exception from the features that were found.</summary>
    public SvgUnsupportedFeatureException(IReadOnlyList<UnsupportedFeature> features)
        : base(BuildMessage(features)) => Features = features;

    /// <summary>Gets the offending features, with the detail for each.</summary>
    public IReadOnlyList<UnsupportedFeature> Features { get; }

    private static string BuildMessage(IReadOnlyList<UnsupportedFeature> features)
    {
        string names = string.Join(", ", features.Select(entry => entry.Feature).Distinct(StringComparer.Ordinal));
        string detail = string.Join("; ", features.Select(entry => $"{entry.Feature}: {entry.Detail}"));
        return $"SVG uses features outside the supported profile: {names}. " +
               $"Turn off strict unsupported-feature checking to draw the rest without them, or register an SVG rasterizer to draw them as images. {detail}";
    }
}

/// <summary>Options for importing an SVG.</summary>
/// <param name="Width">Target box width; defaults to the document's own.</param>
/// <param name="Height">Target box height; defaults to the document's own.</param>
/// <param name="Strict">
/// Throw when the document uses a feature outside the supported profile. Null
/// means true, matching the <c>Svg</c> element's <c>StrictUnsupportedFeatures</c>
/// and the TypeScript side's default.
/// </param>
/// <param name="Fonts">
/// Fonts for <c>text</c>, which is imported as outlines. Without one, text is reported
/// as unsupported.
/// </param>
/// <param name="Rasterizer">
/// Renders a self-contained SVG document to PNG bytes, at the pixel size its
/// <c>width</c> and <c>height</c> give. When <see cref="Strict"/> is false, what the
/// profile cannot draw goes to it and is drawn as an image; without one, those parts
/// are skipped. Supplied by the caller - this package never rasterizes anything
/// itself, which is what keeps it free of dependencies. Only
/// <see cref="SvgImporter.ImportAsync"/> can call it.
/// </param>
/// <param name="RasterScale">
/// Raster pixels per unit of the target box; null means
/// <see cref="SvgImporter.DefaultRasterScale"/>.
/// </param>
/// <remarks>
/// <see cref="Strict"/> is nullable rather than a <c>bool</c> defaulting to true
/// because <c>default(ImportOptions)</c> zeroes every field: a <c>bool</c> would
/// silently make the default non-strict, which is the opposite of the documented
/// behaviour and of what TypeScript does. Null-means-true survives
/// <c>default</c>.
/// </remarks>
internal readonly record struct ImportOptions(
    double? Width = null,
    double? Height = null,
    bool? Strict = null,
    IFontProvider? Fonts = null,
    Func<string, Task<byte[]>>? Rasterizer = null,
    double? RasterScale = null)
{
    /// <summary>Gets whether to throw on an out-of-profile feature; null means true.</summary>
    public bool IsStrict => Strict ?? true;
}

/// <summary>Parses SVG and draws it into a sink.</summary>
internal static class SvgImporter
{
    /// <summary>Raster pixels per unit of the target box when <c>RasterScale</c> is not given.</summary>
    public const double DefaultRasterScale = 2.0;

    /// <summary>Parses SVG and draws it into a sink.</summary>
    /// <remarks>
    /// Synchronous, so it cannot call a rasterizer: passing one is an error rather than
    /// a silent skip. Use <see cref="ImportAsync"/>.
    /// </remarks>
    public static IrDocument Import(string source, IVectorSink sink, ImportOptions options = default)
    {
        if (options.Rasterizer is not null)
        {
            throw new InvalidOperationException("Import is synchronous and cannot call a rasterizer; use ImportAsync");
        }
        IrDocument document = Parse(source, options);
        Emitter.Emit(document, sink);
        return document;
    }

    /// <summary>
    /// Parses SVG and draws it into a sink, rasterizing what the profile cannot draw
    /// when a rasterizer is supplied and strict mode is off.
    /// </summary>
    /// <remarks>
    /// The rasterizer is called sequentially and in document order, so it sees a
    /// deterministic sequence, and once per distinct snippet.
    /// </remarks>
    public static async Task<IrDocument> ImportAsync(
        string source, IVectorSink sink, ImportOptions options = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sink);
        IrDocument document = Parse(source, options);
        var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (options.Rasterizer is { } rasterizer)
        {
            foreach (string svg in RasterSnippets(document.Children))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!images.ContainsKey(svg)) images[svg] = await rasterizer(svg).ConfigureAwait(false);
            }
        }
        Emitter.Emit(document, sink, images);
        return document;
    }

    /// <summary>Every raster snippet in the IR, in drawing order.</summary>
    public static IReadOnlyList<string> RasterSnippets(IReadOnlyList<IrNode> nodes)
    {
        var found = new List<string>();
        void Walk(IReadOnlyList<IrNode> list)
        {
            foreach (IrNode node in list)
            {
                switch (node)
                {
                    case IrRaster raster:
                        found.Add(raster.Svg);
                        break;
                    case IrGroup group:
                        Walk(group.Children);
                        break;
                    case IrClipGroup clip:
                        Walk(clip.Children);
                        break;
                }
            }
        }
        Walk(nodes);
        return found;
    }

    /// <summary>The same document fitted onto a different box.</summary>
    /// <remarks>
    /// Only the root transform depends on the box, so this equals parsing again for
    /// the new size, without parsing again - and keeps any rasterized images valid,
    /// since their regions are in user space. The <c>Svg</c> element uses it to
    /// rasterize before layout, when the final size is not yet known.
    /// </remarks>
    public static IrDocument Fit(IrDocument document, double width, double height) => document with
    {
        RootTransform = ViewBoxParser.Transform(document.Fit.UserBox, width, height, document.Fit.Preserve),
        Width = width,
        Height = height,
    };

    /// <summary>Parses SVG to the IR without drawing it - useful for inspection and tests.</summary>
    public static IrDocument Parse(string source, ImportOptions options = default)
    {
        bool rasterize = !options.IsStrict && options.Rasterizer is not null;
        IrDocument document = IrBuilder.Build(source, new BuildOptions(
            options.Width, options.Height, options.Fonts, rasterize ? options.RasterScale ?? DefaultRasterScale : null));
        if (options.IsStrict && document.Unsupported.Count > 0)
        {
            throw new SvgUnsupportedFeatureException(document.Unsupported);
        }

        return document;
    }
}
