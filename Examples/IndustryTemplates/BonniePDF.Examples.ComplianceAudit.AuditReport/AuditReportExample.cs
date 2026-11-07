using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.ComplianceAudit.AuditReport;

/// <summary>
/// Fictional EU/UK data-protection and information-security audit report with
/// executive summary, KPI tiles, control-status charts, findings register, and
/// confidential running header/footer.
/// </summary>
[System.ComponentModel.DisplayName("GDPR ISO 27001 Audit Report")]
[System.ComponentModel.Category("Compliance & Audit")]
[System.ComponentModel.Description(
    "Enterprise data-protection and security audit report with running confidential header/footer, KPI tiles, charts, findings register, and recommendations. PDF/A-2a + PDF/UA-1.")]
public static class AuditReportExample
{
    private static readonly Color Primary = ColorPalette.Slate[700];

    private static readonly Color Ink = ColorPalette.Slate[700];

    // 600, not 500: several usages read against the Slate-50 KPI panel, and 500
    // falls to 4.26:1 there against the 4.5:1 WCAG AA threshold.
    private static readonly Color Muted = ColorPalette.Slate[600];
    private static readonly Color Rule = ColorPalette.Slate[200];
    private static readonly Color HeaderBg = ColorPalette.Slate[100];

    private static readonly Finding[] Findings =
    [
        new("F-01", "Records of Processing (RoPA)", "GDPR Art. 30", Severity.High, "In progress"),
        new("F-02", "Data Protection Impact Assessment", "GDPR Art. 35", Severity.Critical, "Open"),
        new("F-03", "Access control review", "ISO A.5.15", Severity.High, "In progress"),
        new("F-04", "Encryption at rest", "ISO A.8.24", Severity.Medium, "Remediated"),
        new("F-05", "Personal-data breach procedure", "GDPR Art. 33-34", Severity.Critical, "Open"),
        new("F-06", "Processor contracts (DPAs)", "GDPR Art. 28", Severity.Medium, "In progress"),
        new("F-07", "Data retention schedule", "GDPR Art. 5(1)(e)", Severity.Medium, "Open"),
        new("F-08", "International transfers (SCCs / IDTA)", "GDPR Ch. V", Severity.High, "In progress"),
        new("F-09", "Security awareness training", "ISO A.6.3", Severity.Low, "Remediated"),
        new("F-10", "Logging & monitoring", "ISO A.8.15", Severity.Medium, "In progress"),
        new("F-11", "Supplier security assessment", "ISO A.5.19", Severity.Low, "Open"),
        new("F-12", "Subject access request (SAR) SLA", "GDPR Art. 12(3)", Severity.High, "Open"),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        string longDate = DateTime.Now.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        DocumentSettings settings = new(PdfSize.A4, 54)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            HeaderTemplate = pageNumber => pageNumber == 1
                ? null
                : BandRow("Sample Company - Audit AR-2026-014", "CONFIDENTIAL", ColorPalette.Rose[600]),
            FooterTemplate = pageNumber => BandRow("CONFIDENTIAL - not for distribution",
                $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", Muted),
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Data Protection & Information Security Audit - AR-2026-014";
        doc.Metadata.Author = "Sample Assurance LLP";
        doc.Metadata.Subject = "GDPR (EU/UK) and ISO/IEC 27001:2022 audit report (fictional).";

        AddTitlePage(doc, longDate);
        AddFindingsRegister(doc);
        AddRecommendations(doc);

        return doc;
    }

    private static void AddTitlePage(Document doc, string longDate)
    {
        doc.Add(new Container
        {
            BackgroundColor = Primary,
            Height = 3,
            Margin = new Thickness(0, 0, 0, 14),
        });

        doc.Add(Eyebrow("AUDIT REPORT - CONFIDENTIAL"));
        doc.Add(H1("Data Protection & Information Security Audit"));
        doc.Add(Body(
            "Sample Company - assessment against the EU General Data Protection " +
            "Regulation (2016/679), the UK GDPR / Data Protection Act 2018, and " +
            "ISO/IEC 27001:2022.",
            Muted));

        HorizontalStack meta = new()
            { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 4, 0, 20) };
        meta.Add(MetaItem("Report ref", "AR-2026-014"), 1);
        meta.Add(MetaItem("Audit period", "Apr-Jun 2026"), 1);
        meta.Add(MetaItem("Issued", longDate), 1);
        meta.Add(MetaItem("Auditor", "Sample Assurance LLP"), 1);
        doc.Add(meta);

        doc.Add(H2("Executive summary"));
        doc.Add(Body(
            "Sample Company demonstrates a maturing information-security programme. Of the 93 Annex A controls in scope, " +
            "71 are fully implemented and 15 are partially implemented. Twelve findings were raised, of which two are rated Critical " +
            "and require remediation before the next surveillance visit: the absence of a completed DPIA for high-risk processing, " +
            "and gaps in the personal-data breach notification procedure against the 72-hour deadline."));

        HorizontalStack kpis = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 12, Margin = new Thickness(0, 6, 0, 20) };
        kpis.Add(Kpi("82%", "Overall conformance", Primary), 1);
        kpis.Add(Kpi("71 / 93", "Controls implemented", Primary), 1);
        kpis.Add(Kpi("12", "Open findings", ColorPalette.Amber[700]), 1);
        kpis.Add(Kpi("2", "Critical", ColorPalette.Rose[600]), 1);
        doc.Add(kpis);

