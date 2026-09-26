// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
namespace BonniePDF.Rendering.Svg.Sink;

/// <summary>
/// The one seam between this package and whatever draws its output.
/// </summary>
/// <remarks>
/// <para>
/// Copied from BonniePDF's shipped <c>ICanvas</c>
/// (<c>BonniePDF/Drawing/ICanvas.cs</c>), specifically the path / paint /
/// transform / clip surface that this feature landed. It is a <em>copy</em>, not a
/// reference: this package never depends on the <c>BonniePDF</c> assembly.
/// Keeping the copy faithful is what makes fold-in a mechanical adapter
/// rather than a redesign — read AGENTS.md before changing any signature.
/// </para>
/// <para>
/// Coordinates are in sink user space, y-down. The parser resolves viewBox and
/// <c>preserveAspectRatio</c> into a root transform and emits everything
/// through that, so this interface needs no notion of viewports.
/// </para>
/// <para>
/// Deliberate narrowings relative to <c>ICanvas</c>, both recorded in
/// AGENTS.md: <see cref="FillCurrentPath"/> takes a <see cref="Paint"/> only,
/// where <c>ICanvas</c> also accepts a bare colour for caller convenience;
/// text, form-XObject, marked-content and legacy-clip members are absent,
/// because v1 outlines text to paths; and <see cref="DrawImage"/> takes a
/// <see cref="SinkImage"/> of encoded PNG bytes where <c>ICanvas</c> takes a
/// decoded <c>ImageInfo</c>. It exists for the rasterizer fallback only.
/// </para>
/// </remarks>
internal interface IVectorSink
{
    // -- Graphics state --------------------------------------------------
    // (These two predate this feature; <g> nesting needs them.)

    /// <summary>Pushes a graphics-state scope.</summary>
    void SaveState();

    /// <summary>Pops the innermost graphics-state scope.</summary>
    void RestoreState();

    // -- Path construction ----------------------------------------------

    /// <summary>Starts a new current path, discarding any unpainted one.</summary>
    void BeginPath();

    /// <summary>Starts a new subpath at the given point.</summary>
    void MoveTo(float x, float y);

    /// <summary>Appends a straight segment to the given point.</summary>
    void LineTo(float x, float y);

    /// <summary>Appends a cubic Bézier segment.</summary>
    void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y);

    /// <summary>Appends a quadratic Bézier segment.</summary>
    void QuadraticBezierTo(float cpx, float cpy, float x, float y);

    /// <summary>Appends a closed rectangle.</summary>
    void AddRect(float x, float y, float width, float height);

    /// <summary>Appends a closed axis-aligned ellipse, as four cubic Béziers.</summary>
    void AddEllipse(float cx, float cy, float rx, float ry);

    /// <summary>Appends a closed circle. See <see cref="AddEllipse"/>.</summary>
    void AddCircle(float cx, float cy, float radius);

    /// <summary>Closes the current subpath.</summary>
    void ClosePath();

    // -- Painting the current path ---------------------------------------

    /// <summary>Fills the current path.</summary>
    void FillCurrentPath(Paint paint, FillRule fillRule = FillRule.NonZero);

    /// <summary>Strokes the current path.</summary>
    void StrokeCurrentPath(StrokeStyle style);

    // -- Affine transforms -----------------------------------------------

    /// <summary>Translates the coordinate system.</summary>
    void Translate(float dx, float dy);

    /// <summary>Scales the coordinate system.</summary>
    void Scale(float sx, float sy);

    /// <summary>Rotates the coordinate system, in degrees.</summary>
    void Rotate(float angleDegrees);

    /// <summary>Concatenates an arbitrary affine matrix onto the current one.</summary>
    void Transform(float a, float b, float c, float d, float tx, float ty);

    // -- Clipping + compositing ------------------------------------------

    /// <summary>Intersects the clip region with the current path.</summary>
    void ClipToCurrentPath(FillRule fillRule = FillRule.NonZero);

    /// <summary>Sets the constant alpha applied to subsequent painting.</summary>
    void SetGlobalAlpha(float alpha);

    // -- Images -----------------------------------------------------------
    // Only the rasterizer fallback draws one.

    /// <summary>Draws an image with its top-left corner at (x, y), in the current user space.</summary>
    /// <param name="image">The image.</param>
    /// <param name="x">Left position.</param>
    /// <param name="y">Top position.</param>
    /// <param name="width">Display width.</param>
    /// <param name="height">Display height.</param>
    void DrawImage(SinkImage image, float x, float y, float width, float height);
}
