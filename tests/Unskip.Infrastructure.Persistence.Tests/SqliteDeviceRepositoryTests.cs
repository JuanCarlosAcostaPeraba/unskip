using Microsoft.EntityFrameworkCore;
using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Time;

namespace Unskip.Infrastructure.Persistence.Tests;

public sealed class SqliteDeviceRepositoryTests
{
    [Fact]
    public async Task InitializationAppliesEveryMigration()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        await using var context = database.ContextFactory.CreateDbContext();

        var applied = await context.Database.GetAppliedMigrationsAsync();
        var pending = await context.Database.GetPendingMigrationsAsync();

        Assert.Contains(applied, migration => migration.EndsWith("_InitialLocalDeviceDirectory", StringComparison.Ordinal));
        Assert.Empty(pending);
        Assert.True(File.Exists(database.DatabasePath));
    }

    [Fact]
    public async Task CrudFavoriteAndLastUsedOperationsRoundTrip()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero));
        var directory = new DeviceDirectoryService(database.Database.Devices, clock);

        var created = await directory.CreateAsync(new DeviceInput(
            "Joan",
            "chuc159",
            "10.198.198.4",
            "Fictitious test device",
            PreferredDestination: DeviceDestinationKind.Hostname));

        Assert.True(created.IsSuccessful);
        var id = created.Device!.Id;
        Assert.Equal(created.Device, await directory.GetByIdAsync(id));

        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        var updated = await directory.UpdateAsync(id, new DeviceInput(
            "Joan - Desk",
            "chuc159",
            "10.198.198.4",
            "Updated fictitious device",
            PreferredDestination: DeviceDestinationKind.Ipv4));
        Assert.True(updated.IsSuccessful);
        Assert.Equal(created.Device.CreatedAt, updated.Device!.CreatedAt);
        Assert.Equal("Joan - Desk", updated.Device.Alias);

        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        var favorite = await directory.SetFavoriteAsync(id, true);
        Assert.True(favorite.IsSuccessful);
        Assert.True(favorite.Device!.IsFavorite);

        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        var used = await directory.MarkLastUsedAsync(id);
        Assert.True(used.IsSuccessful);
        Assert.Equal(clock.UtcNow, used.Device!.LastUsedAt);

        var all = await directory.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(id, all[0].Id);

        var deleted = await directory.DeleteAsync(id);
        Assert.True(deleted.IsSuccessful);
        Assert.Null(await directory.GetByIdAsync(id));
    }

    [Fact]
    public async Task MeaningfulDuplicatesReturnConflict()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var directory = new DeviceDirectoryService(
            database.Database.Devices,
            new MutableClock(DateTimeOffset.UtcNow));

        var first = await directory.CreateAsync(new DeviceInput(
            "Reception",
            "front-desk",
            "192.0.2.10",
            null));
        var duplicateAlias = await directory.CreateAsync(new DeviceInput(
            "reception",
            "front-desk-2",
            "192.0.2.11",
            null));
        var duplicateHostname = await directory.CreateAsync(new DeviceInput(
            "Lobby",
            "FRONT-DESK",
            "192.0.2.12",
            null));
        var duplicateIpv4 = await directory.CreateAsync(new DeviceInput(
            "Warehouse",
            "warehouse-1",
            "192.0.2.10",
            null));

        Assert.True(first.IsSuccessful);
        Assert.Equal(DeviceMutationStatus.Conflict, duplicateAlias.Status);
        Assert.Equal(DeviceMutationStatus.Conflict, duplicateHostname.Status);
        Assert.Equal(DeviceMutationStatus.Conflict, duplicateIpv4.Status);
    }

    [Fact]
    public async Task EditingAndDeletingDevicePreservesHistoricalSnapshot()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var directory = new DeviceDirectoryService(
            database.Database.Devices,
            new MutableClock(new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero)));
        var created = await directory.CreateAsync(new DeviceInput(
            "Reception",
            "front-desk",
            null,
            null));
        var id = created.Device!.Id;

        await using (var context = database.ContextFactory.CreateDbContext())
        {
            context.SendHistoryRecords.Add(new SendHistoryEntity
            {
                Id = Guid.NewGuid(),
                DeviceId = id,
                AliasSnapshot = "Reception",
                DestinationKind = DeviceDestinationKind.Hostname,
                DestinationSnapshot = "front-desk",
                DeliveryStatus = MessageDeliveryStatus.Sent,
                OccurredAt = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        var updated = await directory.UpdateAsync(id, new DeviceInput(
            "Reception Renamed",
            "front-desk-new",
            null,
            null));
        var deleted = await directory.DeleteAsync(id);

        await using var verificationContext = database.ContextFactory.CreateDbContext();
        var history = await verificationContext.SendHistoryRecords.AsNoTracking().SingleAsync();
        Assert.True(updated.IsSuccessful);
        Assert.True(deleted.IsSuccessful);
        Assert.Null(history.DeviceId);
        Assert.Equal("Reception", history.AliasSnapshot);
        Assert.Equal("front-desk", history.DestinationSnapshot);
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class TemporaryDatabase : IAsyncDisposable
    {
        private TemporaryDatabase(string directoryPath)
        {
            DirectoryPath = directoryPath;
            DatabasePath = Path.Combine(directoryPath, "unskip.db");
            ContextFactory = new UnskipDbContextFactory(DatabasePath);
            Database = new UnskipDatabase(DatabasePath);
        }

        public string DirectoryPath { get; }

        public string DatabasePath { get; }

        public UnskipDbContextFactory ContextFactory { get; }

        public UnskipDatabase Database { get; }

        public static async Task<TemporaryDatabase> CreateAsync()
        {
            var directoryPath = Path.Combine(
                Path.GetTempPath(),
                "Unskip.Tests",
                Guid.NewGuid().ToString("N"));
            var database = new TemporaryDatabase(directoryPath);
            await database.Database.InitializeAsync();
            return database;
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
