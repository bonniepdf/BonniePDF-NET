using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Drawing;
using BonniePDF.Rendering.Svg;
using BonniePDF.Rendering.Svg.Ir;

namespace BonniePDF;

/// <summary>
/// Draws an SVG document as vector content: paths, fills, strokes, gradients, clips
/// and text drawn as glyph outlines, through the same canvas as every other element.
/// </summary>
/// <remarks>
/// <para>
/// The element sizes its box exactly as <see cref="Image"/> does, from
/// <see cref="Width"/>, <see cref="Height"/> and the document's intrinsic size under
/// <see cref="FitMode"/>; the document's own <c>viewBox</c> and
/// <c>preserveAspectRatio</c> then place its content inside that box, as a browser's
/// <c>&lt;img&gt;</c> does.
/// </para>
/// <para>
/// Text uses the fonts registered with BonniePDF, resolved as for a paragraph.
/// Features outside the supported profile (filters, masks, patterns, stylesheets, ...)
/// fail generation by default; see <see cref="StrictUnsupportedFeatures"/>.
/// </para>
/// </remarks>
public class Svg : ElementBase, IRoleProvider, IAltTextProvider
{
    /// <summary>Points per SVG user unit: user units are CSS pixels, 96 to the inch.</summary>
    private const double PtPerPx = 0.75;

    private Parsed? _parsed;
    private (string Path, string Markup)? _file;
    private (float Width, float Height, string Markup)? _generated;
    private string _altText = string.Empty;

    /// <summary>SVG markup (mutually exclusive with <see cref="FilePath"/> and <see cref="SvgGenerator"/>).</summary>
    public string SvgData { get; set; } = string.Empty;

    /// <summary>Path to an SVG file (mutually exclusive with <see cref="SvgData"/> and <see cref="SvgGenerator"/>).</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Produces the markup for the box it is given, in points (mutually exclusive with
    /// <see cref="SvgData"/> and <see cref="FilePath"/>). The box cannot come from
    /// markup that does not exist yet, so a generator needs both <see cref="Width"/>
    /// and <see cref="Height"/>.
    /// </summary>
    public Func<PdfSize, string>? SvgGenerator { get; set; }

    /// <summary>Desired display width in points (null = from the document).</summary>
    public float? Width { get; set; }

    /// <summary>Desired display height in points (null = from the document).</summary>
    public float? Height { get; set; }

    /// <summary>How the drawing fills its box, as for <see cref="Image"/>.</summary>
    public ImageFitMode FitMode { get; set; } = ImageFitMode.Contain;

    /// <summary>
    /// Alternate text for accessibility. Under PDF/UA-1 it becomes the figure's
    /// <c>/Alt</c>. When not set, reads as the document's own <c>&lt;title&gt;</c>,
    /// which is used in its place.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public string AltText
    {
        get => _altText.Length > 0 ? _altText : DocumentTitle();
        set => _altText = value ?? string.Empty;
    }

    /// <summary>When true, the drawing is decorative and tagged as an artifact, with no alt-text finding.</summary>
    public bool IsDecorative { get; set; }

    /// <summary>
    /// When true (the default), a document using a feature outside the supported
    /// profile fails generation, naming every such feature. When false, the rest is
    /// drawn and each skipped feature is a diagnostic - or, with
    /// <see cref="DocumentSettings.SvgRasterizer"/> set and an async generate path,
    /// drawn as an image in its place.
    /// </summary>
    public bool StrictUnsupportedFeatures { get; set; } = true;

