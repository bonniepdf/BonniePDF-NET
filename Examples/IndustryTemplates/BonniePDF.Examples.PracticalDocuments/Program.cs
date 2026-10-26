using BonniePDF;
using BonniePDF.Examples.PracticalDocuments;

string output = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
foreach (var (id, build) in new (string, Func<Document>)[] {
    ("large-order-invoice", LargeOrderInvoiceExample.Generate), ("long-basket-receipt", LongBasketReceiptExample.Generate),
    ("multilingual-customer-letter", MultilingualCustomerLetterExample.Generate) })
{
    using var document = build();
    await File.WriteAllBytesAsync(Path.Combine(output, id + ".pdf"), await document.GenerateBytesAsync());
    var pages = document.GenerateSvgPages();
    for (int i = 0; i < pages.Count; i++) await File.WriteAllTextAsync(Path.Combine(output, $"{id}-{i + 1}.svg"), pages[i]);
    Console.WriteLine($"{id}: {pages.Count} pages");
}
var totals = LargeOrderInvoiceExample.CalculateTotals([new("Example", 2, 1299, 150, 20), new("Zero rate", 1, 500, 0, 0)]);
if (totals.NetPence != 2948 || totals.TaxPence != 490 || totals.GrossPence != 3438) throw new InvalidOperationException("Invoice totals did not reconcile.");
if (LongBasketReceiptExample.CalculateTotal([new("Purchase", 2, 350, 50), new("Return", -1, 200, 0)]) != 450) throw new InvalidOperationException("Receipt totals did not reconcile.");
using var emptyInvoice = LargeOrderInvoiceExample.BuildInvoice([]);
await File.WriteAllBytesAsync(Path.Combine(output, "empty-invoice.pdf"), await emptyInvoice.GenerateBytesAsync());
