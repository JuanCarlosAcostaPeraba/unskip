namespace Unskip.Core.Messaging.History;

public interface ISendHistoryRepository
{
    Task<IReadOnlyList<SendHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SendHistoryRecord record, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> ClearAsync(CancellationToken cancellationToken = default);
}
