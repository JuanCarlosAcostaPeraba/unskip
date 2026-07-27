using System.Globalization;

namespace Unskip.App.Localization;

internal static class LanguagePolicy
{
    public const string English = "en";
    public const string Spanish = "es";

    public static string Resolve(string? storedLanguage, CultureInfo windowsUiCulture)
    {
        ArgumentNullException.ThrowIfNull(windowsUiCulture);

        var stored = Normalize(storedLanguage);
        return stored ?? Normalize(windowsUiCulture.TwoLetterISOLanguageName) ?? English;
    }

    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        return language.Trim().ToLowerInvariant() switch
        {
            English => English,
            Spanish => Spanish,
            _ => null,
        };
    }

    public static CultureInfo CreateCulture(string language)
    {
        var normalized = Normalize(language)
            ?? throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language.");
        return CultureInfo.GetCultureInfo(normalized);
    }
}
