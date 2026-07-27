using System.Windows;
using Unskip.App.Localization;

namespace Unskip.App.Services;

public sealed class MessageBoxDeviceDeletionConfirmation : IDeviceDeletionConfirmation
{
    public Task<bool> ConfirmAsync(string deviceAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceAlias);
        var result = MessageBox.Show(
            UiText.Format("DeleteDeviceConfirmation", deviceAlias),
            UiText.Get("DeleteDeviceTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
