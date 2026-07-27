using System.Windows;
using Unskip.App.Localization;

namespace Unskip.App.Services;

public sealed class MessageBoxHistoryDeletionConfirmation : IHistoryDeletionConfirmation
{
    public Task<bool> ConfirmDeleteAsync(string destinationAlias) => Task.FromResult(
        MessageBox.Show(
            UiText.Format("DeleteHistoryEntryConfirmation", destinationAlias),
            UiText.Get("DeleteHistoryEntryTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes);

    public Task<bool> ConfirmClearAsync(int count) => Task.FromResult(
        MessageBox.Show(
            UiText.Format("ClearHistoryConfirmation", count),
            UiText.Get("ClearHistoryTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes);
}
