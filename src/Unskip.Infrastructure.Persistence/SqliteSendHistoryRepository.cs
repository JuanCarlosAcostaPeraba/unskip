using Microsoft.EntityFrameworkCore;
using Unskip.Core.Messaging.History;

namespace Unskip.Infrastructure.Persistence;

public sealed class SqliteSendHistoryRepository(UnskipDbContextFactory contextFactory) : ISendHistoryRepository
{
    private readonly UnskipDbContextFactory _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task<IReadOnlyList<SendHistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var entities = await context.SendHistoryRecords.AsNoTracking()
            .OrderByDescending(record => record.OccurredAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.ConvertAll(SendHistoryMapper.ToDomain);
    }

    public async Task AddAsync(SendHistoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await using var context = _contextFactory.CreateDbContext();
        context.SendHistoryRecords.Add(SendHistoryMapper.ToEntity(record));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.SendHistoryRecords.Where(record => record.Id == id)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    public async Task<int> ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.SendHistoryRecords.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
