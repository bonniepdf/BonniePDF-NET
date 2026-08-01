using System.Globalization;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.AuditReport;

/// <summary>
/// Fictional EU/UK data-protection and information-security audit report with
/// executive summary, KPI tiles, control-status charts, findings register, and
/// confidential running header/footer.
/// </summary>
[System.ComponentModel.DisplayName("Compliance Audit Report (GDPR / ISO 27001)")]
[System.ComponentModel.Category("Compliance")]
[System.ComponentModel.Description("Enterprise data-protection and security audit report with running confidential header/footer, KPI tiles, charts, findings register, and recommendations. PDF/A-2a + PDF/UA-1.")]
public static class AuditReportExample
{
    private static readonly Color Primary = ColorPalette.Indigo[700];
    private static readonly Color Ink = ColorPalette.Slate[700];
    private static readonly Color Muted = ColorPalette.Slate[500];
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
            FooterTemplate = pageNumber => BandRow("CONFIDENTIAL - not for distribution", $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", Muted),
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Data Protection & Information Security Audit - AR-2026-014";
        doc.Metadata.Author = "Aegis Assurance LLP";
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
            Padding = new Thickness(0, 3, 0, 0),
            Margin = new Thickness(0, 0, 0, 14),
            Child = ThinSpacer(),
        });

        doc.Add(Eyebrow("AUDIT REPORT - CONFIDENTIAL"));
        doc.Add(H1("Data Protection & Information Security Audit"));
        doc.Add(Body(
            "Sample Company - assessment against the EU General Data Protection " +
            "Regulation (2016/679), the UK GDPR / Data Protection Act 2018, and " +
            "ISO/IEC 27001:2022.",
            Muted));

        HorizontalStack meta = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 4, 0, 20) };
        meta.AddChild(MetaItem("Report ref", "AR-2026-014"), 1);
        meta.AddChild(MetaItem("Audit period", "Apr-Jun 2026"), 1);
        meta.AddChild(MetaItem("Issued", longDate), 1);
        meta.AddChild(MetaItem("Auditor", "Aegis Assurance LLP"), 1);
        doc.Add(meta);

        doc.Add(H2("Executive summary"));
        doc.Add(Body(
            "Sample Company demonstrates a maturing information-security programme. Of the 93 Annex A controls in scope, " +
            "71 are fully implemented and 15 are partially implemented. Twelve findings were raised, of which two are rated Critical " +
            "and require remediation before the next surveillance visit: the absence of a completed DPIA for high-risk processing, " +
            "and gaps in the personal-data breach notification procedure against the 72-hour deadline."));

        Grid kpis = new(1, 1, 1, 1) { Border = Border.None, CellPadding = 0, Margin = new Thickness(0, 6, 0, 20) };
        kpis.AddRow(
            Kpi("82%", "Overall conformance", Primary),
            Kpi("71 / 93", "Controls implemented", Primary),
            Kpi("12", "Open findings", ColorPalette.Amber[600]),
            Kpi("2", "Critical", ColorPalette.Rose[600]));
        doc.Add(kpis);

        doc.Add(H2("Control status"));
        doc.Add(new ChartElement(ControlDonut(), ControlData())
        {
            AltText = "Donut chart: 71 compliant, 15 partial, 5 non-conformant, 2 not applicable of 93 controls.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(H2("Findings by severity"));
        doc.Add(new ChartElement(SeverityBars(), SeverityData())
        {
            AltText = "Column chart of findings by severity: 2 Critical, 4 High, 4 Medium, 2 Low.",
        });
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
                Td(finding.Status, string.Equals(finding.Status, "Remediated", StringComparison.Ordinal) ? ColorPalette.Emerald[600] : Ink));
        }

        doc.Add(table);
    }

    private static void AddRecommendations(Document doc)
    {
        doc.Add(H2("Priority recommendations"));
        doc.Add(Body("1. Complete and sign off the outstanding DPIA (F-02) for high-risk profiling before go-live."));
        doc.Add(Body("2. Rework the breach-response runbook (F-05) to guarantee supervisory-authority notification within 72 hours."));
        doc.Add(Body("3. Establish a Subject Access Request workflow (F-12) that meets the one-month statutory deadline."));

        doc.Add(H2("Scope & methodology"));
        doc.Add(Body(
            "The audit combined documentation review, control walk-throughs, and sample testing across the in-scope processing activities. " +
            "Conformance is assessed against ISO/IEC 27001:2022 Annex A and the accountability obligations of the EU and UK GDPR. " +
            "This report is confidential to Sample Company."));
    }

    private static PieChart ControlDonut() => new()
    {
        Height = 180,
        StretchToFit = true,
        IsDonut = true,
        TotalLabel = "Controls",
        ShowLegend = true,
        Colors = ["#059669", "#f59e0b", "#e11d48", "#94a3b8"],
        BackgroundColor = Color.White,
    };

    private static ChartData ControlData() => new()
    {
        Title = "Control status",
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

    private static BarChart SeverityBars() => new()
    {
        Height = 180,
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
            Title = "Findings by severity",
            Series = [series],
            YAxis = new AxisOptions { MaxValue = 6, StepCount = 3 },
        };
    }

    private static HorizontalStack BandRow(string left, string right, Color rightColor)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.AddChild(new TextBlock { Text = left, FontName = StandardFonts.Helvetica, FontSize = 8, TextColor = Muted }, 3);
        row.AddChild(new TextBlock { Text = right, FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = rightColor, Alignment = TextAlignment.Right }, 2);
        return row;
    }

    private static TextBlock Eyebrow(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 9,
        TextColor = Primary,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 22,
        TextColor = ColorPalette.Slate[800],
        Margin = new Thickness(0, 0, 0, 6),
    };

    private static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        TextColor = Primary,
        Margin = new Thickness(0, 8, 0, 8),
    };

    private static TextBlock Body(string text, Color? color = null, float mb = 10) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 10,
        TextColor = color ?? Ink,
        Alignment = TextAlignment.Justify,
        Margin = new Thickness(0, 0, 0, mb),
    };

    private static VerticalStack MetaItem(string label, string value)
    {
        VerticalStack stack = new();
        stack.AddChild(new TextBlock { Text = label.ToUpperInvariant(), FontName = StandardFonts.HelveticaBold, FontSize = 7, TextColor = Muted, Margin = new Thickness(0, 0, 0, 2) });
        stack.AddChild(new TextBlock { Text = value, FontName = StandardFonts.Helvetica, FontSize = 10, TextColor = Ink });
        return stack;
    }

    private static Container Kpi(string value, string label, Color accent)
    {
        VerticalStack stack = new();
        stack.AddChild(new TextBlock { Text = value, FontName = StandardFonts.HelveticaBold, FontSize = 20, TextColor = accent, Margin = new Thickness(0, 0, 0, 3) });
        stack.AddChild(new TextBlock { Text = label, FontName = StandardFonts.Helvetica, FontSize = 8.5f, TextColor = Muted });
        return new Container { BackgroundColor = ColorPalette.Indigo[50], Padding = new Thickness(14, 12, 14, 12), Child = stack };
    }

    private static TextBlock Th(string text) => new() { Text = text, FontName = StandardFonts.HelveticaBold, FontSize = 9, TextColor = Color.White };

    private static TextBlock Td(string text, Color color) => new() { Text = text, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = color };

    private static TextBlock SeverityCell(Severity severity) => new()
    {
        Text = severity.ToString(),
        FontName = StandardFonts.HelveticaBold,
        FontSize = 9,
        TextColor = SeverityColor(severity),
    };

    private static Color SeverityColor(Severity severity) => severity switch
    {
        Severity.Critical => ColorPalette.Rose[600],
        Severity.High => ColorPalette.Amber[500],
        Severity.Medium => ColorPalette.Blue[500],
        _ => ColorPalette.Emerald[500],
    };

    private static TextBlock ThinSpacer() => new() { Text = " ", FontSize = 1 };

    private enum Severity { Critical, High, Medium, Low }

    private sealed record Finding(string Id, string Area, string Requirement, Severity Severity, string Status);
}
