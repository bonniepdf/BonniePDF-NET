namespace BonniePDF.Examples.BillingPayments.BillingDocuments;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating trade document examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        (string File, Func<BonniePDF.Document> Generate)[] examples =
        [
            ("credit-note.pdf", CreditNoteExample.Generate),
            ("dunning-letter.pdf", DunningLetterExample.Generate),
            ("remittance-advice.pdf", RemittanceAdviceExample.Generate),
            ("customer-account-statement.pdf", CustomerAccountStatementExample.Generate),
        ];

        foreach ((string file, Func<BonniePDF.Document> generate) in examples)
        {
            Console.WriteLine($"Generating {file}...");
            using BonniePDF.Document doc = generate();
            doc.Save(Path.Combine(outputDir, file));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}