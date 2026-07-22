namespace Unskip.Infrastructure.Persistence.Tests;

public sealed class LocalApplicationDataPathProviderTests
{
    [Fact]
    public void DatabasePathUsesCurrentUsersLocalApplicationData()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        var result = LocalApplicationDataPathProvider.GetDatabasePath();

        Assert.Equal(
            Path.Combine(localApplicationData, "Unskip", "unskip.db"),
            result);
    }
}
