using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.LabReport;

/// <summary>
/// Fictional UK/EU pathology laboratory report with patient demographics,
/// specimen timestamps, grouped result panels, reference ranges, flags, clinical
/// interpretation, and authorisation details. This example uses no real health
/// service branding and contains no real medical data.
/// </summary>
[System.ComponentModel.DisplayName("Pathology Lab Report")]
[System.ComponentModel.Category("Healthcare")]
[System.ComponentModel.Description("Fictional UK/EU pathology laboratory report with patient demographics, specimen timestamps, grouped result panels, reference ranges, High/Low flags, and clinical interpretation.")]
public static class LabReportExample
{
    private static readonly Color ClinicalBlue = Color.FromHex("#2563eb");
    private static readonly Color Ink = ColorPalette.Slate[700];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[200];
    private static readonly Color HeaderBackground = ColorPalette.Slate[100];
    private static readonly Color High = ColorPalette.Rose[600];
    private static readonly Color Low = ColorPalette.Amber[600];

    private static readonly Panel[] Panels =
    [
        new("Full Blood Count (FBC)",
        [
            new("Haemoglobin", "118", "g/L", "120 - 150", "L"),
            new("White cell count", "11.8", "x10^9/L", "4.0 - 11.0", "H"),
            new("Platelets", "265", "x10^9/L", "150 - 400", ""),
            new("Haematocrit", "0.37", "L/L", "0.36 - 0.46", ""),
            new("Mean cell volume (MCV)", "88", "fL", "80 - 100", ""),
            new("Neutrophils", "8.9", "x10^9/L", "2.0 - 7.5", "H"),
            new("Lymphocytes", "2.1", "x10^9/L", "1.0 - 4.0", ""),
        ]),
        new("Urea & Electrolytes (U&E)",
        [
            new("Sodium", "139", "mmol/L", "133 - 146", ""),
            new("Potassium", "5.3", "mmol/L", "3.5 - 5.3", ""),
            new("Urea", "7.8", "mmol/L", "2.5 - 7.8", ""),
            new("Creatinine", "96", "umol/L", "49 - 90", "H"),
            new("eGFR", "62", "mL/min/1.73m2", "> 90", "L"),
        ]),
    ];

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, 50)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"CONFIDENTIAL - Patient: WESTON, Harold (Patient ID CGH-9434765919)  -  Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 8,
                TextColor = Muted,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Laboratory Report - specimen L26-118842";
        doc.Metadata.Author = "City General Hospital Pathology Services";
        doc.Metadata.Subject = "Pathology laboratory report with reference ranges and flags (fictional).";

        doc.Add(Header());
        doc.Add(BlueRule());
        doc.Add(InfoBlock());

        foreach (Panel panel in Panels)
        {
            doc.Add(H2(panel.Title));
            doc.Add(PanelTable(panel));
        }

        doc.Add(H2("Clinical interpretation"));
        doc.Add(Body("Mild normocytic anaemia with a neutrophil leucocytosis, consistent with an acute inflammatory or infective process. Renal function shows a mildly raised creatinine with eGFR 62 mL/min/1.73m2 (CKD stage G2). Suggest clinical correlation and a repeat FBC in 7-10 days. Flags: H = above reference range, L = below reference range."));
        doc.Add(Authorisation());

        return doc;
    }

    private static Table Header()
    {
        Table header = new(2.4f, 1)
        {
            CellPadding = 0,
            Border = Border.None,
        };

        VerticalStack org = new();
        org.AddChild(new TextBlock
        {
            Text = "City General Hospital Pathology Services",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ClinicalBlue,
        });
        org.AddChild(new TextBlock
        {
            Text = "Department of Laboratory Medicine - Haematology & Clinical Chemistry",
            FontSize = 9,
            TextColor = Muted,
            Margin = new Thickness(0, 2, 0, 0),
        });
        org.AddChild(new TextBlock
        {
            Text = "UKAS Medical Laboratory 8842 - accredited to ISO 15189",
            FontSize = 8,
            TextColor = Muted,
            Margin = new Thickness(0, 1, 0, 0),
        });

        Heading title = new("LABORATORY REPORT")
        {
            TocIdentifier = "lab-report",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            TextColor = Ink,
            Alignment = TextAlignment.Right,
        };

        header.AddRow(new Container { Child = org }, new Container { Child = title });
        return header;
    }

    private static Container BlueRule() => new()
    {
        BackgroundColor = ClinicalBlue,
        Padding = new Thickness(0, 1.5f, 0, 0),
        Margin = new Thickness(0, 6, 0, 16),
        Child = new TextBlock { Text = " ", FontSize = 1 },
    };

    private static Table InfoBlock()
    {
        Table info = new(1, 1)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 18),
        };

        VerticalStack patient = new();
        patient.AddChild(SectionLabel("PATIENT"));
        patient.AddChild(KeyValue("Name", "WESTON, Harold"));
        patient.AddChild(KeyValue("Date of birth", "02 Nov 1951 (74y)"));
        patient.AddChild(KeyValue("Sex", "Male"));
        patient.AddChild(KeyValue("Patient ID", "CGH-9434765919"));
        patient.AddChild(KeyValue("Hospital no.", "CGH-2208814"));

        VerticalStack specimen = new();
        specimen.AddChild(SectionLabel("SPECIMEN"));
        specimen.AddChild(KeyValue("Specimen ID", "L26-118842"));
        specimen.AddChild(KeyValue("Collected", "06 Jul 2026 08:14"));
        specimen.AddChild(KeyValue("Received", "06 Jul 2026 09:02"));
        specimen.AddChild(KeyValue("Reported", "06 Jul 2026 11:37"));
        specimen.AddChild(KeyValue("Requested by", "Dr A. Kaur, Ward 12"));

        info.AddRow(new Container { Child = patient }, new Container { Child = specimen });
        return info;
    }

    private static Table PanelTable(Panel panel)
    {
        Table table = new(2.6f, 1, 1.2f, 1.4f, 0.7f)
        {
            CellPadding = 7,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = HeaderBackground,
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 4, 0, 14),
        };
        table.AddHeader(
            Th("Analyte", TextAlignment.Left),
            Th("Result", TextAlignment.Right),
            Th("Units", TextAlignment.Left),
            Th("Reference range", TextAlignment.Left),
            Th("Flag", TextAlignment.Center));

        foreach (Analyte analyte in panel.Rows)
        {
            bool abnormal = analyte.Flag.Length > 0;
            Color resultColor = string.Equals(analyte.Flag, "H", StringComparison.Ordinal)
                ? High
                : string.Equals(analyte.Flag, "L", StringComparison.Ordinal)
                    ? Low
                    : Ink;
            table.AddRow(
                Td(analyte.Name, TextAlignment.Left, Ink, false),
                Td(analyte.Result, TextAlignment.Right, resultColor, abnormal),
                Td(analyte.Units, TextAlignment.Left, Muted, false),
                Td(analyte.Range, TextAlignment.Left, Muted, false),
                Td(analyte.Flag, TextAlignment.Center, resultColor, true));
        }

        return table;
    }

    private static Container Authorisation()
    {
        VerticalStack stack = new();
        stack.AddChild(SectionLabel("AUTHORISED BY"));
        stack.AddChild(new TextBlock
        {
            Text = "Dr S. Patel, Consultant Haematologist (electronically authorised)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 10,
            TextColor = Ink,
            Margin = new Thickness(0, 2, 0, 2),
        });
        stack.AddChild(new TextBlock
        {
            Text = "Reported 06 Jul 2026 11:37  -  Analyser: Sysmex XN-1000 / Roche cobas c503",
            FontSize = 8.5f,
            TextColor = Muted,
        });

        return new Container
        {
            BackgroundColor = ColorPalette.Blue[50],
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(ClinicalBlue),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 8, 0, 0),
            Child = stack,
        };
    }

    private static Heading H2(string text) => new(text)
    {
        TocIdentifier = text.ToLowerInvariant().Replace(' ', '-').Replace("&", "and", StringComparison.Ordinal),
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        TextColor = ClinicalBlue,
        Margin = new Thickness(0, 4, 0, 6),
    };

    private static TextBlock Body(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 9.5f,
        TextColor = Ink,
        Alignment = TextAlignment.Justify,
        Margin = new Thickness(0, 0, 0, 10),
    };

    private static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 8,
        TextColor = ClinicalBlue,
        Margin = new Thickness(0, 0, 0, 6),
    };

    private static HorizontalStack KeyValue(string label, string value)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 0, 0, 4),
        };
        row.AddChild(new TextBlock
        {
            Text = label,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            TextColor = Muted,
        }, 2);
        row.AddChild(new TextBlock
        {
            Text = value,
            FontSize = 9.5f,
            TextColor = Ink,
        }, 3);
        return row;
    }

    private static TextBlock Th(string text, TextAlignment align) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 9,
        TextColor = Ink,
        Alignment = align,
    };

    private static TextBlock Td(string text, TextAlignment align, Color color, bool bold) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 9.5f,
        TextColor = color,
        Alignment = align,
    };

    private sealed record Panel(string Title, Analyte[] Rows);

    private sealed record Analyte(string Name, string Result, string Units, string Range, string Flag);
}
