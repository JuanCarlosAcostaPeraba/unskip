using Unskip.Core;
using Unskip.Core.Messaging;

namespace Unskip.App.ViewModels;

/// <summary>
/// Coordinates the device directory and message composer workspaces.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private bool _isComposerVisible;

    public MainWindowViewModel(DeviceDirectoryViewModel deviceDirectory, IMessageSender messageSender)
    {
        DeviceDirectory = deviceDirectory ?? throw new ArgumentNullException(nameof(deviceDirectory));
        Composer = new MessageComposerViewModel(messageSender);
        DeviceDirectory.MessagePreparationRequested += OnMessagePreparationRequested;
        Composer.BackRequested += (_, _) => ShowDevices();
    }

    public string ProductName { get; } = ProductIdentity.Name;

    public string Tagline { get; } = ProductIdentity.Tagline;

    public string AffiliationNotice { get; } = ProductIdentity.AffiliationNotice;

    public string CurrentSection => IsComposerVisible ? "Send" : "Devices";

    public string SectionDescription => IsComposerVisible
        ? "Compose a native Windows LAN message and review the real technical destination."
        : "Choose a saved device or prepare a one-time destination.";

    public IReadOnlyList<NavigationItemViewModel> NavigationItems =>
    [
        new("Send", "↗", IsComposerVisible),
        new("Devices", "◫", !IsComposerVisible),
        new("History", "◷", false, "Later"),
    ];

    public DeviceDirectoryViewModel DeviceDirectory { get; }

    public MessageComposerViewModel Composer { get; }

    public bool IsComposerVisible
    {
        get => _isComposerVisible;
        private set
        {
            if (SetProperty(ref _isComposerVisible, value))
            {
                OnPropertyChanged(nameof(CurrentSection));
                OnPropertyChanged(nameof(SectionDescription));
                OnPropertyChanged(nameof(NavigationItems));
            }
        }
    }

    public Task InitializeAsync()
    {
        return DeviceDirectory.InitializeAsync();
    }

    private void OnMessagePreparationRequested(object? sender, MessagePreparationRequestedEventArgs destination)
    {
        Composer.Prepare(destination);
        IsComposerVisible = true;
    }

    private void ShowDevices()
    {
        IsComposerVisible = false;
    }
}
