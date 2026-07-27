using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.Core;
using Unskip.Core.Links;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;

namespace Unskip.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private static readonly Uri DeveloperPortfolioUri = new("https://jcap.tech", UriKind.Absolute);
    private readonly IExternalUriLauncher _externalUriLauncher;
    private Workspace _workspace = Workspace.Devices;
    private string _developerLinkStatus = string.Empty;

    public MainWindowViewModel(
        DeviceDirectoryViewModel deviceDirectory,
        IMessageSender messageSender,
        SendHistoryService historyService,
        IHistoryDeletionConfirmation historyConfirmation,
        ApplicationUpdateViewModel applicationUpdate,
        IExternalUriLauncher externalUriLauncher,
        IUrgentAttentionPreviewService urgentAttentionPreview,
        LanguageSettingsViewModel languageSettings,
        string versionLabel)
    {
        DeviceDirectory = deviceDirectory ?? throw new ArgumentNullException(nameof(deviceDirectory));
        Updates = applicationUpdate ?? throw new ArgumentNullException(nameof(applicationUpdate));
        Language = languageSettings ?? throw new ArgumentNullException(nameof(languageSettings));
        _externalUriLauncher = externalUriLauncher ?? throw new ArgumentNullException(nameof(externalUriLauncher));
        ArgumentException.ThrowIfNullOrWhiteSpace(versionLabel);
        VersionLabel = versionLabel;
        Composer = new MessageComposerViewModel(
            messageSender,
            historyService,
            urgentAttentionPreview);
        History = new SendHistoryViewModel(historyService, historyConfirmation);
        ShowDevicesCommand = new RelayCommand(_ => Show(Workspace.Devices), _ => !Composer.IsSending);
        ShowComposerCommand = new RelayCommand(
            _ => Show(Workspace.Composer),
            _ => !string.IsNullOrWhiteSpace(Composer.Destination) && !Composer.IsSending);
        ShowHistoryCommand = new AsyncRelayCommand(_ => ShowHistoryAsync(), _ => !Composer.IsSending);
        OpenQuickSendCommand = new RelayCommand(
            _ => QuickSendRequested?.Invoke(this, EventArgs.Empty));
        OpenDeveloperPortfolioCommand = new RelayCommand(_ => OpenDeveloperPortfolio());
        DeviceDirectory.MessagePreparationRequested += (_, destination) => PrepareComposer(destination, false);
        Composer.BackRequested += (_, _) => Show(Workspace.Devices);
        History.RetryRequested += (_, destination) => PrepareComposer(destination, true);
    }

    public string ProductName { get; } = ProductIdentity.Name;

    public event EventHandler? QuickSendRequested;

    public string Tagline { get; } = UiText.Get("LocalMessaging");
    public string AffiliationNotice { get; } = UiText.Get("AffiliationNotice");
    public string DeveloperName { get; } = "Juan Carlos Acosta Perabá";
    public string DeveloperPortfolioUrl { get; } = DeveloperPortfolioUri.AbsoluteUri;
    public string DeveloperLinkStatus
    {
        get => _developerLinkStatus;
        private set => SetProperty(ref _developerLinkStatus, value);
    }

    public string VersionLabel { get; }
    public string CurrentSection => _workspace switch
    {
        Workspace.Composer => UiText.Get("SectionSend"),
        Workspace.History => UiText.Get("SectionHistory"),
        _ => UiText.Get("SectionDevices"),
    };
    public string SectionDescription => _workspace switch
    {
        Workspace.Composer => UiText.Get("SectionSendDescription"),
        Workspace.History => UiText.Get("SectionHistoryDescription"),
        _ => UiText.Get("SectionDevicesDescription"),
    };

    public IReadOnlyList<NavigationItemViewModel> NavigationItems =>
    [
        new(UiText.Get("SectionSend"), "↗", IsComposerVisible, null, ShowComposerCommand),
        new(UiText.Get("SectionDevices"), "◫", IsDevicesVisible, null, ShowDevicesCommand),
        new(UiText.Get("SectionHistory"), "◷", IsHistoryVisible, null, ShowHistoryCommand),
    ];

    public DeviceDirectoryViewModel DeviceDirectory { get; }
    public MessageComposerViewModel Composer { get; }
    public SendHistoryViewModel History { get; }
    public ApplicationUpdateViewModel Updates { get; }
    public LanguageSettingsViewModel Language { get; }
    public RelayCommand ShowDevicesCommand { get; }
    public RelayCommand ShowComposerCommand { get; }
    public AsyncRelayCommand ShowHistoryCommand { get; }
    public RelayCommand OpenQuickSendCommand { get; }
    public RelayCommand OpenDeveloperPortfolioCommand { get; }
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

    private void OpenDeveloperPortfolio()
    {
        DeveloperLinkStatus = _externalUriLauncher.TryOpen(DeveloperPortfolioUri)
            ? string.Empty
            : UiText.Get("PortfolioOpenFailed");
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
