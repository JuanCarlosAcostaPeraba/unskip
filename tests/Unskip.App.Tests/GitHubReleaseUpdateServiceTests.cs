using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Unskip.App.Services;
using Unskip.Core.Updates;

namespace Unskip.App.Tests;

public sealed class GitHubReleaseUpdateServiceTests
{
    [Fact]
    public async Task LatestStableReleaseMapsExpectedTrustedAssets()
    {
        using var temporaryDirectory = new TemporaryUpdateDirectory();
        using var client = new HttpClient(new DelegateHandler(_ => JsonResponse(CreateReleaseJson("0.2.0", 4))));
        var service = new GitHubReleaseUpdateService(client, temporaryDirectory.Path);

        var result = await service.CheckForUpdateAsync(SemanticVersion.Parse("0.1.0"));

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("0.2.0", result.Release!.Version.ToString());
        Assert.Equal("Unskip-0.2.0-win-x64-setup.exe", result.Release.InstallerFileName);
        Assert.Equal("github.com", result.Release.InstallerUri.Host);
    }

    [Fact]
    public async Task InstallerIsPersistedOnlyAfterChecksumVerification()
    {
        var installer = new byte[] { 1, 2, 3, 4 };
        var hash = Convert.ToHexStringLower(SHA256.HashData(installer));
        var release = CreateRelease("0.2.0", installer.Length);
        using var temporaryDirectory = new TemporaryUpdateDirectory();
        using var client = new HttpClient(new DelegateHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith("SHA256SUMS.txt", StringComparison.Ordinal)
                ? TextResponse($"{hash}  {release.InstallerFileName}\n")
                : BinaryResponse(installer)));
        var service = new GitHubReleaseUpdateService(client, temporaryDirectory.Path);

        var download = await service.DownloadAsync(release);

        Assert.True(File.Exists(download.InstallerPath));
        Assert.Equal(installer, await File.ReadAllBytesAsync(download.InstallerPath));
        Assert.True(await service.VerifyAsync(download));
        Assert.False(File.Exists($"{download.InstallerPath}.download"));
    }

    [Fact]
    public async Task MismatchedChecksumDeletesPartialDownload()
    {
        var installer = new byte[] { 1, 2, 3, 4 };
        var release = CreateRelease("0.2.0", installer.Length);
        using var temporaryDirectory = new TemporaryUpdateDirectory();
        using var client = new HttpClient(new DelegateHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith("SHA256SUMS.txt", StringComparison.Ordinal)
                ? TextResponse($"{new string('0', 64)}  {release.InstallerFileName}\n")
                : BinaryResponse(installer)));
        var service = new GitHubReleaseUpdateService(client, temporaryDirectory.Path);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(release));

        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task AssetFromAnotherRepositoryIsRejected()
    {
        using var temporaryDirectory = new TemporaryUpdateDirectory();
        var json = CreateReleaseJson(
            "0.2.0",
            4,
            "https://github.com/example/other/releases/download/v0.2.0/");
        using var client = new HttpClient(new DelegateHandler(_ => JsonResponse(json)));
        var service = new GitHubReleaseUpdateService(client, temporaryDirectory.Path);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.CheckForUpdateAsync(SemanticVersion.Parse("0.1.0")));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task NonStableReleaseIsRejected(bool draft, bool preRelease)
    {
        using var temporaryDirectory = new TemporaryUpdateDirectory();
        var json = CreateReleaseJson("0.2.0", 4, draft: draft, preRelease: preRelease);
        using var client = new HttpClient(new DelegateHandler(_ => JsonResponse(json)));
        var service = new GitHubReleaseUpdateService(client, temporaryDirectory.Path);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.CheckForUpdateAsync(SemanticVersion.Parse("0.1.0")));
    }

    private static ApplicationUpdateRelease CreateRelease(string version, long size)
    {
        var tag = $"v{version}";
        var installerName = $"Unskip-{version}-win-x64-setup.exe";
        var baseUri = $"https://github.com/JuanCarlosAcostaPeraba/unskip/releases/download/{tag}/";
        return new ApplicationUpdateRelease(
            SemanticVersion.Parse(version),
            tag,
            installerName,
            new Uri($"{baseUri}{installerName}"),
            size,
            new Uri($"{baseUri}SHA256SUMS.txt"));
    }

    private static string CreateReleaseJson(
        string version,
        long size,
        string? baseUri = null,
        bool draft = false,
        bool preRelease = false)
    {
        var tag = $"v{version}";
        var installerName = $"Unskip-{version}-win-x64-setup.exe";
        baseUri ??= $"https://github.com/JuanCarlosAcostaPeraba/unskip/releases/download/{tag}/";
        return JsonSerializer.Serialize(new
        {
            tag_name = tag,
            draft,
            prerelease = preRelease,
            assets = new object[]
            {
                new
                {
                    name = installerName,
                    state = "uploaded",
                    size,
                    browser_download_url = $"{baseUri}{installerName}",
                },
                new
                {
                    name = "SHA256SUMS.txt",
                    state = "uploaded",
                    size = 190,
                    browser_download_url = $"{baseUri}SHA256SUMS.txt",
                },
            },
        });
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage TextResponse(string text) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(text, Encoding.ASCII, "text/plain"),
        };

    private static HttpResponseMessage BinaryResponse(byte[] content) =>
        new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content),
        };

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private sealed class TemporaryUpdateDirectory : IDisposable
    {
        public TemporaryUpdateDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "Unskip.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
