using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Multi-Page Flow...");
                    BonniePDF.Examples.Examples._02_Layout.MultiPageFlowExample.Generate(Path.Combine(outputDir, "flow_demo.pdf"));
        Console.WriteLine("Generating Multi-Page Flow (Fluent API)...");
                    BonniePDF.Examples.Examples._02_Layout.MultiPageFlowExampleFluent.Generate(Path.Combine(outputDir, "flow_demo_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
