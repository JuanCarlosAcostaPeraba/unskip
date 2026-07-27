using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.App.Views;
using Unskip.Core.Devices;
using Unskip.Core.Messaging.History;
using Unskip.Core.Time;
using Unskip.Infrastructure.Persistence;
using Unskip.Infrastructure.Windows;

namespace Unskip.App;

/// <summary>
/// Represents the WPF application entry point.
/// </summary>
public partial class App : System.Windows.Application
{
    private ApplicationExitState? _exitState;
    private ResidentApplicationController? _residentController;

    protected override void OnStartup(StartupEventArgs e)
    {
        var languagePreference = FileLanguagePreferenceStore.ForCurrentUser();
        var language = LanguagePolicy.Resolve(
            languagePreference.Load(),
            CultureInfo.CurrentUICulture);
        var culture = LanguagePolicy.CreateCulture(language);
        UiText.SetCulture(culture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        SystemThemeManager.Apply(Resources);

        var database = UnskipDatabase.ForCurrentUser();
        database.InitializeAsync().GetAwaiter().GetResult();

        var clock = new SystemClock();
        var directory = new DeviceDirectoryService(database.Devices, clock);
        var history = new SendHistoryService(database.SendHistory, clock);
        var sender = new WindowsMsgSender();
        var urgentAttentionPreview = new WpfUrgentAttentionPreviewService();
        _exitState = new ApplicationExitState();
        var shutdown = new WpfApplicationShutdown(_exitState);
        var deviceDirectory = new DeviceDirectoryViewModel(
            directory,
            clock,
            new MessageBoxDeviceDeletionConfirmation());
        var updateService = new GitHubReleaseUpdateService(
            new HttpClient { Timeout = TimeSpan.FromSeconds(20) },
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unskip",
                "updates"));
        var updates = new ApplicationUpdateViewModel(
            updateService,
            new SystemUpdateInstallerLauncher(),
            shutdown,
            ApplicationVersion.Current);
        var languageSettings = new LanguageSettingsViewModel(
            language,
            languagePreference,
            new MessageBoxLanguageChangeConfirmation(),
            new WpfApplicationRestart(_exitState));
        var viewModel = new MainWindowViewModel(
            deviceDirectory,
            sender,
            history,
            new MessageBoxHistoryDeletionConfirmation(),
            updates,
            new SystemExternalUriLauncher(),
            urgentAttentionPreview,
            languageSettings,
            UiText.Format("VersionLabel", ApplicationVersion.Current));
        viewModel.InitializeAsync().GetAwaiter().GetResult();
        var quickComposer = new MessageComposerViewModel(sender, history, urgentAttentionPreview);
        var quickSend = new QuickSendViewModel(directory, clock, quickComposer);

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var mainWindow = new MainWindow(viewModel);
        var quickSendWindow = new QuickSendWindow(quickSend);
        var mainHost = new WpfResidentWindow(mainWindow);
        var quickSendHost = new WpfResidentWindow(quickSendWindow);
        var tray = new NotifyIconTrayService();
        var residentController = new ResidentApplicationController(
            mainHost,
            quickSendHost,
            tray,
            shutdown,
            _exitState,
            quickSend.ReloadAsync);
        _residentController = residentController;

        mainWindow.Closing += (_, eventArgs) =>
            eventArgs.Cancel = residentController.TryHideOnClose(mainHost);
        quickSendWindow.Closing += (_, eventArgs) =>
            eventArgs.Cancel = residentController.TryHideOnClose(quickSendHost);
        quickSend.OpenMainWindowRequested += (_, _) =>
        {
            quickSendHost.Hide();
            residentController.ShowMainWindow();
        };
        viewModel.QuickSendRequested += async (_, _) =>
            await residentController.ShowQuickSendAsync().ConfigureAwait(true);

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _exitState?.RequestExit();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _residentController?.Dispose();
        base.OnExit(e);
    }
}
