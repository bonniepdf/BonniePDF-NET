using Xunit;

namespace BonniePDF.Tests.Core;

public class ColorScRgbTests
{
    [Fact]
    public void ClampsEachComponentIntoTheUnitRange()
    {
        Color c = Color.FromScRgb(-0.2f, 1.5f, 0.5f, 2f);
        Assert.Equal(0f, c.ScR);
        Assert.Equal(1f, c.ScG);
        Assert.Equal(0.5f, c.ScB);
        Assert.Equal(1f, c.ScA);
    }
}
