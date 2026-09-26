// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Rendering.Svg.Geometry;
using BonniePDF.Rendering.Svg.Ir;
using BonniePDF.Rendering.Svg.Parsers;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg;

/// <summary>
/// Walks the IR and drives a sink.
/// </summary>
/// <remarks>
/// No SVG concepts appear here: by this point everything is paths, matrices and
/// resolved paint. That is the purpose of the IR, and it is what lets one emitter
/// serve a PDF canvas, an SVG writer, or a test double.
/// </remarks>
internal static class Emitter
{
    /// <summary>
    /// The one place the IR's <c>double</c> arithmetic narrows to the
    /// <c>float</c> the sink takes.
    /// </summary>
    /// <remarks>
    /// Everything upstream computes in double so that every arithmetic step and
    /// every discrete decision matches the TypeScript implementation exactly;
    /// <c>IVectorSink</c> mirrors <c>ICanvas</c>, which is <c>float</c> in C#, so
    /// the narrowing has to happen somewhere. Keeping it to this single function
    /// means the loss is applied once, at the boundary, rather than accumulating
    /// through the parsers - which is what previously flipped an arc's segment
    /// count and broke byte parity. See AGENTS.md.
    /// </remarks>
    private static float Narrow(double value) => (float)value;
    /// <summary>Draws a parsed document into a sink.</summary>
    /// <param name="document">The document.</param>
    /// <param name="sink">What to draw into.</param>
    /// <param name="images">
    /// Rasterized images by snippet. A raster node with no image draws nothing; the
    /// builder has already listed its feature under <c>Rasterized</c>.
    /// </param>
    public static void Emit(IrDocument document, IVectorSink sink, IReadOnlyDictionary<string, byte[]>? images = null)
    {
        foreach (IrNode child in document.Children) EmitNode(child, document.RootTransform, sink, images);
    }

    private static void EmitNode(IrNode node, Matrix parentTransform, IVectorSink sink, IReadOnlyDictionary<string, byte[]>? images)
    {
        if (node is IrRaster raster)
        {
            // The image covers the document's viewport in root user space;
            // everything that places the element within it is inside the snippet.
            if (images is null || !images.TryGetValue(raster.Svg, out byte[]? png)) return;
            sink.SaveState();
            if (!parentTransform.IsIdentity)
            {
                sink.Transform(
                    Narrow(parentTransform.A), Narrow(parentTransform.B), Narrow(parentTransform.C),
                    Narrow(parentTransform.D), Narrow(parentTransform.E), Narrow(parentTransform.F));
            }
            sink.DrawImage(new SinkImage(png), Narrow(raster.X), Narrow(raster.Y), Narrow(raster.Width), Narrow(raster.Height));
            sink.RestoreState();
            return;
        }

        if (node is IrClipGroup clipGroup)
        {
            sink.SaveState();
            foreach (IrClip clip in clipGroup.Clips)
            {
                // The clip's coordinates are flattened into the emitting space
                // rather than applied via a sink transform: a transform and a clip
                // share one graphics state, so the transform could not be undone
                // without losing the clip. Flattening is safe here in a way it is
                // not for paths, because a clip has no stroke width to be scaled.
                Matrix matrix = Matrix.Multiply(parentTransform, clip.Transform);
                sink.BeginPath();
                foreach (PathSegment segment in Clipping.TransformSegments(clip.Segments, matrix))
                {
                    switch (segment.Kind)
                    {
                        case PathSegmentKind.Move:
                            sink.MoveTo(Narrow(segment.X), Narrow(segment.Y));
                            break;
                        case PathSegmentKind.Line:
                            sink.LineTo(Narrow(segment.X), Narrow(segment.Y));
                            break;
                        case PathSegmentKind.Cubic:
                            sink.CubicBezierTo(
                                Narrow(segment.C1X), Narrow(segment.C1Y),
                                Narrow(segment.C2X), Narrow(segment.C2Y),
                                Narrow(segment.X), Narrow(segment.Y));
                            break;
                        case PathSegmentKind.Close:
                            sink.ClosePath();
                            break;
                    }
                }

                sink.ClipToCurrentPath(clip.Rule);
            }

            foreach (IrNode child in clipGroup.Children) EmitNode(child, parentTransform, sink, images);
            sink.RestoreState();
            return;
        }

        if (node is IrGroup group)
        {
            // Group alpha composites the subtree as a unit; the sink owns the
            // transparency group, which is why this is not pushed onto each child.
            sink.SaveState();
            if (group.Opacity < 1.0) sink.SetGlobalAlpha(Narrow(group.Opacity));
            foreach (IrNode child in group.Children) EmitNode(child, parentTransform, sink, images);
            sink.RestoreState();
            return;
        }

        if (node is IrPath path) EmitPath(path, parentTransform, sink);
    }

