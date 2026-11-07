using BonniePDF;
using BonniePDF.Examples.Hospitality;

string output = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
var assets = new Dictionary<string, byte[]>
{
    ["fonts/SourceSerif4-Regular.ttf"] = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", "SourceSerif4-Regular.ttf")),
};
foreach (var (id, build) in new (string, Func<Document>)[] {
    ("restaurant-menu", () => RestaurantMenuExample.Generate(assets)), ("allergen-matrix", () => AllergenMatrixExample.Generate(assets)),
    ("guest-folio", GuestFolioExample.Generate), ("banquet-event-order", BanquetEventOrderExample.Generate),
    ("gift-voucher", () => GiftVoucherExample.Generate(assets)) })
{
    using var document = build();
    await File.WriteAllBytesAsync(Path.Combine(output, id + ".pdf"), await document.GenerateBytesAsync());
    Console.WriteLine($"{id}: {document.GenerateSvgPages().Count} page(s)");
}
