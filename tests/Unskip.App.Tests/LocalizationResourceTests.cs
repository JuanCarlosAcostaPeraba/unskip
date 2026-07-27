using System.Collections;
using System.Globalization;
using System.Resources;
using Unskip.App.Localization;

namespace Unskip.App.Tests;

public sealed class LocalizationResourceTests
{
    private static readonly ResourceManager Resources = new(
        "Unskip.App.Localization.Strings",
        typeof(UiText).Assembly);

    [Fact]
    public void EnglishAndSpanishCatalogsHaveMatchingKeys()
    {
        var englishKeys = GetKeys(CultureInfo.GetCultureInfo("en"));
        var spanishKeys = GetKeys(CultureInfo.GetCultureInfo("es"));

        Assert.NotEmpty(englishKeys);
        Assert.Equal(englishKeys, spanishKeys);
    }

    [Theory]
    [InlineData("SectionDevices", "Devices", "Dispositivos")]
    [InlineData("SectionSend", "Send", "Enviar")]
    [InlineData("CloseMessage", "Close message", "Cerrar mensaje")]
    public void RepresentativeTextExistsInBothLanguages(
        string key,
        string expectedEnglish,
        string expectedSpanish)
    {
        Assert.Equal(expectedEnglish, Resources.GetString(key, CultureInfo.GetCultureInfo("en")));
        Assert.Equal(expectedSpanish, Resources.GetString(key, CultureInfo.GetCultureInfo("es")));
    }

    private static string[] GetKeys(CultureInfo culture)
    {
        var resourceSet = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new MissingManifestResourceException($"No resources for '{culture.Name}'.");
        return resourceSet
            .Cast<DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
