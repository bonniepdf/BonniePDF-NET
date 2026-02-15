using System;
using BonniePDF.Configuration;
using BonniePDF.Extensions;
using BonniePDF.Security.Encryption;
using BonniePDF.Security.Models;
using Xunit;

namespace BonniePDF.Tests;

public class EncryptionTests
{
    [Fact]
    public void AES256_EncryptionAlgorithm_HasCorrectValues()
    {
        var settings = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            UserPassword = "test",
            OwnerPassword = "test"
        };

        Assert.Equal(256, settings.KeyLengthBits);
        Assert.Equal(32, settings.KeyLengthBytes);
        Assert.Equal(5, settings.VersionValue);
        Assert.Equal(6, settings.RevisionValue);
    }

    [Fact]
    public void AES256_Encryptor_ProducesEncryptDictionary()
    {
        var settings = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            UserPassword = "secret",
            OwnerPassword = "owner"
        };

        var encryptor = new PdfEncryptor(settings);
        var dict = encryptor.GetEncryptDictionary();

        Assert.Equal("/Standard", dict["/Filter"]);
        Assert.Equal("5", dict["/V"]);
        Assert.Equal("6", dict["/R"]);
        Assert.Equal("256", dict["/Length"]);
        Assert.True(dict.ContainsKey("/O"));
        Assert.True(dict.ContainsKey("/U"));
        Assert.True(dict.ContainsKey("/OE"));
        Assert.True(dict.ContainsKey("/UE"));
        Assert.True(dict.ContainsKey("/Perms"));
        Assert.True(dict.ContainsKey("/CF"));
        Assert.Contains("/AESV3", dict["/CF"]);
    }

    [Fact]
    public void AES256_EncryptData_ProducesOutput()
    {
        var settings = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            UserPassword = "test",
            OwnerPassword = "test"
        };

        var encryptor = new PdfEncryptor(settings);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello World");
        byte[] encrypted = encryptor.EncryptData(data, 1);

        Assert.NotNull(encrypted);
        Assert.True(encrypted.Length > data.Length); // IV + padding
        Assert.NotEqual(data, encrypted);
    }

    [Fact]
    public void AES256_RequiresPdf17_ThrowsOnPdf14()
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf14;
        doc.Settings.Encryption = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            UserPassword = "test",
            OwnerPassword = "test"
        };

        using MemoryStream ms = new();
        Assert.Throws<InvalidOperationException>(() => doc.Save(ms, true));
    }

    [Fact]
    public void AES256_WithPdf17_SavesSuccessfully()
    {
        using Document doc = new();
        doc.Settings.WithEncryptionAes256(opt => opt.WithPassword("secret"));

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void RC4_128_StillWorksAfterChanges()
    {
        using Document doc = new();
        doc.Settings.WithEncryption128(opt => opt.WithPassword("pass123"));

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string line = reader.ReadLine();
        Assert.StartsWith("%PDF-1.4", line);
    }

    [Fact]
    public void AES256_GetIdArray_ReturnsValidFormat()
    {
        var settings = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            UserPassword = "test",
            OwnerPassword = "test"
        };

        var encryptor = new PdfEncryptor(settings);
        string idArray = encryptor.GetIdArray();

        Assert.StartsWith("[ <", idArray);
        Assert.EndsWith("> ]", idArray);
    }
}
