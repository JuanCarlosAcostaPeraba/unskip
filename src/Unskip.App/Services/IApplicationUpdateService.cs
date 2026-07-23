using Unskip.Core.Updates;

namespace Unskip.App.Services;

public interface IApplicationUpdateService
{
    Task<UpdateCheckResult> CheckForUpdateAsync(
        SemanticVersion currentVersion,
        CancellationToken cancellationToken = default);

    Task<UpdateDownloadResult> DownloadAsync(
        ApplicationUpdateRelease release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(
        UpdateDownloadResult download,
        CancellationToken cancellationToken = default);
}

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    ApplicationUpdateRelease? Release)
{
    public static UpdateCheckResult UpToDate { get; } = new(false, null);

    public static UpdateCheckResult Available(ApplicationUpdateRelease release) =>
        new(true, release ?? throw new ArgumentNullException(nameof(release)));
}

public sealed record UpdateDownloadResult(string InstallerPath, string Sha256);
