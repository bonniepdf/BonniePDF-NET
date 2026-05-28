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
}