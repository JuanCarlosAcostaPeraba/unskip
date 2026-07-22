using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;
using Unskip.Core.Time;

namespace Unskip.Infrastructure.Persistence.Tests;

public sealed class SqliteSendHistoryRepositoryTests
{
    [Fact]
    public async Task RoundTripDeleteAndClearPreserveAllMetadata()
    {
        await using var database = await TestDatabase.CreateAsync();
        var first = Record("Reception", MessageDeliveryStatus.Sent);
        var second = Record("Workshop", MessageDeliveryStatus.Failed);
        await database.Database.SendHistory.AddAsync(first);
        await database.Database.SendHistory.AddAsync(second);

        var loaded = await database.Database.SendHistory.GetAllAsync();
        var deleted = await database.Database.SendHistory.DeleteAsync(first.Id);
        var cleared = await database.Database.SendHistory.ClearAsync();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("front-desk", loaded.Single(item => item.Id == first.Id).ComputerNameSnapshot);
        Assert.Equal(MessageFailureCategory.ProcessFailure, loaded.Single(item => item.Id == second.Id).FailureCategory);
        Assert.True(deleted);
        Assert.Equal(1, cleared);
        Assert.Empty(await database.Database.SendHistory.GetAllAsync());
    }

    [Fact]
    public async Task DeviceChangesCannotRewriteHistoricalSnapshots()
    {
        await using var database = await TestDatabase.CreateAsync();
        var clock = new FixedClock(new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero));
        var directory = new DeviceDirectoryService(database.Database.Devices, clock);
        var created = (await directory.CreateAsync(new DeviceInput("Reception", "front-desk", null, null))).Device!;
        var record = Record("Reception", MessageDeliveryStatus.Sent) with { DeviceId = created.Id };
        await database.Database.SendHistory.AddAsync(record);

        await directory.UpdateAsync(created.Id, new DeviceInput("Renamed", "renamed-pc", null, null));
        await directory.DeleteAsync(created.Id);

        var loaded = Assert.Single(await database.Database.SendHistory.GetAllAsync());
        Assert.Null(loaded.DeviceId);
        Assert.Equal("Reception", loaded.AliasSnapshot);
        Assert.Equal("front-desk", loaded.ComputerNameSnapshot);
    }

    private static SendHistoryRecord Record(string alias, MessageDeliveryStatus status) => new(
        Guid.NewGuid(), null, alias, "front-desk", "192.0.2.8",
        DeviceDestinationKind.Hostname, "front-desk", status,
        status == MessageDeliveryStatus.Sent ? MessageFailureCategory.None : MessageFailureCategory.ProcessFailure,
        new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero),
        TimeSpan.FromMilliseconds(30), status == MessageDeliveryStatus.Sent ? 0 : 5,
        "Sanitized diagnostic", 21);

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock { public DateTimeOffset UtcNow { get; } = utcNow; }

    private sealed class TestDatabase(string path) : IAsyncDisposable
    {
        public UnskipDatabase Database { get; } = new(path);
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), "Unskip.Tests", Guid.NewGuid().ToString("N"), "unskip.db");
            var value = new TestDatabase(path);
            await value.Database.InitializeAsync();
            return value;
        }
        public ValueTask DisposeAsync()
        {
            var directory = Path.GetDirectoryName(Database.DatabasePath);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
