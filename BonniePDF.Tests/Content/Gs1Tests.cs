using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Content;

public class Gs1Tests
{
    [Fact]
    public void ReproducesTheModulesIndependentlyDecodedAsC1AndD2()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Definition", "Fixtures", "valid", "template-authoring.json");
        using var fixture = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement gs1 = fixture.RootElement.GetProperty("_expect").GetProperty("gs1");
        string payload = gs1.GetProperty("payload").GetString()!;
        static string Hash(IEnumerable<bool> modules) =>
            Convert.ToHexString(SHA256.HashData(modules.Select(bit => bit ? (byte)1 : (byte)0).ToArray())).ToLowerInvariant();
        Assert.Equal(gs1.GetProperty("code128").GetString(), Hash(BarcodeEncoder.Encode(BarcodeSymbology.Code128, payload, gs1: true).Modules));
        Assert.Equal(gs1.GetProperty("dataMatrix").GetString(), Hash(DataMatrixEncoder.Encode(payload, gs1: true).Modules.Cast<bool>()));
        Assert.Throws<DataMatrixContentException>(() => DataMatrixEncoder.Encode(payload + '\n', gs1: true));
    }
}
