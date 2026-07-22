using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Time;

namespace Unskip.App.Tests;

internal sealed class ViewModelTestContext
{
    private ViewModelTestContext(IReadOnlyList<Device> devices)
    {
        Clock = new MutableClock(new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero));
        Repository = new InMemoryDeviceRepository(devices);
        Confirmation = new StubConfirmation();
        var service = new DeviceDirectoryService(Repository, Clock);
        Directory = new DeviceDirectoryViewModel(service, Clock, Confirmation);
        Sender = new StubMessageSender();
        Main = new MainWindowViewModel(Directory, Sender);
    }

    public MutableClock Clock { get; }

    public InMemoryDeviceRepository Repository { get; }

    public StubConfirmation Confirmation { get; }

    public StubMessageSender Sender { get; }

    public DeviceDirectoryViewModel Directory { get; }

    public MainWindowViewModel Main { get; }

    public static ViewModelTestContext Create(params Device[] devices)
    {
        return new ViewModelTestContext(devices);
    }

    public static Device Device(
        string deviceAlias,
        string? computerName,
        string? ipv4Address,
        DeviceDestinationKind preferredDestination = DeviceDestinationKind.Hostname,
        bool isFavorite = false,
        string? description = null)
    {
        var timestamp = new DateTimeOffset(2026, 7, 21, 9, 0, 0, TimeSpan.Zero);
        return new Device(
            Guid.NewGuid(),
            deviceAlias,
            DeviceValidator.CreateAliasKey(deviceAlias),
            computerName,
            ipv4Address,
            description,
            isFavorite,
            preferredDestination,
            timestamp,
            timestamp,
            null);
    }

    internal sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    internal sealed class StubConfirmation : IDeviceDeletionConfirmation
    {
        public bool Response { get; set; }

        public int RequestCount { get; private set; }

        public Task<bool> ConfirmAsync(string deviceAlias)
        {
            RequestCount++;
            return Task.FromResult(Response);
        }
    }

    internal sealed class StubMessageSender : IMessageSender
    {
        public List<MessageRequest> Requests { get; } = [];

        public MessageSendResult Result { get; set; } = new(
            MessageDeliveryStatus.Sent,
            MessageFailureCategory.None,
            0,
            string.Empty,
            string.Empty,
            TimeSpan.FromMilliseconds(20),
            "Windows accepted the message request. This does not confirm that a person read it.");

        public Task<MessageSendResult> SendAsync(
            MessageRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Result);
        }
    }

    internal sealed class InMemoryDeviceRepository(IReadOnlyList<Device> devices) : IDeviceRepository
    {
        private readonly List<Device> _devices = [.. devices];

        public Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Device> result = _devices
                .OrderByDescending(device => device.IsFavorite)
                .ThenByDescending(device => device.LastUsedAt)
                .ThenBy(device => device.Alias)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_devices.SingleOrDefault(device => device.Id == id));
        }

        public Task<DeviceStoreWriteStatus> AddAsync(
            Device device,
            CancellationToken cancellationToken = default)
        {
            if (HasConflict(device))
            {
                return Task.FromResult(DeviceStoreWriteStatus.Conflict);
            }

            _devices.Add(device);
            return Task.FromResult(DeviceStoreWriteStatus.Saved);
        }

        public Task<DeviceStoreWriteStatus> UpdateAsync(
            Device device,
            CancellationToken cancellationToken = default)
        {
            var index = _devices.FindIndex(candidate => candidate.Id == device.Id);
            if (index < 0)
            {
                return Task.FromResult(DeviceStoreWriteStatus.NotFound);
            }

            if (HasConflict(device))
            {
                return Task.FromResult(DeviceStoreWriteStatus.Conflict);
            }

            _devices[index] = device;
            return Task.FromResult(DeviceStoreWriteStatus.Saved);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = _devices.RemoveAll(device => device.Id == id) > 0;
            return Task.FromResult(removed);
        }

        private bool HasConflict(Device device)
        {
            return _devices.Any(candidate => candidate.Id != device.Id
                && (candidate.AliasKey == device.AliasKey
                    || SameOptional(candidate.ComputerName, device.ComputerName)
                    || SameOptional(candidate.Ipv4Address, device.Ipv4Address)));
        }

        private static bool SameOptional(string? left, string? right)
        {
            return left is not null
                && right is not null
                && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
