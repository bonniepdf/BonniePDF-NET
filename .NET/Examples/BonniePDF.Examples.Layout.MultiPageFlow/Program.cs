namespace BonniePDF.Examples.Layout.MultiPageFlow;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Multi-Page Flow...");
        MultiPageFlowExample.Generate(Path.Combine(outputDir, "flow_demo.pdf"));
        Console.WriteLine("Generating Multi-Page Flow (Fluent API)...");
        MultiPageFlowExampleFluent.Generate(Path.Combine(outputDir, "flow_demo_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}