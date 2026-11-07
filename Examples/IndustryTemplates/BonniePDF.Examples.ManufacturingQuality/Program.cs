using BonniePDF;
using BonniePDF.Examples.ManufacturingQuality;

string output = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
foreach (var (id, build) in new (string, Func<Document>)[] {
    ("certificate-of-analysis", CertificateOfAnalysisExample.Generate), ("safety-data-sheet", SafetyDataSheetExample.Generate),
    ("final-inspection-record", FinalInspectionRecordExample.Generate), ("pallet-label", PalletLabelExample.Generate) })
{
    using var document = build();
    await File.WriteAllBytesAsync(Path.Combine(output, id + ".pdf"), await document.GenerateBytesAsync());
    Console.WriteLine($"{id}: {document.GenerateSvgPages().Count} page(s)");
}
