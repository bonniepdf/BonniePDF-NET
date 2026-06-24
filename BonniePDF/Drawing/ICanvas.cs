using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Images;

namespace BonniePDF.Drawing;

/// <summary>
/// A device-independent drawing surface. All coordinates are top left origin (Screen Coordinates).
/// </summary>
public interface ICanvas
{
    // --- 1. Dimensions & State ---
    /// <summary>Gets the width of the canvas</summary>
    float Width { get; }

    /// <summary>Gets the height of the canvas</summary>
    float Height { get; }

    /// <summary>Pushes graphics state (PDF: q, SVG: &lt;g&gt;)</summary>
    void SaveState();

    /// <summary>
    /// Pops graphics state (PDF: Q, SVG: &lt;/g&gt;)
    /// </summary>
    void RestoreState();

    // --- 2. Primitives ---

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    void FillRect(float x, float y, float width, float height, Color color);

    /// <summary>
    /// Draws a stroked rectangle (border).
    /// </summary>
    void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth);

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    void FillCircle(float cx, float cy, float radius, Color color);

    /// <summary>
    /// Draws a filled ellipse.
    /// </summary>
    void FillEllipse(float cx, float cy, float rx, float ry, Color color);

    /// <summary>
    /// Fills a closed path defined by a series of points.
    /// The path is automatically closed from the last point to the first.
    /// </summary>
    /// <param name="points">The points that define the path</param>
    /// <param name="color">Fill color</param>
    void FillPath(IList<Point> points, Color color);

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    void FillRoundedRect(float x, float y, float width, float height, float radius, Color color);

    /// <summary>
    /// Draws a stroked rounded rectangle (border).
    /// </summary>
    void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color, float strokeWidth);

    // --- 3. TextBlock ---

    /// <summary>
    /// Draws text at the specified location.
    /// </summary>
    /// <param name="x">Left position</param>
    /// <param name="y">Top position of the bounding box</param>
    /// <param name="text">The text to draw</param>
    /// <param name="color">TextBlock color</param>
    /// <param name="fontSize">Font size in points</param>
    /// <param name="fontFamily">Font family name</param>
    /// <param name="align">Alignment relative to X</param>
    void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily, TextAlignment align);

    /// <summary>
    /// Draws rotated text at the specified location.
    /// </summary>
    /// <param name="x">Left position</param>
    /// <param name="y">Top position of the bounding box</param>
    /// <param name="text">The text to draw</param>
    /// <param name="color">Text color</param>
    /// <param name="fontSize">Font size in points</param>
    /// <param name="fontFamily">Font family name</param>
    /// <param name="angleDegrees">Rotation angle in degrees (counter-clockwise)</param>
    void DrawTextRotated(float x, float y, string text, Color color, float fontSize, string fontFamily, float angleDegrees);

    /// <summary>
    /// Draws justified text by distributing extra space across word gaps to fill the available width.
    /// </summary>
    /// <param name="x">Left position</param>
    /// <param name="y">Top position of the bounding box</param>
    /// <param name="text">The text to draw</param>
    /// <param name="color">Text color</param>
    /// <param name="fontSize">Font size in points</param>
    /// <param name="fontFamily">Font family name</param>
    /// <param name="availableWidth">The full line width to justify across</param>
    void DrawTextJustified(float x, float y, string text, Color color, float fontSize, string fontFamily,
        float availableWidth);

    /// <summary>
    /// Measures text size to allow for precise alignment calculations in charts.
    /// </summary>
    Size MeasureText(string text, float fontSize, string fontFamily);

    /// <summary>
    /// Draws an image at the specified location.
    /// </summary>
    /// <param name="image">The image information</param>
    /// <param name="x">Left position</param>
    /// <param name="y">Top position</param>
    /// <param name="width">Display width</param>
    /// <param name="height">Display height</param>
    void DrawImage(ImageInfo image, float x, float y, float width, float height);

    /// <summary>
    /// Draws a Form XObject (external object reference) at the specified location.
    /// Used for deferred rendering of content (e.g. Total Pages).
    /// </summary>
    /// <param name="resourceName">The resource name (e.g. /DXO1)</param>
    /// <param name="x">Left position</param>
    /// <param name="y">Top position</param>
    void DrawFormXObject(string resourceName, float x, float y);

    // --- 4. Marked content (PDF/UA tagging) ---

    /// <summary>
    /// Begin a marked-content sequence tagged with a PDF structure role
    /// (e.g. <c>"H1"</c>, <c>"P"</c>, <c>"Figure"</c>). On PDF this
    /// emits <c>/&lt;role&gt; &lt;&lt; /MCID n &gt;&gt; BDC</c>; on SVG
    /// it is a no-op (SVG-out doesn't carry PDF structure). The MCID is
    /// auto-allocated and attached to the currently-open structure
    /// element via the tagging context.
    ///
    /// When the canvas has no tagging context (the document is not
    /// opted into PDF/UA), this method is a no-op so renderer code can
    /// call it unconditionally.
    ///
    /// If an outer BDC is already open, the inner call emits BMC instead
    /// to prevent "Nested MCID" warnings in veraPDF.
    /// </summary>
    void BeginMarkedContent(string role);

    /// <summary>
    /// Begin a grouping/container marked-content sequence without
    /// allocating an MCID. Emits <c>/&lt;role&gt; BMC</c> on PDF. Use
    /// for container roles (Table, TR, THead, TBody, TFoot, Sect, …)
    /// whose content is referenced by child structure elements rather
    /// than directly by an MCID.
    ///
    /// No-op when the document is not tagged or when using SVG output.
    /// </summary>
    void BeginContainerContent(string role);

    /// <summary>
    /// End the most recent marked-content sequence opened by
    /// <see cref="BeginMarkedContent"/>. Emits <c>EMC</c> on PDF; no-op
    /// on SVG and when tagging is disabled.
    /// </summary>
    void EndMarkedContent();

    /// <summary>
    /// Begin an artifact (decorative/navigational) marked-content region.
    /// Emits <c>/Artifact BMC</c> on PDF. Unlike
    /// <see cref="BeginMarkedContent"/>, artifact regions are not
    /// registered in the structure tree and do not allocate an MCID.
    /// No-op when the document is not tagged or when using SVG output.
    /// </summary>
    void BeginArtifact();

    /// <summary>
    /// End the artifact region opened by <see cref="BeginArtifact"/>.
    /// Emits <c>EMC</c> on PDF; no-op on SVG.
    /// </summary>
    void EndArtifact();

    /// <summary>
    /// True when drawing currently sits inside an /Artifact region. Used by the
    /// child-tagging helper to suppress structure tags (PDF/UA 7.1-2 forbids
    /// tagged content inside an artifact). Defaults to <c>false</c> for canvases
    /// that don't track artifact nesting (e.g. SVG output).
    /// </summary>
    bool IsInsideArtifact() => false;

    /// <summary>
    /// Begin a region whose shapes should render with <c>shape-rendering="crispEdges"</c>
    /// (axis-aligned lines/rects snapped to the device pixel grid, no antialiasing
    /// blur). Used for chart axis lines so they stay sharp even when the chart as a
    /// whole keeps default antialiasing for diagonals/arcs. On SVG this opens a
    /// <c>&lt;g shape-rendering="crispEdges"&gt;</c>; on PDF (and other canvases) it
    /// is a no-op via this default implementation. Pair with <see cref="EndCrispEdges"/>.
    /// </summary>
    void BeginCrispEdges() { }

    /// <summary>End the region opened by <see cref="BeginCrispEdges"/>.</summary>
    void EndCrispEdges() { }

    // --- 5. Clipping ---

    /// <summary>
    /// Begin a rounded-rectangle clipping region: all subsequent drawing is
    /// masked to the rounded rectangle at (<paramref name="x"/>,
    /// <paramref name="y"/>) sized <paramref name="width"/>×<paramref name="height"/>
    /// with the given corner <paramref name="radius"/> (top-left origin, like every
    /// other canvas coordinate). On PDF this pushes the graphics state and emits a
    /// rounded-rect clip path (<c>W n</c>); on SVG it opens a <c>&lt;g clip-path&gt;</c>
    /// referencing a generated <c>&lt;clipPath&gt;</c>. Canvases that don't support
    /// clipping fall back to this no-op default (the content simply isn't masked).
    /// Must be paired with <see cref="EndClip"/>.
    /// </summary>
    void BeginRoundedClip(float x, float y, float width, float height, float radius) { }

    /// <summary>End the clipping region opened by <see cref="BeginRoundedClip"/>.</summary>
    void EndClip() { }

    // --- 6. Path building ---

    /// <summary>Begin a new path, discarding any existing uncommitted path.</summary>
    void BeginPath();

    /// <summary>Move the current point to (<paramref name="x"/>, <paramref name="y"/>) without drawing.</summary>
    void MoveTo(float x, float y);

    /// <summary>Add a straight line segment from the current point to (<paramref name="x"/>, <paramref name="y"/>).</summary>
    void LineTo(float x, float y);

    /// <summary>Add a cubic Bezier curve.</summary>
    void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y);

    /// <summary>Add a quadratic Bezier curve.</summary>
    void QuadraticBezierTo(float cpx, float cpy, float x, float y);

    /// <summary>Add a rectangle sub-path.</summary>
    void AddRect(float x, float y, float width, float height);

    /// <summary>Close the current sub-path.</summary>
    void ClosePath();

    /// <summary>Fill the current path with the given paint.</summary>
    void FillCurrentPath(Paint paint, FillRule fillRule = FillRule.NonZero);

    /// <summary>Stroke the current path.</summary>
    void StrokeCurrentPath(StrokeStyle style);

    // --- 7. Transforms ---

    /// <summary>Translate the coordinate system.</summary>
    void Translate(float dx, float dy);

    /// <summary>Scale the coordinate system.</summary>
    void Scale(float sx, float sy);

    /// <summary>Rotate the coordinate system by the given angle in degrees.</summary>
    void Rotate(float angleDegrees);

    /// <summary>Apply a 2D affine transform matrix [a b c d tx ty].</summary>
    void Transform(float a, float b, float c, float d, float tx, float ty);

    // --- 8. Clipping & Alpha ---

    /// <summary>Clip subsequent drawing to the current path.</summary>
    void ClipToCurrentPath(FillRule fillRule = FillRule.NonZero);

    /// <summary>Set global alpha for subsequent drawing operations (0.0 to 1.0).</summary>
    void SetGlobalAlpha(float alpha);
}