        VerticalStack charts = new() { Spacing = 12, Margin = new Thickness(0, 0, 0, 6) };
        charts.Add(ChartCard("Control status (93 controls)", ControlDonut()));
        charts.Add(ChartCard("Findings by severity", SeverityBars()));
        doc.Add(charts);
    }

    private static void AddFindingsRegister(Document doc)
    {
        doc.AddPage();
        doc.Add(H2("Findings register"));
        doc.Add(Body("Each finding maps to a control or regulatory requirement and a remediation status.", Muted, 10));

        Table table = new(0.6f, 2.6f, 1.4f, 1, 1.1f)
        {
            CellPadding = 8,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Primary,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(Th("ID"), Th("Area"), Th("Requirement"), Th("Severity"), Th("Status"));

        foreach (Finding finding in Findings)
        {
            table.AddRow(
                Td(finding.Id, Ink),
                Td(finding.Area, Ink),
                Td(finding.Requirement, Muted),
                SeverityCell(finding.Severity),
                // Emerald[700], not [600]: regular 9pt text on white needs 4.5:1 and
                // 600 falls to 3.65:1.
                Td(finding.Status,
                    string.Equals(finding.Status, "Remediated", StringComparison.Ordinal)
                        ? ColorPalette.Emerald[700]
                        : Ink));
        }

        doc.Add(table);
    }

    private static void AddRecommendations(Document doc)
    {
        doc.Add(H2("Priority recommendations"));
        doc.Add(Body("1. Complete and sign off the outstanding DPIA (F-02) for high-risk profiling before go-live."));
        doc.Add(Body(
            "2. Rework the breach-response runbook (F-05) to guarantee supervisory-authority notification within 72 hours."));
        doc.Add(Body(
            "3. Establish a Subject Access Request workflow (F-12) that meets the one-month statutory deadline."));

        doc.Add(H2("Scope & methodology"));
        doc.Add(Body(
            "The audit combined documentation review, control walk-throughs, and sample testing across the in-scope processing activities. " +
            "Conformance is assessed against ISO/IEC 27001:2022 Annex A and the accountability obligations of the EU and UK GDPR. " +
            "This report is confidential to Sample Company."));
    }

    private static PieChart ControlDonut() => new(ControlData())
    {
        AltText = "Donut chart: 71 compliant, 15 partial, 5 non-conformant, 2 not applicable of 93 controls.",
        Height = 130,
        StretchToFit = true,
        IsDonut = true,
        TotalLabel = "Controls",
        ShowLegend = true,
        Colors = ["#059669", "#f59e0b", "#e11d48", "#94a3b8"],
        BackgroundColor = Color.White,
    };

    private static ChartData ControlData() => new()
    {
        Series =
        [
            new ChartSeries
            {
                DataPoints =
                [
                    new ChartDataPoint { Label = "Compliant", Value = 71 },
                    new ChartDataPoint { Label = "Partial", Value = 15 },
                    new ChartDataPoint { Label = "Non-conformant", Value = 5 },
                    new ChartDataPoint { Label = "Not applicable", Value = 2 },
                ],
            },
        ],
    };

    private static BarChart SeverityBars() => new(SeverityData())
    {
        AltText = "Column chart of findings by severity: 2 Critical, 4 High, 4 Medium, 2 Low.",
        Height = 130,
        StretchToFit = true,
        BarColor = Primary,
        FontFamily = StandardFonts.Helvetica,
        LabelFontSize = 8,
        BackgroundColor = Color.White,
    };

    private static ChartData SeverityData()
    {
        Severity[] order = [Severity.Critical, Severity.High, Severity.Medium, Severity.Low];
        ChartSeries series = new();
        foreach (Severity severity in order)
        {
            series.DataPoints.Add(new ChartDataPoint
            {
                Label = severity.ToString(),
                Value = Findings.Count(f => f.Severity == severity),
            });
        }

        return new ChartData
        {
            Series = [series],
            YAxis = new AxisOptions { MaxValue = 6, StepCount = 3 },
        };
    }

    private static HorizontalStack BandRow(string left, string right, Color rightColor)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(new TextBlock { Text = left, FontName = StandardFonts.Helvetica, FontSize = 8, Color = Muted }, 3);
        row.Add(
            new TextBlock
            {
                Text = right, FontName = StandardFonts.HelveticaBold, FontSize = 8, Color = rightColor,
                Alignment = TextAlignment.Right
            }, 2);
        return row;
    }

    private static TextBlock Eyebrow(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 9,
        Color = Primary,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 22,
        Color = ColorPalette.Slate[800],
        Margin = new Thickness(0, 0, 0, 6),
    };

    private static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        Color = Primary,
        Margin = new Thickness(0, 8, 0, 8),
    };

    private static TextBlock Body(string text, Color? color = null, float mb = 10) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 10,
        Color = color ?? Ink,
        Alignment = TextAlignment.Justify,
        Margin = new Thickness(0, 0, 0, mb),
    };

    private static VerticalStack MetaItem(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(), FontName = StandardFonts.HelveticaBold, FontSize = 7, Color = Muted,
            Margin = new Thickness(0, 0, 0, 2)
        });
        stack.Add(new TextBlock { Text = value, FontName = StandardFonts.Helvetica, FontSize = 10, Color = Ink });
        return stack;
    }

    private static Container Kpi(string value, string label, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = value, FontName = StandardFonts.HelveticaBold, FontSize = 20, Color = accent,
            Margin = new Thickness(0, 0, 0, 3)
        });
        stack.Add(new TextBlock { Text = label, FontName = StandardFonts.Helvetica, FontSize = 8.5f, Color = Muted });
        return new Container
            { BackgroundColor = ColorPalette.Slate[50], CornerRadius = 8, Padding = new Thickness(14, 12, 14, 12), Child = stack };
    }

    private static Container ChartCard(string title, IElement chart)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 11,
            Color = Primary,
            Margin = new Thickness(0, 0, 0, 8),
        });
        stack.Add(chart);
        return new Container
        {
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 8,
            ClipContent = true,
            Padding = new Thickness(14, 14, 14, 14),
            Child = stack,
        };
    }

    private static TextBlock Th(string text) => new()
        { Text = text, FontName = StandardFonts.HelveticaBold, FontSize = 9, Color = Color.White };

    private static TextBlock Td(string text, Color color) => new()
        { Text = text, FontName = StandardFonts.Helvetica, FontSize = 9, Color = color };

    private static TextBlock SeverityCell(Severity severity) => new()
    {
        Text = severity.ToString(),
        FontName = StandardFonts.HelveticaBold,
        FontSize = 9,
        Color = SeverityColor(severity),
    };

    // Bold 9pt text painted directly on the white row background, so each shade
    // has to clear 4.5:1 on its own - the 500-level series colour that reads fine
    // as a chart fill is too light as text (Amber/Emerald 500 fall to 2.13/2.47:1,
    // Blue 500 to 3.76:1). Critical's Rose[600] already cleared it (4.53:1).
    private static Color SeverityColor(Severity severity) => severity switch
    {
        Severity.Critical => ColorPalette.Rose[600],
        Severity.High => ColorPalette.Amber[700],
        Severity.Medium => ColorPalette.Blue[600],
        _ => ColorPalette.Emerald[700],
    };

    private enum Severity
    {
        Critical,
        High,
        Medium,
        Low
    }

    private sealed record Finding(string Id, string Area, string Requirement, Severity Severity, string Status);
}
