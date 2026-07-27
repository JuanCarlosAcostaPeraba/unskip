using System.Windows;
using Unskip.App.Localization;

namespace Unskip.App.Services;

internal sealed class MessageBoxLanguageChangeConfirmation : ILanguageChangeConfirmation
{
    public bool Confirm(string languageName) =>
        MessageBox.Show(
            UiText.Format("LanguageRestartConfirmation", languageName),
            UiText.Get("LanguageRestartTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information,
            MessageBoxResult.Cancel) == MessageBoxResult.OK;
}
