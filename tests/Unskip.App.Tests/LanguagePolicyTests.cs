using System.Globalization;
using Unskip.App.Localization;

namespace Unskip.App.Tests;

public sealed class LanguagePolicyTests
{
    [Theory]
    [InlineData("en", "es-ES", "en")]
    [InlineData(" ES ", "en-US", "es")]
    [InlineData(null, "es-ES", "es")]
    [InlineData("", "en-US", "en")]
    [InlineData("de", "de-DE", "en")]
    public void ResolveUsesSavedSupportedLanguageThenWindowsLanguageThenEnglish(
        string? savedLanguage,
        string windowsLanguage,
        string expected)
    {
        var actual = LanguagePolicy.Resolve(
            savedLanguage,
            CultureInfo.GetCultureInfo(windowsLanguage));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreateCultureRejectsUnsupportedLanguage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LanguagePolicy.CreateCulture("fr"));
    }
}
