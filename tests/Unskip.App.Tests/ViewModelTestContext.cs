using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.Core.Devices;
using Unskip.Core.Links;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;
using Unskip.Core.Time;
using Unskip.Core.Updates;

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
        HistoryRepository = new InMemorySendHistoryRepository();
        HistoryConfirmation = new StubHistoryConfirmation();
        var history = new SendHistoryService(HistoryRepository, Clock);
        UpdateService = new StubApplicationUpdateService();
        UpdateInstaller = new StubUpdateInstallerLauncher();
        ApplicationShutdown = new StubApplicationShutdown();
        ExternalUriLauncher = new StubExternalUriLauncher();
        UrgentAttentionPreview = new StubUrgentAttentionPreviewService();
        Updates = new ApplicationUpdateViewModel(
            UpdateService,
            UpdateInstaller,
            ApplicationShutdown,
            SemanticVersion.Parse("0.1.0"));
        Main = new MainWindowViewModel(
            Directory,
            Sender,
            history,
            HistoryConfirmation,
            Updates,
            ExternalUriLauncher,
            UrgentAttentionPreview,
            "Version 0.1.0-test");
    }

    public MutableClock Clock { get; }

    public InMemoryDeviceRepository Repository { get; }

    public StubConfirmation Confirmation { get; }

    public StubMessageSender Sender { get; }

    public InMemorySendHistoryRepository HistoryRepository { get; }

    public StubHistoryConfirmation HistoryConfirmation { get; }

    public StubApplicationUpdateService UpdateService { get; }

    public StubUpdateInstallerLauncher UpdateInstaller { get; }

    public StubApplicationShutdown ApplicationShutdown { get; }

    public StubExternalUriLauncher ExternalUriLauncher { get; }

    public StubUrgentAttentionPreviewService UrgentAttentionPreview { get; }

    public DeviceDirectoryViewModel Directory { get; }

    public ApplicationUpdateViewModel Updates { get; }

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

    internal sealed class StubHistoryConfirmation : IHistoryDeletionConfirmation
    {
        public bool Response { get; set; }

        public Task<bool> ConfirmDeleteAsync(string destinationAlias) => Task.FromResult(Response);

        public Task<bool> ConfirmClearAsync(int count) => Task.FromResult(Response);
    }

    internal sealed class StubApplicationUpdateService : IApplicationUpdateService
    {
        public UpdateCheckResult CheckResult { get; set; } = UpdateCheckResult.UpToDate;

        public UpdateDownloadResult DownloadResult { get; set; } =
            new(@"C:\updates\Unskip-0.2.0-win-x64-setup.exe", new string('a', 64));

        public bool VerificationResult { get; set; } = true;

        public Exception? CheckException { get; set; }

        public Exception? DownloadException { get; set; }

        public int CheckCount { get; private set; }

        public int DownloadCount { get; private set; }

        public int VerificationCount { get; private set; }

        public Task<UpdateCheckResult> CheckForUpdateAsync(
            SemanticVersion currentVersion,
            CancellationToken cancellationToken = default)
        {
            CheckCount++;
            return CheckException is null
                ? Task.FromResult(CheckResult)
                : Task.FromException<UpdateCheckResult>(CheckException);
        }

        public Task<UpdateDownloadResult> DownloadAsync(
            ApplicationUpdateRelease release,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            DownloadCount++;
            progress?.Report(100);
            return DownloadException is null
                ? Task.FromResult(DownloadResult)
                : Task.FromException<UpdateDownloadResult>(DownloadException);
        }

        public Task<bool> VerifyAsync(
            UpdateDownloadResult download,
            CancellationToken cancellationToken = default)
        {
            VerificationCount++;
            return Task.FromResult(VerificationResult);
        }
    }

    internal sealed class StubUpdateInstallerLauncher : IUpdateInstallerLauncher
    {
        public bool Result { get; set; } = true;

        public string? InstallerPath { get; private set; }

        public bool TryLaunch(string installerPath)
        {
            InstallerPath = installerPath;
            return Result;
        }
    }

    internal sealed class StubApplicationShutdown : IApplicationShutdown
    {
        public int RequestCount { get; private set; }

        public void Shutdown()
        {
            RequestCount++;
        }
    }

    internal sealed class StubExternalUriLauncher : IExternalUriLauncher
    {
        public bool Result { get; set; } = true;

        public Uri? OpenedUri { get; private set; }

        public bool TryOpen(Uri uri)
        {
            OpenedUri = uri;
            return Result;
        }
    }

    internal sealed class StubUrgentAttentionPreviewService : IUrgentAttentionPreviewService
    {
        public int ShowCount { get; private set; }

        public string? Message { get; private set; }

        public Exception? Exception { get; set; }

        public Task ShowAsync(string message, CancellationToken cancellationToken = default)
        {
            ShowCount++;
            Message = message;
            return Exception is null
                ? Task.CompletedTask
                : Task.FromException(Exception);
        }
    }

    internal sealed class InMemorySendHistoryRepository : ISendHistoryRepository
    {
        public List<SendHistoryRecord> Records { get; } = [];

        public Task<IReadOnlyList<SendHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SendHistoryRecord>>([.. Records.OrderByDescending(record => record.OccurredAt)]);

        public Task AddAsync(SendHistoryRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.RemoveAll(record => record.Id == id) > 0);

        public Task<int> ClearAsync(CancellationToken cancellationToken = default)
        {
            var count = Records.Count;
            Records.Clear();
            return Task.FromResult(count);
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
