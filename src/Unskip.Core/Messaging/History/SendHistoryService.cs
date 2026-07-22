using Unskip.Core.Time;

namespace Unskip.Core.Messaging.History;

public sealed class SendHistoryService(ISendHistoryRepository repository, IClock clock)
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ISendHistoryRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public Task<IReadOnlyList<SendHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public Task<int> ClearAsync(CancellationToken cancellationToken = default) =>
        _repository.ClearAsync(cancellationToken);

    public async Task<SendHistoryRecord> RecordAsync(
        SendHistoryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        var diagnostic = attempt.DiagnosticSummary?.Trim();
        if (diagnostic?.Length > SendHistoryPolicy.MaximumDiagnosticSummaryLength)
        {
            diagnostic = diagnostic[..SendHistoryPolicy.MaximumDiagnosticSummaryLength];
        }

        var record = new SendHistoryRecord(
            Guid.NewGuid(), attempt.DeviceId, attempt.AliasSnapshot,
            attempt.ComputerNameSnapshot, attempt.Ipv4AddressSnapshot,
            attempt.DestinationKind, attempt.DestinationSnapshot,
            attempt.DeliveryStatus, attempt.FailureCategory, _clock.UtcNow,
            attempt.Duration, attempt.ExitCode, diagnostic, attempt.MessageLength);
        await _repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return record;
    }
}
