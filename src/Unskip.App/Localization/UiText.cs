using System.Globalization;
using System.Resources;

namespace Unskip.App.Localization;

internal static class UiText
{
    private static readonly ResourceManager ResourceManager = new(
        "Unskip.App.Localization.Strings",
        typeof(UiText).Assembly);
    private static CultureInfo _culture = CultureInfo.GetCultureInfo(LanguagePolicy.English);

    internal static CultureInfo Culture => _culture;

    internal static void SetCulture(CultureInfo culture)
    {
        _culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return ResourceManager.GetString(key, _culture)
            ?? throw new MissingManifestResourceException(
                $"The localized UI resource '{key}' was not found.");
    }

    public static string Format(string key, params object?[] arguments) =>
        string.Format(_culture, Get(key), arguments);
}
