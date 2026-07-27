using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.App.Services;

namespace Unskip.App.ViewModels;

public sealed class LanguageSettingsViewModel : ObservableObject
{
    private readonly IApplicationRestart _applicationRestart;
    private readonly ILanguageChangeConfirmation _confirmation;
    private readonly ILanguagePreferenceStore _preferenceStore;
    private readonly string _currentLanguage;
    private string _statusMessage = string.Empty;

    public LanguageSettingsViewModel(
        string currentLanguage,
        ILanguagePreferenceStore preferenceStore,
        ILanguageChangeConfirmation confirmation,
        IApplicationRestart applicationRestart)
    {
        _currentLanguage = LanguagePolicy.Normalize(currentLanguage)
            ?? throw new ArgumentOutOfRangeException(
                nameof(currentLanguage),
                currentLanguage,
                "Unsupported current language.");
        _preferenceStore = preferenceStore ?? throw new ArgumentNullException(nameof(preferenceStore));
        _confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
        _applicationRestart = applicationRestart ?? throw new ArgumentNullException(nameof(applicationRestart));
        ChangeLanguageCommand = new RelayCommand(ChangeLanguage, CanChangeLanguage);
    }

    public IReadOnlyList<SupportedLanguage> Languages { get; } =
    [
        new(LanguagePolicy.English, "English"),
        new(LanguagePolicy.Spanish, "Español"),
    ];

    public string CurrentLanguage => _currentLanguage;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand ChangeLanguageCommand { get; }

    private bool CanChangeLanguage(object? parameter) =>
        parameter is string language
        && LanguagePolicy.Normalize(language) is string normalized
        && !string.Equals(normalized, _currentLanguage, StringComparison.Ordinal);

    private void ChangeLanguage(object? parameter)
    {
        if (parameter is not string language
            || LanguagePolicy.Normalize(language) is not string normalized)
        {
            return;
        }

        var option = Languages.Single(item => item.Code == normalized);
        if (!_confirmation.Confirm(option.DisplayName))
        {
            return;
        }

        if (!_preferenceStore.TrySave(normalized))
        {
            StatusMessage = UiText.Get("LanguageSaveFailed");
            return;
        }

        if (!_applicationRestart.TryRestart())
        {
            StatusMessage = UiText.Get("LanguageRestartFailed");
        }
    }
}
