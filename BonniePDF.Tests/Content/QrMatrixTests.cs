using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Content;
public class QrMatrixTests
{
    public static IEnumerable<object[]> Vectors()
    {
        using JsonDocument data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content", "Fixtures", "qr-matrices.json")));
        foreach (JsonElement item in data.RootElement.GetProperty("cases").EnumerateArray())
            yield return[item.GetProperty("text").GetString()!, item.GetProperty("requestedCorrection").GetString()!, item.GetProperty("version").GetInt32(), item.GetProperty("mask").GetInt32(), item.GetProperty("sha256").GetString()!];
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void MatchesIndependentMatrix(string text, string requestedCorrection, int version, int mask, string sha256)
    {
        QrEcc ecl = requestedCorrection switch
        {
            "low" => QrEcc.Low,
            "quartile" => QrEcc.Quartile,
            "high" => QrEcc.High,
            _ => QrEcc.Medium
        };
        QrCodeEncoder qr = QrCodeEncoder.EncodeText(text, ecl);
        bool[, ] matrix = qr.ToMatrix();
        string rows = string.Join("\n", Enumerable.Range(0, qr.Size).Select(y => new string (Enumerable.Range(0, qr.Size).Select(x => matrix[y, x] ? '1' : '0').ToArray())));
        Assert.Equal(version, qr.Version);
        Assert.Equal(mask, qr.Mask);
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(rows))).ToLowerInvariant());
    }
}
