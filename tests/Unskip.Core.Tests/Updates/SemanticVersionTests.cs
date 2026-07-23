using Unskip.Core.Updates;

namespace Unskip.Core.Tests.Updates;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("0.1.0")]
    [InlineData("1.2.3-beta.1")]
    [InlineData("10.20.30-rc.2")]
    public void SupportedVersionsRoundTrip(string value)
    {
        var version = SemanticVersion.Parse(value);

        Assert.Equal(value, version.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("v1.2.3")]
    [InlineData("1.2")]
    [InlineData("01.2.3")]
    [InlineData("1.2.3-01")]
    [InlineData("1.2.3-beta.")]
    public void InvalidVersionsAreRejected(string value)
    {
        Assert.False(SemanticVersion.TryParse(value, out _));
    }

    [Fact]
    public void PreReleaseOrderingFollowsSemanticVersionRules()
    {
        var orderedValues = new[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0-beta",
            "1.0.0-beta.2",
            "1.0.0-beta.11",
            "1.0.0-rc.1",
            "1.0.0",
            "1.0.1",
        };

        var versions = orderedValues.Select(SemanticVersion.Parse).ToList();

        Assert.Equal(versions, versions.Order());
    }
}
