using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;
using Unskip.Core.Time;

namespace Unskip.Core.Tests.Messaging;

public sealed class SendHistoryServiceTests
{
    [Fact]
    public async Task RecordAssignsIdentityClockAndTruncatesDiagnostic()
    {
        var repository = new RecordingRepository();
        var timestamp = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var service = new SendHistoryService(repository, new FixedClock(timestamp));

        var record = await service.RecordAsync(new SendHistoryAttempt(
            null, "Reception", "front-desk", null, DeviceDestinationKind.Hostname,
            "front-desk", MessageDeliveryStatus.Failed, MessageFailureCategory.ProcessFailure,
            TimeSpan.FromMilliseconds(25), 5,
            new string('d', SendHistoryPolicy.MaximumDiagnosticSummaryLength + 10), 42));

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(timestamp, record.OccurredAt);
        Assert.Equal(SendHistoryPolicy.MaximumDiagnosticSummaryLength, record.DiagnosticSummary!.Length);
        Assert.Equal(42, record.MessageLength);
        Assert.Same(record, Assert.Single(repository.Records));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock { public DateTimeOffset UtcNow { get; } = utcNow; }

    private sealed class RecordingRepository : ISendHistoryRepository
    {
        public List<SendHistoryRecord> Records { get; } = [];
        public Task<IReadOnlyList<SendHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SendHistoryRecord>>(Records);
        public Task AddAsync(SendHistoryRecord record, CancellationToken cancellationToken = default) { Records.Add(record); return Task.CompletedTask; }
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> ClearAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
