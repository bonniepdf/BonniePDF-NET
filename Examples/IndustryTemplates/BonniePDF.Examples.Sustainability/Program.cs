using BonniePDF;
using BonniePDF.Examples.Sustainability;

string output = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
foreach (var (id, build) in new (string, Func<Document>)[] {
    ("carbon-emissions-report", CarbonEmissionsReportExample.Generate), ("energy-performance-certificate", EnergyPerformanceCertificateExample.Generate) })
{
    using var document = build();
    await File.WriteAllBytesAsync(Path.Combine(output, id + ".pdf"), await document.GenerateBytesAsync());
    Console.WriteLine($"{id}: {document.GenerateSvgPages().Count} page(s)");
}
