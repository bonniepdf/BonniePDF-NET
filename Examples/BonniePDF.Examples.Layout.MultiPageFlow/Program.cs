namespace BonniePDF.Examples.Layout.MultiPageFlow;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Multi-Page Flow...");
        using (var doc = MultiPageFlowExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "flow_demo.pdf"));
        }
        Console.WriteLine("Generating Multi-Page Flow (Fluent API)...");
        using (var doc = MultiPageFlowExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "flow_demo_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
