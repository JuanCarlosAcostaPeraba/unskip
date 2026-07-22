using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Unskip.Core.Devices;

namespace Unskip.Infrastructure.Persistence;

public sealed class SqliteDeviceRepository(UnskipDbContextFactory contextFactory) : IDeviceRepository
{
    private readonly UnskipDbContextFactory _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task<IReadOnlyList<Device>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var entities = await context.Devices
            .AsNoTracking()
            .OrderByDescending(device => device.IsFavorite)
            .ThenByDescending(device => device.LastUsedAt)
            .ThenBy(device => device.Alias)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.ConvertAll(DeviceMapper.ToDomain);
    }

    public async Task<Device?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var entity = await context.Devices
            .AsNoTracking()
            .SingleOrDefaultAsync(device => device.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DeviceMapper.ToDomain(entity);
    }

    public async Task<DeviceStoreWriteStatus> AddAsync(
        Device device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        await using var context = _contextFactory.CreateDbContext();
        context.Devices.Add(DeviceMapper.ToEntity(device));
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DeviceStoreWriteStatus> UpdateAsync(
        Device device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        await using var context = _contextFactory.CreateDbContext();
        var entity = await context.Devices
            .SingleOrDefaultAsync(candidate => candidate.Id == device.Id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return DeviceStoreWriteStatus.NotFound;
        }

        DeviceMapper.CopyToEntity(device, entity);
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var entity = await context.Devices
            .SingleOrDefaultAsync(device => device.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return false;
        }

        context.Devices.Remove(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static async Task<DeviceStoreWriteStatus> SaveAsync(
        UnskipDbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return DeviceStoreWriteStatus.Saved;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return DeviceStoreWriteStatus.Conflict;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException
            && sqliteException.SqliteErrorCode == 19
            && sqliteException.SqliteExtendedErrorCode is 1555 or 2067;
    }
}
