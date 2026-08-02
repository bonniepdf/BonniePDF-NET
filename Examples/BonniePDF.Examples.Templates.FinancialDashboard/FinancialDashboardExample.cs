using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.FinancialDashboard;

/// <summary>
/// Generates a single-page landscape financial dashboard with KPI cards,
/// a 12-month revenue bar chart, and a top-products table.
/// </summary>
[System.ComponentModel.DisplayName("Financial Dashboard")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Landscape A4 financial dashboard with KPI strip, 12-month revenue bar chart with alt text, and top-products table.")]
public static class FinancialDashboardExample
{
    // ── Brand colours - drawn from the BonniePDF logo palette ────────────────
    private static readonly Color Espresso = Color.FromHex("#442714");
    private static readonly Color Terra = Color.FromHex("#956b51");
    private static readonly Color Brown = Color.FromHex("#805e48");
    private static readonly Color Sand = Color.FromHex("#d2c8b7");
    private static readonly Color Cream = Color.FromHex("#f4f1ef");
    private static readonly Color HeaderMute = Color.FromHex("#b99a88");
    private static readonly Color RowAlt = Color.FromHex("#e8e0d8");
    private static readonly Color DeltaPos = Color.FromHex("#2d7a3a");
    private static readonly Color DeltaNeg = Color.FromHex("#b94040");

    public static Document Generate()
    {
        // Landscape, zero margin - layout elements carry their own padding
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 0,
            PageColor = Cream
        });

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle()
            .WithFamily("Helvetica")
            .WithSize(9)
            .WithColor(ColorPalette.Slate[600]);

        DateTime today = DateTime.Now;
        int currentQuarter = (today.Month - 1) / 3 + 1;
        int quarter = currentQuarter == 1 ? 4 : currentQuarter - 1;
        int year = currentQuarter == 1 ? today.Year - 1 : today.Year;
        string quarterLabel = $"Q{quarter} {year}";
        string title = $"{quarterLabel} Financial Report";
        string generated = $"Generated {today:d MMMM yyyy}";

        doc.Metadata.Title = title;
        doc.Metadata.Author = "Sample Company";
        doc.Metadata.Subject = $"{quarterLabel} financial dashboard - KPIs, monthly revenue chart, and product breakdown";

        // ════════════════════════════════════════════════════════════════════════
        // 1.  Header bar
        // ════════════════════════════════════════════════════════════════════════

        VerticalStack companyStack = new();
        companyStack.AddChild(new TextBlock
        {
            Text = "Sample Company",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Cream
        });
        companyStack.AddChild(new TextBlock
        {
            Text = "Confidential - Internal Use Only",
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            TextColor = HeaderMute,
            Margin = new Thickness(0, 5, 0, 0)
        });

        VerticalStack reportStack = new();
        reportStack.AddChild(new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            TextColor = Cream,
            Alignment = TextAlignment.Right
        });
        reportStack.AddChild(new TextBlock
        {
            Text = generated,
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            TextColor = HeaderMute,
            Alignment = TextAlignment.Right,
            Margin = new Thickness(0, 5, 0, 0)
        });

        Grid headerLayout = new(1f, 1f)
        {
            Border = Border.None,
            CellPadding = 0
        };
        headerLayout.AddRow(
            companyStack,
            reportStack
        );

        doc.Add(new Container
        {
            BackgroundColor = Espresso,
            Padding = new Thickness(24, 20, 24, 20),
            Child = headerLayout
        });

        // ════════════════════════════════════════════════════════════════════════
        // 2.  KPI strip - four equal-width metric cards
        // ════════════════════════════════════════════════════════════════════════

        Container totalRevenueCell = CreateKpiCard("£2.41M", "TOTAL REVENUE", "↑ 18.3% YoY", DeltaPos, false);
        Container grossProfitCell = CreateKpiCard("£640K", "GROSS PROFIT", "↑ 11.7% YoY", DeltaPos, true);
        Container newCustomersCell = CreateKpiCard("3,814", "NEW CUSTOMERS", "↑ 24.1% YoY", DeltaPos, true);
        Container satisfactionCell = CreateKpiCard("94.2%", "SATISFACTION", "↓ 0.8%", DeltaNeg, true);

        Grid kpiTable = new(1f, 1f, 1f, 1f)
        {
            Border = Border.None,
            CellPadding = 0
        };
        kpiTable.AddRow(totalRevenueCell, grossProfitCell, newCustomersCell, satisfactionCell);
        doc.Add(kpiTable);

        // KPI rule
        doc.Add(new Container
        {
            BackgroundColor = Sand,
            Padding = new Thickness(0, 0.75f, 0, 0),
            Child = new TextBlock { Text = " ", FontSize = 1 }
        });

        // ════════════════════════════════════════════════════════════════════════
        // 3.  Monthly revenue bar chart
        // ════════════════════════════════════════════════════════════════════════

        doc.Add(new Container
        {
            Padding = new Thickness(24, 14, 24, 6),
            Child = new TextBlock
            {
                Text = "MONTHLY REVENUE  (£000s)",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 7,
                TextColor = Terra
            }
        });

        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        decimal[] revenue = [125, 146, 140, 175, 163, 190, 197, 182, 211, 226, 240, 233];

        ChartSeries series = new() { Color = Terra };
        for (int i = 0; i < months.Length; i++)
        {
            series.DataPoints.Add(new ChartDataPoint { Label = months[i], Value = revenue[i] });
        }

        ChartData chartData = new()
        {
            Series = [series],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                MaxValue = 250,
                StepCount = 5
            }
        };

        BarChart barChart = new(chartData)
        {
            AltText = "Bar chart of monthly revenue in thousands, ranging from 125 in January to 233 in December.",
            Height = 200,
            StretchToFit = true,
            BarColor = Terra,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 8
        };

        // Charts are document elements and participate in normal width resolution.
        doc.Add(barChart);

        // ════════════════════════════════════════════════════════════════════════
        // 4.  Top products table
        // ════════════════════════════════════════════════════════════════════════

        // Product rule
        doc.Add(new Container
        {
            BackgroundColor = Sand,
            Padding = new Thickness(0, 0.75f, 0, 0),
            Child = new TextBlock { Text = " ", FontSize = 1 }
        });

        doc.Add(new Container
        {
            Padding = new Thickness(24, 14, 24, 6),
            Child = new TextBlock
            {
                Text = "TOP PRODUCTS",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 7,
                TextColor = Terra
            }
        });

        Table productTable = new(2.2f, 1f, 1.4f, 0.8f)
        {
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Sand),
            CellPadding = 8,
            HeaderBackgroundColor = RowAlt,
            HeaderTextColor = Espresso
        };

        productTable.AddHeader(
            new TextBlock { Text = "Product", FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = Espresso, Alignment = TextAlignment.Left },
            new TextBlock { Text = "Units", FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = Espresso, Alignment = TextAlignment.Right },
            new TextBlock { Text = "Revenue", FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = Espresso, Alignment = TextAlignment.Right },
            new TextBlock { Text = "Margin", FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = Espresso, Alignment = TextAlignment.Right }
        );

        var products = new[]
        {
            new { Name = "Enterprise Plan", Units = "142",   Revenue = "£890,400", Margin = "71%" },
            new { Name = "Pro Plan",        Units = "1,204", Revenue = "£722,400", Margin = "64%" },
            new { Name = "Starter Plan",    Units = "2,468", Revenue = "£493,600", Margin = "58%" },
            new { Name = "Add-ons",         Units = "-",     Revenue = "£304,000", Margin = "82%" },
        };

        foreach (var p in products)
        {
            productTable.AddRow(
                new TextBlock { Text = p.Name, FontName = StandardFonts.Helvetica, FontSize = 8, TextColor = Espresso, Alignment = TextAlignment.Left },
                new TextBlock { Text = p.Units, FontName = StandardFonts.Helvetica, FontSize = 8, TextColor = Brown, Alignment = TextAlignment.Right },
                new TextBlock { Text = p.Revenue, FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = Espresso, Alignment = TextAlignment.Right },
                new TextBlock { Text = p.Margin, FontName = StandardFonts.Helvetica, FontSize = 8, TextColor = Brown, Alignment = TextAlignment.Right }
            );
        }

        doc.Add(new Container
        {
            Padding = new Thickness(24, 0, 24, 20),
            Child = productTable
        });

        return doc;
    }

    private static Container CreateKpiCard(string value, string label, string delta, Color deltaColor, bool showLeftBorder)
    {
        VerticalStack stack = new();
        stack.AddChild(new TextBlock
        {
            Text = value,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Espresso
        });
        stack.AddChild(new TextBlock
        {
            Text = label,
            FontName = StandardFonts.Helvetica,
            FontSize = 7,
            TextColor = Brown,
            Margin = new Thickness(0, 4, 0, 0)
        });
        stack.AddChild(new TextBlock
        {
            Text = delta,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7,
            TextColor = deltaColor,
            Margin = new Thickness(0, 4, 0, 0)
        });

        Container cell = new()
        {
            Child = stack,
            Padding = new Thickness(20, 16, 20, 16)
        };

        if (showLeftBorder)
        {
            cell.Border = Border.LeftOnly(0.75f);
            cell.BorderColors = BorderColors.LeftOnly(Sand);
        }

        return cell;
    }
}
