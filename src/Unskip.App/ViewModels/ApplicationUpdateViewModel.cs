using System.IO;
using System.Net.Http;
using System.Text.Json;
using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.Core.Updates;

namespace Unskip.App.ViewModels;

public sealed class ApplicationUpdateViewModel : ObservableObject
{
    private readonly IApplicationShutdown _applicationShutdown;
    private readonly IApplicationUpdateService _applicationUpdateService;
    private readonly SemanticVersion _currentVersion;
    private readonly IUpdateInstallerLauncher _installerLauncher;
    private ApplicationUpdateRelease? _availableRelease;
    private UpdateDownloadResult? _download;
    private int _downloadProgress;
    private bool _isBusy;
    private bool _isDownloading;
    private string _statusMessage = UiText.Get("UpdatesOnDemand");

    public ApplicationUpdateViewModel(
        IApplicationUpdateService applicationUpdateService,
        IUpdateInstallerLauncher installerLauncher,
        IApplicationShutdown applicationShutdown,
        SemanticVersion currentVersion)
    {
        _applicationUpdateService = applicationUpdateService
            ?? throw new ArgumentNullException(nameof(applicationUpdateService));
        _installerLauncher = installerLauncher
            ?? throw new ArgumentNullException(nameof(installerLauncher));
        _applicationShutdown = applicationShutdown
            ?? throw new ArgumentNullException(nameof(applicationShutdown));
        _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));

        CheckForUpdatesCommand = new AsyncRelayCommand(_ => CheckForUpdatesAsync(), _ => !IsBusy);
        DownloadUpdateCommand = new AsyncRelayCommand(
            _ => DownloadUpdateAsync(),
            _ => !IsBusy && _availableRelease is not null && _download is null);
        InstallUpdateCommand = new AsyncRelayCommand(
            _ => InstallUpdateAsync(),
            _ => !IsBusy && _download is not null);
    }

    public AsyncRelayCommand CheckForUpdatesCommand { get; }

    public AsyncRelayCommand DownloadUpdateCommand { get; }

    public AsyncRelayCommand InstallUpdateCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int DownloadProgress
    {
        get => _downloadProgress;
        private set
        {
            if (SetProperty(ref _downloadProgress, value))
            {
                OnPropertyChanged(nameof(DownloadProgressLabel));
            }
        }
    }

    public string DownloadProgressLabel => $"{DownloadProgress}%";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyStateChanged();
            }
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(IsProgressVisible));
            }
        }
    }

    public bool IsDownloadVisible => _availableRelease is not null && _download is null;

    public bool IsInstallVisible => _download is not null;

    public bool IsProgressVisible => IsBusy && IsDownloading;

    private async Task CheckForUpdatesAsync()
    {
        IsBusy = true;
        IsDownloading = false;
        StatusMessage = UiText.Get("UpdatesChecking");
        try
        {
            var result = await _applicationUpdateService
                .CheckForUpdateAsync(_currentVersion)
                .ConfigureAwait(true);
            _availableRelease = result.Release;
            _download = null;
            StatusMessage = result.IsUpdateAvailable
                ? UiText.Format("UpdatesAvailable", result.Release!.Version)
                : UiText.Get("UpdatesCurrent");
        }
        catch (Exception exception) when (IsExpectedUpdateFailure(exception))
        {
            _availableRelease = null;
            _download = null;
            StatusMessage = UiText.Get("UpdatesCheckFailed");
        }
        finally
        {
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    private async Task DownloadUpdateAsync()
    {
        if (_availableRelease is null)
        {
            return;
        }

        IsBusy = true;
        IsDownloading = true;
        DownloadProgress = 0;
        StatusMessage = UiText.Format("UpdatesDownloading", _availableRelease.Version);
        try
        {
            var progress = new Progress<int>(value => DownloadProgress = Math.Clamp(value, 0, 100));
            _download = await _applicationUpdateService
                .DownloadAsync(_availableRelease, progress)
                .ConfigureAwait(true);
            StatusMessage = UiText.Get("UpdatesReadyToInstall");
        }
        catch (InvalidDataException)
        {
            _download = null;
            StatusMessage = UiText.Get("UpdatesVerificationFailed");
        }
        catch (Exception exception) when (IsExpectedUpdateFailure(exception))
        {
            _download = null;
            StatusMessage = UiText.Get("UpdatesDownloadFailed");
        }
        finally
        {
            IsDownloading = false;
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    private async Task InstallUpdateAsync()
    {
        if (_download is null)
        {
            return;
        }

        IsBusy = true;
        IsDownloading = false;
        StatusMessage = UiText.Get("UpdatesVerifyingInstaller");
        try
        {
            if (!await _applicationUpdateService.VerifyAsync(_download).ConfigureAwait(true))
            {
                _download = null;
                StatusMessage = UiText.Get("UpdatesInstallerChanged");
                return;
            }

            if (!_installerLauncher.TryLaunch(_download.InstallerPath))
            {
                StatusMessage = UiText.Get("UpdatesInstallerStartFailedRetry");
                return;
            }

            StatusMessage = UiText.Get("UpdatesInstallerStarted");
            _applicationShutdown.Shutdown();
        }
        catch (Exception exception) when (IsExpectedUpdateFailure(exception))
        {
            StatusMessage = UiText.Get("UpdatesInstallerStartFailed");
        }
        finally
        {
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(IsDownloadVisible));
        OnPropertyChanged(nameof(IsInstallVisible));
        OnPropertyChanged(nameof(IsProgressVisible));
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        DownloadUpdateCommand.NotifyCanExecuteChanged();
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    private static bool IsExpectedUpdateFailure(Exception exception) =>
        exception is HttpRequestException
            or IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or TaskCanceledException
            or JsonException;
}