    private static void EmitPath(IrPath path, Matrix parentTransform, IVectorSink sink)
    {
        ComputedStyle style = path.Style;
        // Already resolved by the builder, opacity folded in - a gradient needs
        // this path's bounding box, which only the builder has.
        Paint? fill = path.FillPaint;
        Paint? stroke = path.StrokePaint;
        // Nothing to draw: skip before touching the sink, so an invisible element
        // leaves no trace in the output.
        if (fill is null && stroke is null) return;

        Matrix transform = Matrix.Multiply(parentTransform, path.Transform);

        sink.SaveState();
        // The transform is applied rather than folded into the coordinates because
        // a scaled group must scale its stroke width too. Flattening the points
        // would silently leave strokes at their unscaled width.
        if (!transform.IsIdentity)
        {
            sink.Transform(
                Narrow(transform.A), Narrow(transform.B), Narrow(transform.C),
                Narrow(transform.D), Narrow(transform.E), Narrow(transform.F));
        }

        BuildPath(path, sink);

        if (fill is not null) sink.FillCurrentPath(fill, style.FillRule);

        if (stroke is not null)
        {
            // Re-declare the path: a sink may consume the current path when it
            // paints, and relying on it surviving a fill is the kind of assumption
            // that works on one backend and not the next.
            if (fill is not null) BuildPath(path, sink);
            sink.StrokeCurrentPath(StrokeStyleFor(stroke, style));
        }

        sink.RestoreState();
    }

    private static void BuildPath(IrPath path, IVectorSink sink)
    {
        sink.BeginPath();
        foreach (PathSegment segment in path.Segments)
        {
            switch (segment.Kind)
            {
                case PathSegmentKind.Move:
                    sink.MoveTo(Narrow(segment.X), Narrow(segment.Y));
                    break;
                case PathSegmentKind.Line:
                    sink.LineTo(Narrow(segment.X), Narrow(segment.Y));
                    break;
                case PathSegmentKind.Cubic:
                    sink.CubicBezierTo(
                        Narrow(segment.C1X), Narrow(segment.C1Y),
                        Narrow(segment.C2X), Narrow(segment.C2Y),
                        Narrow(segment.X), Narrow(segment.Y));
                    break;
                case PathSegmentKind.Close:
                    sink.ClosePath();
                    break;
            }
        }
    }

    private static StrokeStyle StrokeStyleFor(Paint paint, ComputedStyle style) => new()
    {
        Paint = paint,
        Width = Narrow(style.StrokeWidth),
        DashPattern = style.DashPattern?.Select(Narrow).ToArray(),
        DashOffset = style.DashOffset is { } offset ? Narrow(offset) : null,
        LineCap = style.LineCap,
        LineJoin = style.LineJoin,
        // Passed through only when the source specified one - see the note on
        // ComputedStyle.MiterLimit about the differing defaults.
        MiterLimit = style.MiterLimit is { } miter ? Narrow(miter) : null,
    };
}
