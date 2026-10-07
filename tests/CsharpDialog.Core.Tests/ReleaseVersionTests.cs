using csharpDialog.Core.Notifications;

namespace CsharpDialog.Core.Tests;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("2026.10.06.2026+3f2a9c1d", "2026.10.06.2026")]
    [InlineData("2026.10.6.917", "2026.10.06.0917")]
    [InlineData("2026.1.2.5+abc", "2026.01.02.0005")]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.0.0+sha", "1.0.0")]
    [InlineData(null, "unknown")]
    [InlineData("", "unknown")]
    public void StripsShaAndPadsDateVersions(string? input, string expected)
        => Assert.Equal(expected, ReleaseVersion.Display(input));
}
