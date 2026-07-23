namespace Unskip.Infrastructure.Windows.Tests;

public sealed class ExternalUriStartInfoFactoryTests
{
    [Fact]
    public void CreateUsesDefaultWindowsAssociationForAbsoluteHttpsUri()
    {
        var startInfo = ExternalUriStartInfoFactory.Create(new Uri("https://example.test/portfolio"));

        Assert.Equal("https://example.test/portfolio", startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
        Assert.False(startInfo.ErrorDialog);
        Assert.Empty(startInfo.ArgumentList);
        Assert.Empty(startInfo.Arguments);
    }

    [Theory]
    [InlineData("http://example.test")]
    [InlineData("https://user@example.test")]
    public void CreateRejectsUntrustedUriForms(string value)
    {
        Assert.Throws<ArgumentException>(() => ExternalUriStartInfoFactory.Create(new Uri(value)));
    }

    [Fact]
    public void CreateRejectsRelativeUri()
    {
        Assert.Throws<ArgumentException>(
            () => ExternalUriStartInfoFactory.Create(new Uri("/portfolio", UriKind.Relative)));
    }
}
