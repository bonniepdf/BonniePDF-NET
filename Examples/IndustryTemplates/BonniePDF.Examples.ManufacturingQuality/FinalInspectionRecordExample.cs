using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.ManufacturingQuality;

/// <summary>
/// A final inspection record on landscape A4: twelve machined parts against ten checks, with the check
/// names set vertically above narrow columns, measurements beside pass or fail marks, and the rejected
/// part flagged for a nonconformance report.
/// </summary>
[System.ComponentModel.DisplayName("Final Inspection Record")]
[System.ComponentModel.Category("Manufacturing & Quality")]
[System.ComponentModel.Description("A final inspection record on landscape A4: twelve machined parts against ten checks, with the check names set vertically above narrow columns, measurements beside pass or fail marks, and the rejected part flagged for a nonconformance report.")]
public static class FinalInspectionRecordExample
{
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Body = ColorPalette.Stone[700];
    private static readonly Color Mute = ColorPalette.Stone[500];
    private static readonly Color Accent = ColorPalette.Orange[800];
    private static readonly Color Fail = ColorPalette.Rose[700];

    /// <summary>The checks, in the order they are made: name, the requirement, and whether the result is a measurement.</summary>
    private static readonly (string Name, string Requirement, bool Measured)[] Checks =
    [
        ("Bore A", "Ø12.00 ±0.02", true),
        ("Bore B", "Ø8.00 ±0.02", true),
        ("Overall length", "120.0 ±0.1", true),
        ("Flatness", "0.05 max", true),
        ("Surface finish", "Ra 1.6 max", true),
        ("Thread M10", "Go / no-go", false),
        ("Hardness", "55 to 60 HRC", true),
        ("Leak test", "210 bar, 5 min", false),
        ("Marking", "Legible, correct", false),
        ("Visual", "No burrs or damage", false),
    ];

    /// <summary>One row per serial number: each check's result, as a measurement or "Pass" / "Fail".</summary>
    private static readonly (string Serial, string[] Results)[] Parts =
    [
        ("HM-0401", ["12.01", "8.00", "120.02", "0.02", "0.9", "Pass", "57", "Pass", "Pass", "Pass"]),
        ("HM-0402", ["12.00", "7.99", "119.98", "0.03", "1.1", "Pass", "58", "Pass", "Pass", "Pass"]),
        ("HM-0403", ["11.99", "8.01", "120.01", "0.02", "1.0", "Pass", "56", "Pass", "Pass", "Pass"]),
        ("HM-0404", ["12.02", "8.00", "120.04", "0.04", "1.2", "Pass", "57", "Pass", "Pass", "Pass"]),
        ("HM-0405", ["12.01", "8.02", "119.97", "0.03", "0.8", "Pass", "59", "Pass", "Pass", "Pass"]),
        ("HM-0406", ["12.01", "8.01", "120.00", "0.02", "1.0", "Pass", "58", "Fail", "Pass", "Pass"]),
        ("HM-0407", ["12.00", "8.00", "120.03", "0.01", "0.9", "Pass", "57", "Pass", "Pass", "Pass"]),
        ("HM-0408", ["11.99", "7.99", "119.99", "0.03", "1.3", "Pass", "56", "Pass", "Pass", "Pass"]),
        ("HM-0409", ["12.01", "8.00", "120.01", "0.02", "1.1", "Pass", "58", "Pass", "Pass", "Pass"]),
        ("HM-0410", ["12.00", "8.01", "120.02", "0.04", "0.9", "Pass", "57", "Pass", "Pass", "Pass"]),
        ("HM-0411", ["12.02", "8.00", "119.98", "0.02", "1.0", "Pass", "59", "Pass", "Pass", "Pass"]),
        ("HM-0412", ["12.01", "7.99", "120.00", "0.03", "1.2", "Pass", "58", "Pass", "Pass", "Pass"]),
    ];

