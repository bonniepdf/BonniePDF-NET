using BonniePDF.Security.Encryption;
using Xunit;

namespace BonniePDF.Tests.Core;

public class NullableInputTests
{
    [Fact]
    public void OptionalNullInputsRetainTheirExistingEmptyOrIgnoredBehavior()
    {
        DocumentMetadata metadata = new() { Keywords = "retained" };
        metadata.SetKeywords(null);
        Assert.Equal("retained", metadata.Keywords);

        OrderedList ordered = new();
        UnorderedList unordered = new();
        ordered.AddItem(null);
        unordered.AddItem(null);
        Assert.Equal(string.Empty, Assert.Single(ordered.Items));
        Assert.Equal(string.Empty, Assert.Single(unordered.Items));

        Layers layers = new();
        Assert.Same(layers, layers.AddBackground(null).AddForeground(null));
        Assert.Empty(layers.BackgroundLayers);
        Assert.Empty(layers.ForegroundLayers);

        Svg svg = new() { SvgData = "<svg xmlns=\"http://www.w3.org/2000/svg\"><title>Fallback</title></svg>", AltText = "Override" };
        svg.AltText = null;
        Assert.Equal("Fallback", svg.AltText);

        DocumentSettings settings = new();
        settings.WithPasswords(null, null);
        Assert.Equal(string.Empty, settings.Encryption!.UserPassword);
        Assert.Equal(string.Empty, settings.Encryption.OwnerPassword);
        EncryptionOptions options = new EncryptionOptions().WithUserPassword(null).WithOwnerPassword(null).WithPassword(null);
        Assert.Throws<InvalidOperationException>(() => options.Build());
        Assert.Equal("owner", options.WithOwnerPassword("owner").Build().OwnerPassword);
    }

    [Fact]
    public void NullableEncryptionBuffersPreserveNullAndNonNullResults()
    {
        RC4Cipher cipher = new(new byte[] { 1, 2, 3 });
        cipher.Transform(null);
        cipher.Transform(null, 0, 0);
        Assert.Null(cipher.TransformCopy(null));
        Assert.NotNull(cipher.TransformCopy(new byte[] { 4, 5 }));

        PdfEncryptor encryptor = new(new EncryptionSettings { OwnerPassword = "owner" });
        Assert.Null(encryptor.EncryptData(null, 1));
        encryptor.EncryptInPlace(null, 1);
        Assert.Empty(encryptor.EncryptString(null, 1));
        Assert.NotNull(encryptor.EncryptData(new byte[] { 4, 5 }, 1));
    }
}
