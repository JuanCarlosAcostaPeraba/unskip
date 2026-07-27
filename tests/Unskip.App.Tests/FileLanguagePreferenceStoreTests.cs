using Unskip.App.Services;

namespace Unskip.App.Tests;

public sealed class FileLanguagePreferenceStoreTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        "Unskip.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoadRoundTripUsesLocalUtf8File()
    {
        var path = Path.Combine(_testDirectory, "nested", "language.txt");
        var store = new FileLanguagePreferenceStore(path);

        var saved = store.TrySave("es");

        Assert.True(saved);
        Assert.Equal("es", store.Load());
        Assert.Equal(
            [0x65, 0x73],
            File.ReadAllBytes(path));
    }

    [Fact]
    public void MissingPreferenceReturnsNull()
    {
        var store = new FileLanguagePreferenceStore(
            Path.Combine(_testDirectory, "language.txt"));

        Assert.Null(store.Load());
    }

    [Fact]
    public void RelativePreferencePathIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new FileLanguagePreferenceStore("language.txt"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