    /// <summary>The measuring equipment used: instrument, asset number, and when its calibration falls due.</summary>
    private static readonly (string Instrument, string Asset, string Due)[] Equipment =
    [
        ("Bore gauge, 6 to 18 mm", "QG-114", "12 Jan 2027"),
        ("Height gauge, 300 mm", "QG-087", "3 Mar 2027"),
        ("Surface roughness tester", "QG-152", "19 Dec 2026"),
        ("Rockwell hardness tester", "QG-031", "8 Feb 2027"),
        ("Hydraulic leak test rig", "QR-009", "27 Nov 2026"),
    ];

    /// <summary>The tolerance band of each measured check, used to flag a measurement outside it.</summary>
    private static readonly (double Low, double High)?[] Limits =
    [
        (11.98, 12.02), (7.98, 8.02), (119.9, 120.1), (0, 0.05), (0, 1.6), null, (55, 60), null, null, null,
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Orientation = PageOrientation.Landscape,
                Margin = 30,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(Body),
                FooterTemplate = PageTemplate.FromElements(@default: new TextBlock(
                    "Sample Precision Ltd  ·  Quality record QR-7.5-03, revision 4  ·  Retain for ten years with the batch file")
                {
                    FontSize = 7,
                    Color = Mute,
                }),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Final inspection record: batch 24-118",
                Author = "Sample Precision Ltd",
                Subject = "Final inspection of hydraulic manifold blocks HM-220",
            },
        };

        // ── Title and the batch at a glance ──
        HorizontalStack top = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 20, Margin = new Thickness(0, 0, 0, 12) };
        VerticalStack title = new() { Spacing = 3 };
        title.Add(Tb("SAMPLE PRECISION LTD  ·  QUALITY", 7.5f, Accent, bold: true, letterSpacing: 1.5f));
        title.Add(new Heading("Final inspection record")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            Color = Ink,
            Margin = new Thickness(0),
        });
        title.Add(Tb("Hydraulic manifold block HM-220, drawing HM-220-01 revision C", 9.5f, Body));
        top.Add(title, 1);
        foreach (var (label, value) in new[] { ("Works order", "WO-26-0418"), ("Batch", "24-118"), ("Quantity", "12"), ("Inspected", "5 Nov 2026") })
        {
            VerticalStack fact = new() { Spacing = 2 };
            fact.Add(Tb(label.ToUpperInvariant(), 6.5f, ColorPalette.Stone[600], bold: true));
            fact.Add(Tb(value, 11, Ink, bold: true));
            top.Add(new Container { BackgroundColor = ColorPalette.Stone[100], CornerRadius = 4, Padding = new Thickness(10, 7, 10, 7), Child = fact });
        }
        doc.Add(top);

        // ── The matrix: one row per part, one narrow column per check, check names set vertically ──
        Table matrix = new([1.3f, .. Checks.Select(_ => 1f), 1.1f])
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[300]),
            HeaderBackgroundColor = ColorPalette.Stone[800],
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 12),
        };
        TableCell[] header =
        [
            HeaderCell(Tb("Serial number", 7.5f, Color.White, bold: true)),
            .. Checks.Select(check => HeaderCell(Vertical(check.Name, check.Requirement))),
            HeaderCell(Tb("Result", 7.5f, Color.White, bold: true)),
        ];
        matrix.AddHeader(header);
        foreach (var (serial, results) in Parts)
        {
            bool failed = results.Select((result, i) => Outside(result, i)).Any(b => b);
            TableRow row = new()
            {
                Cells =
                [
                    new TableCell { Content = Tb(serial, 8.5f, Ink, bold: true) },
                    .. results.Select((result, i) => new TableCell
                    {
                        Content = Tb(result, 8, Outside(result, i) ? Fail : Body, bold: Outside(result, i), align: TextAlignment.Center),
                    }),
                    new TableCell { Content = Verdict(failed) },
                ],
            };
            if (failed) row.BackgroundColor = ColorPalette.Rose[50];
            matrix.AddRow(row);
        }
        doc.Add(matrix);

        // ── Outcome and sign-off ──
        int rejected = Parts.Count(part => part.Results.Select((result, i) => Outside(result, i)).Any(b => b));
        Table equipment = new(2, 0.8f, 1)
        {
            CellPadding = 3,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[100],
            HeaderTextColor = Ink,
        };
        equipment.AddHeader(Tb("Measuring equipment", 7.5f, Ink, bold: true), Tb("Asset", 7.5f, Ink, bold: true), Tb("Calibration due", 7.5f, Ink, bold: true));
        foreach (var (instrument, asset, due) in Equipment) equipment.AddRow(Tb(instrument, 8, Body), Tb(asset, 8, Body), Tb(due, 8, Body));

        HorizontalStack outcome = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 24 };
        VerticalStack summary = new() { Spacing = 3 };
        summary.Add(Tb(string.Create(CultureInfo.InvariantCulture, $"{Parts.Length - rejected} accepted, {rejected} rejected"), 11, Ink, bold: true));
        summary.Add(Tb("HM-0406 failed the leak test at 210 bar and is quarantined under nonconformance report NCR-26-0093. Measurements in millimetres unless stated; hardness on the Rockwell C scale.", 8, Body));
        outcome.Add(summary, 1.1f);
        outcome.Add(equipment, 1.4f);
        VerticalStack signatures = new() { Spacing = 4 };
        foreach (string who in new[] { "Inspected by: J. Patel", "Released by: M. Novak, quality engineer" })
        {
            signatures.Add(new Container { Height = 22, Border = new Border(0, 0, 0, 0.75f), BorderColors = new BorderColors(ColorPalette.Stone[400]) });
            signatures.Add(Tb(who, 7.5f, Mute));
        }
        outcome.Add(signatures, 1);
        doc.Add(outcome);
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Text in Helvetica or Helvetica Bold.</summary>
    private static TextBlock Tb(string text, float size, Color color, bool bold = false, TextAlignment? align = null, float? letterSpacing = null)
    {
        TextBlock block = new(text)
        {
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) block.Alignment = align.Value;
        if (letterSpacing.HasValue) block.LetterSpacing = letterSpacing.Value;
        return block;
    }

    /// <summary>A check's name and requirement, turned a quarter turn to read upwards.</summary>
    private static Rotate Vertical(string name, string requirement)
    {
        TextBlock title = Tb(name, 7.5f, Color.White, bold: true);
        TextBlock limit = Tb(requirement, 6.5f, ColorPalette.Stone[300]);
        VerticalStack stack = new() { Spacing = 1 };
        stack.Add(title);
        stack.Add(limit);
        return new Rotate
        {
            Angle = 270,
            Mode = RotationMode.Constrained,
            ContentWidth = MathF.Ceiling(MathF.Max(title.GetPreferredWidth(), limit.GetPreferredWidth())) + 1,
            Child = stack,
        };
    }

    /// <summary>A header cell, padded for the dark header row.</summary>
    private static TableCell HeaderCell(IElement content) => new()
    {
        Content = content,
        Padding = new Thickness(4, 6, 4, 6),
        VerticalAlignment = VerticalAlignment.End,
    };

    /// <summary>True when a result is a failed attribute check or a measurement outside its tolerance.</summary>
    private static bool Outside(string result, int check)
    {
        if (result == "Fail") return true;
        if (Limits[check] is not { } limits) return false;
        double value = double.Parse(result, CultureInfo.InvariantCulture);
        return value < limits.Low || value > limits.High;
    }

    /// <summary>The part's verdict as a small chip.</summary>
    private static Container Verdict(bool failed) => new()
    {
        FitContent = true,
        BackgroundColor = failed ? ColorPalette.Rose[100] : ColorPalette.Emerald[50],
        CornerRadius = 3,
        Padding = new Thickness(5, 1.5f, 5, 1.5f),
        Child = Tb(failed ? "Reject" : "Accept", 7.5f, failed ? ColorPalette.Rose[800] : ColorPalette.Emerald[800], bold: true),
    };
}
