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
}