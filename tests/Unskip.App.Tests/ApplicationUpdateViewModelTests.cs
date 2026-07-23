using System.Net.Http;
using Unskip.App.Services;
using Unskip.Core.Updates;

namespace Unskip.App.Tests;

public sealed class ApplicationUpdateViewModelTests
{
    [Fact]
    public async Task AvailableVerifiedUpdateCanStartInstallerAndShutdown()
    {
        var context = ViewModelTestContext.Create();
        context.UpdateService.CheckResult = UpdateCheckResult.Available(CreateRelease("0.2.0"));

        await context.Updates.CheckForUpdatesCommand.ExecuteAsync();

        Assert.True(context.Updates.IsDownloadVisible);
        Assert.Contains("0.2.0", context.Updates.StatusMessage, StringComparison.Ordinal);

        await context.Updates.DownloadUpdateCommand.ExecuteAsync();

        Assert.True(context.Updates.IsInstallVisible);
        Assert.Equal("Download verified. Ready to install.", context.Updates.StatusMessage);

        await context.Updates.InstallUpdateCommand.ExecuteAsync();

        Assert.Equal(context.UpdateService.DownloadResult.InstallerPath, context.UpdateInstaller.InstallerPath);
        Assert.Equal(1, context.ApplicationShutdown.RequestCount);
        Assert.Equal(1, context.UpdateService.VerificationCount);
    }

    [Fact]
    public async Task OfflineCheckDoesNotExposeExceptionOrDisableApplication()
    {
        var context = ViewModelTestContext.Create();
        context.UpdateService.CheckException = new HttpRequestException("sensitive endpoint detail");

        await context.Updates.CheckForUpdatesCommand.ExecuteAsync();

        Assert.False(context.Updates.IsBusy);
        Assert.False(context.Updates.IsDownloadVisible);
        Assert.Equal("Couldn't check for updates. Unskip still works offline.", context.Updates.StatusMessage);
        Assert.DoesNotContain("sensitive", context.Updates.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangedDownloadIsNeverStarted()
    {
        var context = ViewModelTestContext.Create();
        context.UpdateService.CheckResult = UpdateCheckResult.Available(CreateRelease("0.2.0"));
        context.UpdateService.VerificationResult = false;

        await context.Updates.CheckForUpdatesCommand.ExecuteAsync();
        await context.Updates.DownloadUpdateCommand.ExecuteAsync();
        await context.Updates.InstallUpdateCommand.ExecuteAsync();

        Assert.Null(context.UpdateInstaller.InstallerPath);
        Assert.Equal(0, context.ApplicationShutdown.RequestCount);
        Assert.False(context.Updates.IsInstallVisible);
        Assert.Contains("changed or is damaged", context.Updates.StatusMessage, StringComparison.Ordinal);
    }

    private static ApplicationUpdateRelease CreateRelease(string version)
    {
        var semanticVersion = SemanticVersion.Parse(version);
        var tag = $"v{version}";
        var baseUri = $"https://github.com/JuanCarlosAcostaPeraba/unskip/releases/download/{tag}/";
        return new ApplicationUpdateRelease(
            semanticVersion,
            tag,
            $"Unskip-{version}-win-x64-setup.exe",
            new Uri($"{baseUri}Unskip-{version}-win-x64-setup.exe"),
            4,
            new Uri($"{baseUri}SHA256SUMS.txt"));
    }
}
