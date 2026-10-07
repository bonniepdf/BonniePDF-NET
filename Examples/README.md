# BonniePDF examples

Each example's document-building file contains its own sample data and helpers.
You can copy it into a .NET 8 project with a reference to `BonniePDF` and implicit
usings enabled. The `Program.cs` files are runners that save the generated PDFs.

Start with `BonniePDF.Examples.Basics.SimpleDocument/SimpleDocumentExample.cs`.
Other recipes introduce individual elements; the templates combine them into
invoices, reports, letters, and other complete documents.

Declare your application's licence tier, build a document, and export it:

```csharp
BonniePdfLicense.Tier = LicenseTier.Evaluation; // Use your application's applicable tier.
Document document = SimpleDocumentExample.Generate();
File.WriteAllBytes("example.pdf", document.GenerateBytes());
```

Import `BonniePDF` and the copied example's namespace. The example projects
declare the evaluation tier automatically through `ExampleLicenseSetup.cs`.
Photos, custom fonts, novels, and attachments remain explicit asset inputs:
examples either accept their bytes or identify the files they read. The projects
copy bundled assets to the output directory; use your own asset loading when
copying these examples into an application.

Local helpers sit below the main document-building method. They belong to that
example and can be adapted without changing another example.
