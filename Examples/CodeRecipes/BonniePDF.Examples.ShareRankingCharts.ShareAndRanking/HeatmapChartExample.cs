using BonniePDF;

namespace BonniePDF.Examples.ShareRankingCharts.ShareAndRanking;

[System.ComponentModel.DisplayName("Heatmap Chart")]
[System.ComponentModel.Category("Share & Ranking Charts")]
[System.ComponentModel.Description(
    "2D categorical matrix heatmap showing monthly cohort retention with linear colour interpolation, missing cells, continuous colour legend, and WCAG auto-contrast cell values.")]
public static class HeatmapChartExample
{
    public static Document Generate()
    {
        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = _ =>
            {
                HorizontalStack stack = new() { WidthDistribution = WidthDistribution.Weighted };
                stack.Add(new TextBlock
                {
                    Text = "BonniePDF - Heatmap Chart",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    Color = ColorPalette.Slate[500],
                }, null);
                stack.Add(PageNumber.Text().Font(StandardFonts.Helvetica, 10).Color(ColorPalette.Slate[500]).AlignRight(), 1f);
                return stack;
            },
        });

        document.Settings.Version = PdfVersion.Pdf17;
        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle().WithFamily(StandardFonts.Helvetica).WithSize(12).WithColor(ColorPalette.Slate[600]);

        document.Metadata.Title = "Heatmap Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = "Heatmap chart example - 2D cohort retention matrix with auto-contrast cell labels";

        document.Add(new TextBlock
        {
            Text = "Heatmap Chart: Monthly Cohort Retention",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });

        document.Add(new TextBlock
        {
            Text = "2D matrix colour ramp with missing cells, continuous legend, and WCAG auto-contrast values",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20),
        });

        document.Add(new HeatmapChart
        {
            Theme = "modern",
            Title = "Cohort Retention (%)",
            XCategories = ["M0", "M1", "M2", "M3", "M4", "M5"],
            YCategories = ["Jan", "Feb", "Mar", "Apr", "May"],
            Values =
            [
                [100m, 84m, 76m, 71m, 68m, 65m],
                [100m, 86m, 79m, 74m, 70m, null],
                [100m, 88m, 82m, 77m, null, null],
                [100m, 90m, 84m, null, null, null],
                [100m, 91m, null, null, null, null],
            ],
            ValueSuffix = "%",
            AltText = "Cohort retention heatmap from January to May across months M0 through M5, showing retention improving from 84% in January M1 to 91% in May M1.",
            Height = 380,
            StretchToFit = true,
            BackgroundColor = Color.White,
        });

        return document;
    }
}
