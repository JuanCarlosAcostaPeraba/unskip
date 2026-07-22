using System.Windows;

namespace Unskip.App.Services;

public sealed class MessageBoxHistoryDeletionConfirmation : IHistoryDeletionConfirmation
{
    public Task<bool> ConfirmDeleteAsync(string destinationAlias) => Task.FromResult(
        MessageBox.Show(
            $"Delete the local history entry for {destinationAlias}?",
            "Delete history entry",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes);

    public Task<bool> ConfirmClearAsync(int count) => Task.FromResult(
        MessageBox.Show(
            $"Delete all {count} local history entries? This cannot be undone.",
            "Clear local history",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes);
}
