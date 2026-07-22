using Microsoft.EntityFrameworkCore;
using Unskip.Core.Devices;

namespace Unskip.Infrastructure.Persistence;

public sealed class UnskipDatabase
{
    private readonly UnskipDbContextFactory _contextFactory;

    public UnskipDatabase(string databasePath)
    {
        _contextFactory = new UnskipDbContextFactory(databasePath);
        Devices = new SqliteDeviceRepository(_contextFactory);
        SendHistory = new SqliteSendHistoryRepository(_contextFactory);
    }

    public string DatabasePath => _contextFactory.DatabasePath;

    public IDeviceRepository Devices { get; }

    public Core.Messaging.History.ISendHistoryRepository SendHistory { get; }

    public static UnskipDatabase ForCurrentUser()
    {
        var databasePath = LocalApplicationDataPathProvider.GetDatabasePath();
        return new UnskipDatabase(databasePath);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(DatabasePath)
            ?? throw new InvalidOperationException("The database path has no parent directory.");
        Directory.CreateDirectory(directory);

        await using var context = _contextFactory.CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
