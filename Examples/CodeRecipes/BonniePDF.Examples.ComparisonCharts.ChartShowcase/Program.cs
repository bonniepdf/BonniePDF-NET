namespace BonniePDF.Examples.ComparisonCharts.ChartShowcase;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Charting Examples...");
        using (var doc = ChartShowcaseExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "charting_example.pdf"));
        }

        Console.WriteLine("Generating Bar Chart...");
        using (var doc = BarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "bar_chart_example.pdf"));
        }

        Console.WriteLine("Generating Grouped Bar Chart...");
        using (var doc = GroupedBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "grouped_bar_chart_example.pdf"));
        }

        Console.WriteLine("Generating Horizontal Bar Chart...");
        using (var doc = HorizontalBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_bar_chart_example.pdf"));
        }

        Console.WriteLine("Generating Horizontal Grouped Bar Chart...");
        using (var doc = HorizontalGroupedBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_grouped_bar_chart_example.pdf"));
        }

        Console.WriteLine("Generating Stacked Bar Chart...");
        using (var doc = StackedBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "stacked_bar_chart_example.pdf"));
        }

        Console.WriteLine("Generating Combo Chart...");
        using (var doc = ComboChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "combo_chart_example.pdf"));
        }

        Console.WriteLine("Generating Waterfall Chart...");
        using (var doc = WaterfallChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "waterfall_chart_example.pdf"));
        }

        Console.WriteLine("Generating Bullet Chart...");
        using (var doc = BulletChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "bullet_chart_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}