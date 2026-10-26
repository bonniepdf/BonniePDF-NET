using BonniePDF;
using BonniePDF.Examples.PracticalRecipes;

string output = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
foreach (var (id, build) in new (string, Func<Document>)[] {
    ("page-break-control", PageBreakControlExample.Generate), ("practical-tables", PracticalTablesExample.Generate),
    ("browser-download", BrowserDownloadExample.Generate), ("font-fallback", FontFallbackExample.Generate),
    ("document-diagnostics", DocumentDiagnosticsExample.Generate) })
{
    using var document = build();
    await File.WriteAllBytesAsync(Path.Combine(output, id + ".pdf"), await document.GenerateBytesAsync());
    var pages = document.GenerateSvgPages();
    for (int i = 0; i < pages.Count; i++) await File.WriteAllTextAsync(Path.Combine(output, $"{id}-{i + 1}.svg"), pages[i]);
    Console.WriteLine($"{id}: {pages.Count} pages");
}
await FontFallbackExample.VerifyMissingCharacterAsync();
await DocumentDiagnosticsExample.VerifyDiagnosticsAsync();

var batchDocuments = new System.Collections.Concurrent.ConcurrentBag<Document>();
Document BuildRecord(int record)
{
    var document = new Document(); document.Metadata.Title = $"Batch {record}";
    document.Add(new TextBlock($"Record {record}")); batchDocuments.Add(document); return document;
}
int completed = await AsyncBatchExample.GenerateAsync(Enumerable.Range(1, 4), BuildRecord,
    (_, bytes, _) => bytes.Length > 100 ? Task.CompletedTask : throw new InvalidOperationException("Empty PDF."), concurrency: 2);
if (completed != 4 || batchDocuments.Any(doc => doc.Pages.Count != 0)) throw new InvalidOperationException("Batch cleanup failed.");
try
{
    await AsyncBatchExample.GenerateAsync(Enumerable.Range(1, 3), BuildRecord,
        (_, _, _) => throw new IOException("Writer failed"), concurrency: 2);
    throw new InvalidOperationException("Writer failure did not propagate.");
}
catch (IOException) { }
if (batchDocuments.Any(doc => doc.Pages.Count != 0)) throw new InvalidOperationException("Failed batch leaked a document.");
using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
try
{
    await AsyncBatchExample.GenerateAsync(Enumerable.Range(1, 1), BuildRecord, (_, _, _) => Task.CompletedTask, cancellationToken: cancellation.Token);
    throw new InvalidOperationException("Cancellation did not propagate.");
}
catch (OperationCanceledException) { }
