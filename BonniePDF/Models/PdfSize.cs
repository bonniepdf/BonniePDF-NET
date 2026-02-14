using System;

namespace BonniePDF.Models;

public struct PdfSize
{
    public float Width { get; }
    public float Height { get; }

    private const float PointsPerInch = 72f;
    private const float MillimetersPerInch = 25.4f;

    public PdfSize(float width, float height)
    {
        Width = width;
        Height = height;
    }

    // --- Conversion Helpers ---

    /// <summary>Creates a size from inches</summary>
    public static PdfSize FromInches(float width, float height) =>
        new(width * PointsPerInch, height * PointsPerInch);

    /// <summary>Creates a size from millimeters</summary>
    public static PdfSize FromMM(float width, float height) =>
        new((width / MillimetersPerInch) * PointsPerInch, (height / MillimetersPerInch) * PointsPerInch);

    // --- ISO A-Series ---
    public static PdfSize A0 => new(2383.94f, 3370.39f);
    public static PdfSize A1 => new(1683.78f, 2383.94f);
    public static PdfSize A2 => new(1190.55f, 1683.78f);
    public static PdfSize A3 => new(841.89f, 1190.55f);
    public static PdfSize A4 => new(595.28f, 841.89f);
    public static PdfSize A5 => new(420f, 595.28f);

    // --- ISO B-Series (Common for Books/Posters) ---
    public static PdfSize B0 => new(2834.65f, 4008.19f);
    public static PdfSize B1 => new(2004.09f, 2834.65f);
    public static PdfSize B2 => new(1417.32f, 2004.09f);
    public static PdfSize B3 => new(1000.63f, 1417.32f);
    public static PdfSize B4 => new(708.66f, 1000.63f);
    public static PdfSize B5 => new(498.90f, 708.66f);

    // --- North American Standards ---
    public static PdfSize Letter => FromInches(8.5f, 11f);
    public static PdfSize Legal => FromInches(8.5f, 14f);
    public static PdfSize Tabloid => FromInches(11f, 17f);
    public static PdfSize Ledger => FromInches(17f, 11f);
    public static PdfSize Statement => FromInches(5.5f, 8.5f);
    public static PdfSize Executive => FromInches(7.25f, 10.5f);

    // --- Orientation Logic (Idempotent) ---
    public readonly PdfSize Portrait() => new(Math.Min(Width, Height), Math.Max(Width, Height));
    public readonly PdfSize Landscape() => new(Math.Max(Width, Height), Math.Min(Width, Height));

    public PdfSize GetOrientedSize(PageOrientation orientation) =>
        orientation == PageOrientation.Landscape ? Landscape() : Portrait();

    public readonly bool IsLandscape => Width > Height;
    public readonly bool IsPortrait => Height >= Width;
}