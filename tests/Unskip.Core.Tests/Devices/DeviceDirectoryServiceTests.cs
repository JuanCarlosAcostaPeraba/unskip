using Unskip.Core.Devices;
using Unskip.Core.Time;

namespace Unskip.Core.Tests.Devices;

public sealed class DeviceDirectoryServiceTests
{
    [Fact]
    public async Task CreateAssignsStableIdentityAndClockTimestamps()
    {
        var repository = new RecordingDeviceRepository();
        var timestamp = new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero);
        var service = new DeviceDirectoryService(repository, new FixedClock(timestamp));

        var result = await service.CreateAsync(new DeviceInput(
            "Reception",
            "front-desk",
            null,
            null));

        Assert.True(result.IsSuccessful);
        Assert.NotEqual(Guid.Empty, result.Device!.Id);
        Assert.Equal(timestamp, result.Device.CreatedAt);
        Assert.Equal(timestamp, result.Device.UpdatedAt);
        Assert.Null(result.Device.LastUsedAt);
        Assert.Same(result.Device, repository.StoredDevice);
    }

    [Fact]
    public async Task InvalidCreateNeverTouchesRepository()
    {
        var repository = new RecordingDeviceRepository();
        var service = new DeviceDirectoryService(repository, new FixedClock(DateTimeOffset.UtcNow));

        var result = await service.CreateAsync(new DeviceInput(null, null, null, null));

        Assert.Equal(DeviceMutationStatus.ValidationFailed, result.Status);
        Assert.Equal(0, repository.WriteCount);
    }

    [Fact]
    public async Task MarkLastUsedUsesInjectedClock()
    {
        var createdAt = new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero);
        var usedAt = createdAt.AddDays(1);
        var repository = new RecordingDeviceRepository
        {
            StoredDevice = CreateDevice(createdAt),
        };
        var service = new DeviceDirectoryService(repository, new FixedClock(usedAt));

        var result = await service.MarkLastUsedAsync(repository.StoredDevice.Id);

        Assert.True(result.IsSuccessful);
        Assert.Equal(usedAt, result.Device!.LastUsedAt);
        Assert.Equal(usedAt, result.Device.UpdatedAt);
        Assert.Equal(createdAt, result.Device.CreatedAt);
    }

    [Fact]
    public async Task MissingDeviceReturnsNotFound()
    {
        var service = new DeviceDirectoryService(
            new RecordingDeviceRepository(),
            new FixedClock(DateTimeOffset.UtcNow));

        var result = await service.SetFavoriteAsync(Guid.NewGuid(), true);

        Assert.Equal(DeviceMutationStatus.NotFound, result.Status);
    }

    private static Device CreateDevice(DateTimeOffset timestamp)
    {
        return new Device(
            Guid.NewGuid(),
            "Reception",
            "RECEPTION",
            "front-desk",
            null,
            null,
            false,
            DeviceDestinationKind.Hostname,
            timestamp,
            timestamp,
            null);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class RecordingDeviceRepository : IDeviceRepository
    {
        public Device? StoredDevice { get; set; }

        public int WriteCount { get; private set; }

        public Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Device> devices = StoredDevice is null ? [] : [StoredDevice];
            return Task.FromResult(devices);
        }

        public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StoredDevice?.Id == id ? StoredDevice : null);
        }

        public Task<DeviceStoreWriteStatus> AddAsync(
            Device device,
            CancellationToken cancellationToken = default)
        {
            WriteCount++;
            StoredDevice = device;
            return Task.FromResult(DeviceStoreWriteStatus.Saved);
        }

        public Task<DeviceStoreWriteStatus> UpdateAsync(
            Device device,
            CancellationToken cancellationToken = default)
        {
            WriteCount++;
            StoredDevice = device;
            return Task.FromResult(DeviceStoreWriteStatus.Saved);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (StoredDevice?.Id != id)
            {
                return Task.FromResult(false);
            }

            WriteCount++;
            StoredDevice = null;
            return Task.FromResult(true);
        }
    }
}
