using System;
using System.Collections.Generic;

namespace BonniePDF.Models;

/// <summary>
/// Represents the size of a PDF page in points.
/// Includes standard page sizes (A4, Letter, etc.) and helper methods for orientation and unit conversion.
/// </summary>
public struct PdfSize
{
    /// <summary>
    /// Gets all standard PDF sizes defined in this struct.
    /// </summary>
    public static IEnumerable<PdfSize> AllSizes => new[]
    {
        A0, A1, A2, A3, A4, A5,
        B0, B1, B2, B3, B4, B5,
        Letter, Legal, Tabloid, Ledger, Statement, Executive
    };

    /// <summary>
    /// Gets the width of the page in points.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height of the page in points.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Gets the name of the page size (e.g., "A4", "Letter").
    /// Returns null for custom sizes.
    /// </summary>
    public string? Name { get; }

    private const float PointsPerInch = 72f;
    private const float MillimetersPerInch = 25.4f;

    /// <summary>
    /// Creates a new PdfSize with the specified width and height in points.
    /// </summary>
    /// <param name="width">Width in points.</param>
    /// <param name="height">Height in points.</param>
    /// <param name="name">Optional name of the page size.</param>
    public PdfSize(float width, float height, string? name = null)
    {
        Width = width;
        Height = height;
        Name = name;
    }

    // --- Conversion Helpers ---

    /// <summary>
    /// Creates a PdfSize from dimensions in inches.
    /// </summary>
    /// <param name="width">Width in inches.</param>
    /// <param name="height">Height in inches.</param>
    /// <param name="name">Optional name of the page size.</param>
    /// <returns>A new PdfSize converted to points.</returns>
    public static PdfSize FromInches(float width, float height, string? name = null) =>
        new(width * PointsPerInch, height * PointsPerInch, name);

    /// <summary>
    /// Creates a PdfSize from dimensions in millimeters.
    /// </summary>
    /// <param name="width">Width in millimeters.</param>
    /// <param name="height">Height in millimeters.</param>
    /// <param name="name">Optional name of the page size.</param>
    /// <returns>A new PdfSize converted to points.</returns>
    public static PdfSize FromMM(float width, float height, string? name = null) =>
        new((width / MillimetersPerInch) * PointsPerInch, (height / MillimetersPerInch) * PointsPerInch, name);

    // --- ISO A-Series ---

    /// <summary>ISO A0 size (841 x 1189 mm).</summary>
    public static PdfSize A0 => FromMM(841f, 1189f, "A0");

    /// <summary>ISO A1 size (594 x 841 mm).</summary>
    public static PdfSize A1 => FromMM(594f, 841f, "A1");

    /// <summary>ISO A2 size (420 x 594 mm).</summary>
    public static PdfSize A2 => FromMM(420f, 594f, "A2");

    /// <summary>ISO A3 size (297 x 420 mm).</summary>
    public static PdfSize A3 => FromMM(297f, 420f, "A3");

    /// <summary>ISO A4 size (210 x 297 mm).</summary>
    public static PdfSize A4 => FromMM(210f, 297f, "A4");

    /// <summary>ISO A5 size (148 x 210 mm).</summary>
    public static PdfSize A5 => FromMM(148f, 210f, "A5");

    // --- ISO B-Series (Common for Books/Posters) ---

    /// <summary>ISO B0 size (1000 x 1414 mm).</summary>
    public static PdfSize B0 => FromMM(1000f, 1414f, "B0");

    /// <summary>ISO B1 size (707 x 1000 mm).</summary>
    public static PdfSize B1 => FromMM(707f, 1000f, "B1");

    /// <summary>ISO B2 size (500 x 707 mm).</summary>
    public static PdfSize B2 => FromMM(500f, 707f, "B2");

    /// <summary>ISO B3 size (353 x 500 mm).</summary>
    public static PdfSize B3 => FromMM(353f, 500f, "B3");

    /// <summary>ISO B4 size (250 x 353 mm).</summary>
    public static PdfSize B4 => FromMM(250f, 353f, "B4");

    /// <summary>ISO B5 size (176 x 250 mm).</summary>
    public static PdfSize B5 => FromMM(176f, 250f, "B5");

    // --- North American Standards ---

    /// <summary>Letter size (8.5 x 11 inches).</summary>
    public static PdfSize Letter => FromInches(8.5f, 11f, "Letter");

    /// <summary>Legal size (8.5 x 14 inches).</summary>
    public static PdfSize Legal => FromInches(8.5f, 14f, "Legal");

    /// <summary>Tabloid size (11 x 17 inches).</summary>
    public static PdfSize Tabloid => FromInches(11f, 17f, "Tabloid");

    /// <summary>Ledger size (17 x 11 inches).</summary>
    public static PdfSize Ledger => FromInches(17f, 11f, "Ledger");

    /// <summary>Statement size (5.5 x 8.5 inches).</summary>
    public static PdfSize Statement => FromInches(5.5f, 8.5f, "Statement");

    /// <summary>Executive size (7.25 x 10.5 inches).</summary>
    public static PdfSize Executive => FromInches(7.25f, 10.5f, "Executive");

    // --- Orientation Logic (Idempotent) ---

    /// <summary>
    /// Returns a new PdfSize in portrait orientation (Height >= Width).
    /// </summary>
    public readonly PdfSize Portrait() => new(Math.Min(Width, Height), Math.Max(Width, Height), Name);

    /// <summary>
    /// Returns a new PdfSize in landscape orientation (Width > Height).
    /// </summary>
    public readonly PdfSize Landscape() => new(Math.Max(Width, Height), Math.Min(Width, Height), Name);

    /// <summary>
    /// Returns a new PdfSize adjusted for the specified orientation.
    /// </summary>
    /// <param name="orientation">The desired page orientation.</param>
    /// <returns>A new PdfSize with width and height swapped if necessary.</returns>
    public PdfSize GetOrientedSize(PageOrientation orientation) =>
        orientation == PageOrientation.Landscape ? Landscape() : Portrait();

    /// <summary>
    /// Gets a value indicating whether the size is landscape (Width > Height).
    /// </summary>
    public readonly bool IsLandscape => Width > Height;

    /// <summary>
    /// Gets a value indicating whether the size is portrait (Height >= Width).
    /// </summary>
    public readonly bool IsPortrait => Height >= Width;
}
