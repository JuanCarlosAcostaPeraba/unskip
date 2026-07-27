using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.Core.Devices;
using Unskip.Core.Messaging.History;
using Unskip.Core.Time;
using Unskip.Infrastructure.Persistence;
using Unskip.Infrastructure.Windows;

namespace Unskip.App;

/// <summary>
/// Represents the WPF application entry point.
/// </summary>
public partial class App : Application
{
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
            new WpfApplicationShutdown(),
            ApplicationVersion.Current);
        var languageSettings = new LanguageSettingsViewModel(
            language,
            languagePreference,
            new MessageBoxLanguageChangeConfirmation(),
            new WpfApplicationRestart());
        var viewModel = new MainWindowViewModel(
            deviceDirectory,
            new WindowsMsgSender(),
            history,
            new MessageBoxHistoryDeletionConfirmation(),
            updates,
            new SystemExternalUriLauncher(),
            new WpfUrgentAttentionPreviewService(),
            languageSettings,
            UiText.Format("VersionLabel", ApplicationVersion.Current));
        viewModel.InitializeAsync().GetAwaiter().GetResult();

        base.OnStartup(e);

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
    }
}