    /// <summary>
    /// The structure role in a tagged PDF: <c>Figure</c> when the element has alternative text, otherwise
    /// <c>Artifact</c>. It is always <c>Artifact</c> when <see cref="IsDecorative"/> is set.
    /// </summary>
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText) ? SemanticRole.Artifact : SemanticRole.Figure;

    /// <summary>
    /// Parses the document ahead of layout, rasterizing what the profile cannot draw
    /// when a rasterizer is given and <see cref="StrictUnsupportedFeatures"/> is off.
    /// The async generate paths call this; there is no need to call it yourself.
    /// </summary>
    internal async Task PrepareAsync(Func<string, Task<byte[]>>? rasterizer, CancellationToken cancellationToken)
    {
        Parsed parsed = Parse();
        if (rasterizer is null || StrictUnsupportedFeatures || parsed.Document.Unsupported.Count == 0) return;
        if (parsed.Images is not null) return;

        // Rasterize at the size the element will most likely be drawn: the box is not
        // final until layout, but the IR is re-fitted to it then.
        (float Width, float Height)? box = ProvisionalBox(parsed.Document);
        IrDocument document = SvgImporter.Parse(parsed.Source, new ImportOptions(
            Width: box is { } b ? b.Width / PtPerPx : null,
            Height: box is { } c ? c.Height / PtPerPx : null,
            Strict: false,
            Fonts: new BonniePdfFontProvider(),
            Rasterizer: rasterizer));
        var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string svg in SvgImporter.RasterSnippets(document.Children))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!images.ContainsKey(svg)) images[svg] = await rasterizer(svg).ConfigureAwait(false);
        }

        _parsed = parsed with { Document = document, Images = images };
    }

    /// <summary>
    /// The synchronous generate paths call this before layout. A document that the
    /// registered rasterizer would draw differently fails here rather than being drawn
    /// without it.
    /// </summary>
    internal void Prepare(bool rasterizerRegistered)
    {
        Parsed parsed = Parse();
        if (rasterizerRegistered && !StrictUnsupportedFeatures && parsed.Images is null && parsed.Document.Unsupported.Count > 0)
        {
            throw new InvalidOperationException(
                "This SVG uses features outside the supported profile, which the registered SVG rasterizer draws as images, " +
                "and only the async generate path can call it. Use GenerateBytesAsync() or SaveAsync().");
        }
    }

    /// <inheritdoc />
    protected override float GetPreferredContentWidth()
    {
        (float Width, float Height)? native = NativeSize();
        if (Width.HasValue) return Width.Value;
        if (native is not { } n) return 0f;
        if (Height.HasValue) return Height.Value * (n.Width / n.Height);
        return n.Width;
    }

    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) => ResolveBox(contentWidth)?.Height ?? 0f;

    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        if (ResolveBox(contentWidth) is not { } box)
        {
            ctx.Resources.Diagnostics.AddError(
                DiagnosticCategory.Render,
                "The SVG has no intrinsic size (no width, height or viewBox), so it needs both width and height; it was not drawn.",
                "svg");
            return;
        }

        Parsed parsed = Parse();
        foreach (UnsupportedFeature entry in parsed.Document.Unsupported)
        {
            ctx.Resources.Diagnostics.AddWarning(
                DiagnosticCategory.Render, $"SVG feature not drawn: {entry.Feature}: {entry.Detail}", "svg");
        }

        double widthPx = box.Width / PtPerPx;
        double heightPx = box.Height / PtPerPx;
        IrDocument document = SvgImporter.Fit(parsed.Document, widthPx, heightPx);
        var sink = new BonniePdfCanvasSink(ctx.Canvas, parsed.Decode);

        // Drawn in CSS px scaled onto points, so a document with no viewBox, whose user
        // units are px, is drawn at 0.75 pt per unit too. Clipped to its box, as a
        // browser clips an SVG to its viewport.
        ICanvas canvas = ctx.Canvas;
        canvas.SaveState();
        canvas.Translate(ctx.CurrentX, ctx.CurrentY);
        canvas.Scale((float)PtPerPx, (float)PtPerPx);
        canvas.BeginPath();
        canvas.MoveTo(0f, 0f);
        canvas.LineTo((float)widthPx, 0f);
        canvas.LineTo((float)widthPx, (float)heightPx);
        canvas.LineTo(0f, (float)heightPx);
        canvas.ClosePath();
        canvas.ClipToCurrentPath();
        Emitter.Emit(document, sink, parsed.Images);
        canvas.RestoreState();

        ctx.CurrentY += box.Height;
    }

    /// <summary>The display box in points, or null when there is nothing to size it from.</summary>
    private (float Width, float Height)? ResolveBox(float availableWidth)
    {
        if (SvgGenerator is not null) return GeneratorBox();
        if (NativeSize() is not { } native)
        {
            return Width.HasValue && Height.HasValue ? (Width.Value, Height.Value) : null;
        }

        return ImageBox.Resolve(Width, Height, FitMode, true, native.Width, native.Height, availableWidth);
    }

    /// <summary>The box to rasterize at before layout: the final one, unless it depends on the available width.</summary>
    private (float Width, float Height)? ProvisionalBox(IrDocument document)
    {
        if (SvgGenerator is not null) return GeneratorBox();
        if (IntrinsicSize(document) is not { } native) return ResolveBox(0f);
        return ImageBox.Resolve(Width, Height, FitMode, true, native.Width, native.Height, native.Width);
    }

    private (float Width, float Height)? GeneratorBox() =>
        Width.HasValue && Height.HasValue ? (Width.Value, Height.Value) : null;

    /// <summary>Intrinsic size in points, or null when the document declares none.</summary>
    private (float Width, float Height)? NativeSize() =>
        SvgGenerator is not null ? GeneratorBox() : IntrinsicSize(Parse().Document);

    private string DocumentTitle()
    {
        try
        {
            return Parse().Document.Title ?? string.Empty;
        }
#pragma warning disable CA1031 // Unreadable markup has no title; the error surfaces when it is drawn.
        catch (Exception)
#pragma warning restore CA1031
        {
            return string.Empty;
        }
    }

    /// <summary>Parses the source, once per distinct source, and enforces strictness.</summary>
    private Parsed Parse()
    {
        string source = Source();
        bool strict = StrictUnsupportedFeatures;
        if (_parsed is null || !string.Equals(_parsed.Source, source, StringComparison.Ordinal) || _parsed.Strict != strict)
        {
            IrDocument document = SvgImporter.Parse(source, new ImportOptions(Strict: false, Fonts: new BonniePdfFontProvider()));
            _parsed = new Parsed(source, strict, document, null);
        }

        if (strict && _parsed.Document.Unsupported.Count > 0)
        {
            throw new SvgUnsupportedFeatureException(_parsed.Document.Unsupported);
        }

        return _parsed;
    }

    private string Source()
    {
        int given = (SvgData.Length > 0 ? 1 : 0) + (FilePath.Length > 0 ? 1 : 0) + (SvgGenerator is not null ? 1 : 0);
        if (given > 1) throw new InvalidOperationException("Set only one of SvgData, FilePath and SvgGenerator");

        if (SvgData.Length > 0) return SvgData;
        if (FilePath.Length > 0)
        {
            if (_file is not { } file || !string.Equals(file.Path, FilePath, StringComparison.Ordinal))
            {
                file = (FilePath, File.ReadAllText(FilePath, Encoding.UTF8));
                _file = file;
            }

            return file.Markup;
        }

        if (SvgGenerator is not null)
        {
            if (GeneratorBox() is not { } size) throw new InvalidOperationException("An SVG generator needs both Width and Height");
            if (_generated is not { } generated || generated.Width != size.Width || generated.Height != size.Height)
            {
                generated = (size.Width, size.Height, SvgGenerator(new PdfSize(size.Width, size.Height)));
                _generated = generated;
            }

            return generated.Markup;
        }

        throw new InvalidOperationException("Either SvgData, FilePath or SvgGenerator must be specified");
    }

    private static (float Width, float Height)? IntrinsicSize(IrDocument document) =>
        document.Fit is { IntrinsicWidth: { } width, IntrinsicHeight: { } height }
            ? ((float)(width * PtPerPx), (float)(height * PtPerPx))
            : null;

    /// <summary>A parsed document, and what drawing it needs.</summary>
    private sealed record Parsed(string Source, bool Strict, IrDocument Document, IReadOnlyDictionary<string, byte[]>? Images)
    {
        private readonly Dictionary<byte[], ImageInfo> _decoded = new(ReferenceEqualityComparer.Instance);

        /// <summary>Decodes a rasterized image once, however often the element is drawn.</summary>
        public ImageInfo Decode(byte[] png)
        {
            if (!_decoded.TryGetValue(png, out ImageInfo? info))
            {
                info = ImageLoader.Load(png);
                _decoded[png] = info;
            }

            return info;
        }
    }
}
