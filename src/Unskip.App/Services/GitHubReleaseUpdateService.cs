using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unskip.Core.Updates;

namespace Unskip.App.Services;

public sealed class GitHubReleaseUpdateService : IApplicationUpdateService
{
    private const long MaximumInstallerSize = 250 * 1024 * 1024;
    private const int MaximumChecksumLength = 64 * 1024;
    private const string RepositoryPath = "/JuanCarlosAcostaPeraba/unskip/";
    private static readonly Uri LatestReleaseUri =
        new("https://api.github.com/repos/JuanCarlosAcostaPeraba/unskip/releases/latest");

    private readonly HttpClient _httpClient;
    private readonly string _updatesRoot;

    public GitHubReleaseUpdateService(HttpClient httpClient, string updatesRoot)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(updatesRoot);
        _updatesRoot = Path.GetFullPath(updatesRoot);
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        SemanticVersion currentVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        using var request = CreateRequest(LatestReleaseUri);
        using var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var release = await JsonSerializer
            .DeserializeAsync<ReleaseDto>(responseStream, cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("GitHub returned an empty release response.");

        if (release.Draft || release.PreRelease)
        {
            throw new InvalidDataException("The latest release endpoint returned a non-stable release.");
        }

        var versionText = release.TagName?.StartsWith('v') == true
            ? release.TagName[1..]
            : release.TagName;
        if (!SemanticVersion.TryParse(versionText, out var latestVersion))
        {
            throw new InvalidDataException("The latest release tag is not a supported semantic version.");
        }

        if (latestVersion.CompareTo(currentVersion) <= 0)
        {
            return UpdateCheckResult.UpToDate;
        }

        var installerFileName = $"Unskip-{latestVersion}-win-x64-setup.exe";
        var installerAsset = GetAsset(release.Assets, installerFileName);
        var checksumAsset = GetAsset(release.Assets, "SHA256SUMS.txt");
        if (installerAsset.Size <= 0 || installerAsset.Size > MaximumInstallerSize)
        {
            throw new InvalidDataException("The release installer has an unexpected size.");
        }

        var installerUri = ValidateReleaseUri(
            installerAsset.DownloadUrl,
            release.TagName!,
            installerFileName);
        var checksumUri = ValidateReleaseUri(
            checksumAsset.DownloadUrl,
            release.TagName!,
            "SHA256SUMS.txt");

        return UpdateCheckResult.Available(new ApplicationUpdateRelease(
            latestVersion,
            release.TagName!,
            installerFileName,
            installerUri,
            installerAsset.Size,
            checksumUri));
    }

    public async Task<UpdateDownloadResult> DownloadAsync(
        ApplicationUpdateRelease release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);
        ValidateReleaseUri(release.InstallerUri.ToString(), release.TagName, release.InstallerFileName);
        ValidateReleaseUri(release.ChecksumUri.ToString(), release.TagName, "SHA256SUMS.txt");
        if (release.InstallerSize <= 0 || release.InstallerSize > MaximumInstallerSize)
        {
            throw new InvalidDataException("The release installer has an unexpected size.");
        }

        var expectedHash = await DownloadExpectedHashAsync(release, cancellationToken).ConfigureAwait(false);
        var versionDirectory = Path.Combine(_updatesRoot, $"v{release.Version}");
        Directory.CreateDirectory(versionDirectory);

        var installerPath = Path.Combine(versionDirectory, release.InstallerFileName);
        var temporaryPath = $"{installerPath}.download";
        progress?.Report(0);
        try
        {
            using var request = CreateRequest(release.InstallerUri);
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long downloaded = 0;
            string actualHash;
            await using (var source = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await using var destination = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                while (true)
                {
                    var count = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (count == 0)
                    {
                        break;
                    }

                    downloaded += count;
                    if (downloaded > release.InstallerSize || downloaded > MaximumInstallerSize)
                    {
                        throw new InvalidDataException("The downloaded installer exceeded its declared size.");
                    }

                    hash.AppendData(buffer, 0, count);
                    await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                    progress?.Report((int)(downloaded * 100 / release.InstallerSize));
                }

                actualHash = Convert.ToHexStringLower(hash.GetHashAndReset());
            }

            if (downloaded != release.InstallerSize)
            {
                throw new InvalidDataException("The downloaded installer size did not match the release metadata.");
            }

            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The downloaded installer checksum did not match the release.");
            }

            File.Move(temporaryPath, installerPath, true);
            progress?.Report(100);
            return new UpdateDownloadResult(installerPath, expectedHash);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    public async Task<bool> VerifyAsync(
        UpdateDownloadResult download,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(download);
        var fullPath = Path.GetFullPath(download.InstallerPath);
        var rootPrefix = _updatesRoot.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(fullPath))
        {
            return false;
        }

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var actualHash = Convert.ToHexStringLower(
            await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
        return string.Equals(actualHash, download.Sha256, StringComparison.Ordinal);
    }

    private async Task<string> DownloadExpectedHashAsync(
        ApplicationUpdateRelease release,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(release.ChecksumUri);
        using var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumChecksumLength)
        {
            throw new InvalidDataException("The release checksum file is unexpectedly large.");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (content.Length > MaximumChecksumLength)
        {
            throw new InvalidDataException("The release checksum file is unexpectedly large.");
        }

        foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf("  ", StringComparison.Ordinal);
            if (separator != 64
                || !string.Equals(line[(separator + 2)..], release.InstallerFileName, StringComparison.Ordinal)
                || !line[..separator].All(Uri.IsHexDigit))
            {
                continue;
            }

            return line[..separator].ToLower(CultureInfo.InvariantCulture);
        }

        throw new InvalidDataException("The release checksum file did not contain the expected installer.");
    }

    private static HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("Unskip-Update-Client/1.0");
        return request;
    }

    private static AssetDto GetAsset(IReadOnlyList<AssetDto>? assets, string expectedName)
    {
        var matches = assets?
            .Where(asset => string.Equals(asset.Name, expectedName, StringComparison.Ordinal)
                && string.Equals(asset.State, "uploaded", StringComparison.Ordinal))
            .ToList();
        return matches is { Count: 1 }
            ? matches[0]
            : throw new InvalidDataException($"The release did not contain exactly one {expectedName} asset.");
    }

    private static Uri ValidateReleaseUri(string? value, string tagName, string fileName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A release asset URL was not a trusted GitHub HTTPS URL.");
        }

        var expectedPath = $"{RepositoryPath}releases/download/{Uri.EscapeDataString(tagName)}/{Uri.EscapeDataString(fileName)}";
        if (!string.Equals(uri.AbsolutePath, expectedPath, StringComparison.Ordinal))
        {
            throw new InvalidDataException("A release asset URL did not match the expected repository path.");
        }

        return uri;
    }

    private sealed record ReleaseDto(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool PreRelease,
        [property: JsonPropertyName("assets")] IReadOnlyList<AssetDto>? Assets);

    private sealed record AssetDto(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("state")] string? State,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("browser_download_url")] string? DownloadUrl);
}
