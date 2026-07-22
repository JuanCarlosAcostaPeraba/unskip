using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Unskip.Infrastructure.Persistence;

public sealed class UnskipDbContextFactory
{
    private readonly DbContextOptions<UnskipDbContext> _options;

    public UnskipDbContextFactory(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        DatabasePath = Path.GetFullPath(databasePath);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = false,
        }.ToString();

        _options = new DbContextOptionsBuilder<UnskipDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }

    public string DatabasePath { get; }

    public UnskipDbContext CreateDbContext()
    {
        return new UnskipDbContext(_options);
    }
}
