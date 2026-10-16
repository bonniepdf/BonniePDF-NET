using BonniePDF.Examples.Typography.InternationalTypography;
string output = Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
using var doc = InternationalTypographyExample.Generate();
doc.Save(Path.Combine(output, "international-typography.pdf"));
File.WriteAllText(Path.Combine(output, "international-typography.svg"), doc.GenerateSvgPages()[0]);
Console.WriteLine(output);
