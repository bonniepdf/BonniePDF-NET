using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Rendering.Writers;
using BonniePDF.Security.Encryption;
using Xunit;

namespace BonniePDF.Tests.Security;
public class EncryptedObjectTests
{
    [Fact]
    public void StringTokensMatchSharedVectors()
    {
        using JsonDocument fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Security", "Fixtures", "string-tokens.json")));
        foreach (JsonElement item in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            List<string> decoded = [];
            string actual = PdfStringEncryption.EncryptStrings(item.GetProperty("fragment").GetString()!, bytes =>
            {
                decoded.Add(Convert.ToHexString(bytes));
                return bytes;
            });
            Assert.Equal(item.GetProperty("expected").GetString(), actual);
            Assert.Equal(item.GetProperty("decodedHex").EnumerateArray().Select(x => x.GetString()), decoded);
        }

        foreach (JsonElement item in fixture.RootElement.GetProperty("invalid").EnumerateArray())
            Assert.Throws<FormatException>(() => PdfStringEncryption.EncryptStrings(item.GetString()!, bytes => bytes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WritersEncryptNestedStringsWithTheirObjectKey(bool asynchronous)
    {
        var encryptor = new PdfEncryptor(new EncryptionSettings { UserPassword = "reader", OwnerPassword = "owner" }, new byte[16]);
        using MemoryStream stream = new();
        var dict = new Dictionary<string, string>
        {
            ["/Lang"] = "(en-GB)",
            ["/Action"] = "<< /URI (https://example.com) >>"
        };
        if (asynchronous)
        {
            await using var writer = new AsyncPdfWriter(stream, leaveOpen: true)
            {
                Encryptor = encryptor
            };
            await writer.WriteEncryptDictionaryAsync(writer.GetNewId());
            await writer.WriteObjectAsync(writer.GetNewId(), dict);
            await writer.WriteRawObjectAsync(writer.GetNewId(), "[<FEFF03A9> (title)]");
            await writer.WriteBinaryObjectAsync(writer.GetNewId(), dict, [1, 2], 2);
        }
        else
        {
            using var writer = new PdfWriter(stream, leaveOpen: true)
            {
                Encryptor = encryptor
            };
            writer.WriteEncryptDictionary(writer.GetNewId());
            writer.WriteObject(writer.GetNewId(), dict);
            writer.WriteRawObject(writer.GetNewId(), "[<FEFF03A9> (title)]");
            writer.WriteBinaryObject(writer.GetNewId(), dict, [1, 2]);
        }

        string pdf = Encoding.Latin1.GetString(stream.ToArray());
        Assert.Contains(encryptor.GetEncryptDictionary()["/O"], pdf);
        foreach (int id in new[]
        {
            2,
            4
        }

        )
        {
            Assert.Contains("/Lang <" + Convert.ToHexString(encryptor.EncryptData(Encoding.ASCII.GetBytes("en-GB"), id)) + ">", pdf);
            Assert.Contains("/URI <" + Convert.ToHexString(encryptor.EncryptData(Encoding.ASCII.GetBytes("https://example.com"), id)) + ">", pdf);
        }

        Assert.Contains("<" + Convert.ToHexString(encryptor.EncryptData(Convert.FromHexString("FEFF03A9"), 3)) + ">", pdf);
        Assert.DoesNotContain("(title)", pdf);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task XmpEncryptionAgreesWithAesMetadataFlag(bool encryptMetadata, bool asynchronous)
    {
        var settings = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            UserPassword = "reader",
            OwnerPassword = "owner",
            EncryptMetadata = encryptMetadata
        };
        var encryptor = new PdfEncryptor(settings);
        Assert.Equal(encryptMetadata ? "true" : "false", encryptor.GetEncryptDictionary()["/EncryptMetadata"]);
        var key = new Aes256EncryptionKey(settings);
        using Aes aes = Aes.Create();
        aes.Key = key.FileEncryptionKey;
        byte[] permissions = aes.DecryptEcb(key.PermsEntry, PaddingMode.None);
        Assert.Equal((byte)(encryptMetadata ? 'T' : 'F'), permissions[8]);
        using MemoryStream stream = new();
        var metadata = new DocumentMetadata
        {
            Title = "Metadata Ω"
        };
        if (asynchronous)
        {
            await using var writer = new AsyncPdfWriter(stream, leaveOpen: true)
            {
                Encryptor = encryptor
            };
            await new MetadataWriter(writer).WriteXmpMetadataAsync(writer.GetNewId(), metadata, new DocumentSettings());
        }
        else
        {
            using var writer = new PdfWriter(stream, leaveOpen: true)
            {
                Encryptor = encryptor
            };
            new MetadataWriter(writer).WriteXmpMetadata(writer.GetNewId(), metadata, new DocumentSettings());
        }

        string pdf = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(!encryptMetadata, pdf.Contains("Metadata Ω", StringComparison.Ordinal));
        Assert.Equal(!encryptMetadata, pdf.Contains("<?xpacket", StringComparison.Ordinal));
    }

    [Fact]
    public void LegacyRc4IgnoresMetadataFlagForKeyDerivation()
    {
        var settings = new EncryptionSettings
        {
            UserPassword = "reader",
            OwnerPassword = "owner"
        };
        byte[] id = new byte[16];
        byte[] expected = new PdfEncryptor(settings, id).EncryptData([1, 2, 3], 1);
        settings.EncryptMetadata = false;
        Assert.Equal(expected, new PdfEncryptor(settings, id).EncryptData([1, 2, 3], 1));
    }
}
