using System.Windows;

namespace Unskip.App.Services;

public sealed class MessageBoxDeviceDeletionConfirmation : IDeviceDeletionConfirmation
{
    public Task<bool> ConfirmAsync(string deviceAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceAlias);
        var result = MessageBox.Show(
            $"Delete '{deviceAlias}' from this device directory?\n\nExisting send-history snapshots will be preserved.",
            "Delete device",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
