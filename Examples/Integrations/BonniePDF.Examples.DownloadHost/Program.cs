using BonniePDF;
using BonniePDF.Examples.PracticalRecipes;
using BonniePDF.Examples.PracticalDocuments;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
var app = builder.Build();
// Load the application assets once. The example assembly declares Evaluation for this runner.
byte[] font = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSans-Regular.ttf"));
byte[] photo = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "images", "products", "terracotta-planter.jpg"));

app.MapGet("/download", async (HttpContext context) =>
{
    using var document = BrowserDownloadExample.Build(font, photo);
    byte[] bytes = await document.GenerateBytesAsync(cancellationToken: context.RequestAborted);
    if (document.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)) return Results.Problem("PDF generation reported a problem.");
    context.Response.Headers.CacheControl = "no-store";
    return Results.File(bytes, "application/pdf", "browser-download.pdf");
});

app.MapPost("/invoice", async (List<LargeOrderInvoiceExample.InvoiceLine> lines, HttpContext context) =>
{
    if (lines.Count > 200 || lines.Exists(line => line is null || string.IsNullOrEmpty(line.Description) || line.Description.Length > 2000)) return Results.BadRequest("Invalid invoice data.");
    Document document;
    try { document = LargeOrderInvoiceExample.BuildInvoice(lines); }
    catch (Exception error) when (error is ArgumentException or OverflowException) { return Results.BadRequest("Invalid invoice amounts."); }
    using (document)
    {
        byte[] bytes = await document.GenerateBytesAsync(cancellationToken: context.RequestAborted);
        if (document.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)) return Results.Problem("PDF generation reported a problem.");
        context.Response.Headers.CacheControl = "no-store";
        return Results.File(bytes, "application/pdf", "invoice.pdf");
    }
});

await app.RunAsync();
