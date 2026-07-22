namespace Unskip.Infrastructure.Persistence;

public static class LocalApplicationDataPathProvider
{
    public static string GetDatabasePath()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("The current user's local application data directory is unavailable.");
        }

        return Path.Combine(localApplicationData, "Unskip", "unskip.db");
    }
}
