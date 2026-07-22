using Unskip.App.Commands;
using Unskip.App.Services;
using Unskip.Core;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;

namespace Unskip.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private Workspace _workspace = Workspace.Devices;

    public MainWindowViewModel(
        DeviceDirectoryViewModel deviceDirectory,
        IMessageSender messageSender,
        SendHistoryService historyService,
        IHistoryDeletionConfirmation historyConfirmation)
    {
        DeviceDirectory = deviceDirectory ?? throw new ArgumentNullException(nameof(deviceDirectory));
        Composer = new MessageComposerViewModel(messageSender, historyService);
        History = new SendHistoryViewModel(historyService, historyConfirmation);
        ShowDevicesCommand = new RelayCommand(_ => Show(Workspace.Devices), _ => !Composer.IsSending);
        ShowComposerCommand = new RelayCommand(
            _ => Show(Workspace.Composer),
            _ => !string.IsNullOrWhiteSpace(Composer.Destination) && !Composer.IsSending);
        ShowHistoryCommand = new AsyncRelayCommand(_ => ShowHistoryAsync(), _ => !Composer.IsSending);
        DeviceDirectory.MessagePreparationRequested += (_, destination) => PrepareComposer(destination, false);
        Composer.BackRequested += (_, _) => Show(Workspace.Devices);
        History.RetryRequested += (_, destination) => PrepareComposer(destination, true);
    }

    public string ProductName { get; } = ProductIdentity.Name;
    public string Tagline { get; } = ProductIdentity.Tagline;
    public string AffiliationNotice { get; } = ProductIdentity.AffiliationNotice;
    public string CurrentSection => _workspace switch { Workspace.Composer => "Send", Workspace.History => "History", _ => "Devices" };
    public string SectionDescription => _workspace switch
    {
        Workspace.Composer => "Compose a native Windows LAN message and review the real technical destination.",
        Workspace.History => "Review local send attempts without storing message bodies or claiming acknowledgement.",
        _ => "Choose a saved device or prepare a one-time destination.",
    };

    public IReadOnlyList<NavigationItemViewModel> NavigationItems =>
    [
        new("Send", "↗", IsComposerVisible, null, ShowComposerCommand),
        new("Devices", "◫", IsDevicesVisible, null, ShowDevicesCommand),
        new("History", "◷", IsHistoryVisible, null, ShowHistoryCommand),
    ];

    public DeviceDirectoryViewModel DeviceDirectory { get; }
    public MessageComposerViewModel Composer { get; }
    public SendHistoryViewModel History { get; }
    public RelayCommand ShowDevicesCommand { get; }
    public RelayCommand ShowComposerCommand { get; }
    public AsyncRelayCommand ShowHistoryCommand { get; }
    public bool IsDevicesVisible => _workspace == Workspace.Devices;
    public bool IsComposerVisible => _workspace == Workspace.Composer;
    public bool IsHistoryVisible => _workspace == Workspace.History;

    public async Task InitializeAsync()
    {
        await DeviceDirectory.InitializeAsync().ConfigureAwait(true);
        await History.ReloadAsync().ConfigureAwait(true);
    }

    private void PrepareComposer(MessagePreparationRequestedEventArgs destination, bool clearDraft)
    {
        Composer.Prepare(destination);
        if (clearDraft)
        {
            Composer.Message = string.Empty;
        }

        Show(Workspace.Composer);
        ShowComposerCommand.NotifyCanExecuteChanged();
    }

    private async Task ShowHistoryAsync()
    {
        await History.ReloadAsync().ConfigureAwait(true);
        Show(Workspace.History);
    }

    private void Show(Workspace workspace)
    {
        _workspace = workspace;
        OnPropertyChanged(nameof(CurrentSection));
        OnPropertyChanged(nameof(SectionDescription));
        OnPropertyChanged(nameof(NavigationItems));
        OnPropertyChanged(nameof(IsDevicesVisible));
        OnPropertyChanged(nameof(IsComposerVisible));
        OnPropertyChanged(nameof(IsHistoryVisible));
    }

    private enum Workspace { Devices, Composer, History }
}
