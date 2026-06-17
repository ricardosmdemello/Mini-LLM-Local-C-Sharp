using MiniLlmLocal.Core.Models;

namespace MiniLlmLocal.Tests;

public class DownloadProgressTests
{
    [Fact]
    public void Fraction_HalfWay()
    {
        var p = new DownloadProgress(50, 100);
        Assert.Equal(0.5, p.Fraction);
    }

    [Fact]
    public void Fraction_NullWhenTotalUnknown()
    {
        Assert.Null(new DownloadProgress(50, null).Fraction);
        Assert.Null(new DownloadProgress(50, 0).Fraction);
    }

    [Fact]
    public void Fraction_ClampedToOne()
    {
        var p = new DownloadProgress(150, 100);
        Assert.Equal(1.0, p.Fraction);
    }

    [Theory]
    [InlineData(512L, "512.0 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1_610_612_736L, "1.5 GB")]
    public void FormatBytes_Formats(long bytes, string expected)
    {
        Assert.Equal(expected, ModelInfo.FormatBytes(bytes));
    }
}
