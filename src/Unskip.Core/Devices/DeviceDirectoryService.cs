using Unskip.Core.Time;

namespace Unskip.Core.Devices;

public sealed class DeviceDirectoryService
{
    private readonly IClock _clock;
    private readonly IDeviceRepository _repository;

    public DeviceDirectoryService(IDeviceRepository repository, IClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<DeviceMutationResult> CreateAsync(
        DeviceInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = DeviceValidator.Validate(input);
        if (!validation.IsValid)
        {
            return DeviceMutationResult.ValidationFailure(validation.Errors);
        }

        var value = validation.Value!;
        var timestamp = _clock.UtcNow;
        var device = new Device(
            Guid.NewGuid(),
            value.Alias,
            value.AliasKey,
            value.ComputerName,
            value.Ipv4Address,
            value.Description,
            value.IsFavorite,
            value.PreferredDestination,
            timestamp,
            timestamp,
            null);

        var status = await _repository.AddAsync(device, cancellationToken).ConfigureAwait(false);
        return MapWriteStatus(status, device);
    }

    public async Task<DeviceMutationResult> UpdateAsync(
        Guid id,
        DeviceInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = DeviceValidator.Validate(input);
        if (!validation.IsValid)
        {
            return DeviceMutationResult.ValidationFailure(validation.Errors);
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return DeviceMutationResult.NotFound();
        }

        var value = validation.Value!;
        var device = existing with
        {
            Alias = value.Alias,
            AliasKey = value.AliasKey,
            ComputerName = value.ComputerName,
            Ipv4Address = value.Ipv4Address,
            Description = value.Description,
            IsFavorite = value.IsFavorite,
            PreferredDestination = value.PreferredDestination,
            UpdatedAt = _clock.UtcNow,
        };

        var status = await _repository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        return MapWriteStatus(status, device);
    }

    public async Task<DeviceMutationResult> SetFavoriteAsync(
        Guid id,
        bool isFavorite,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return DeviceMutationResult.NotFound();
        }

        var device = existing with
        {
            IsFavorite = isFavorite,
            UpdatedAt = _clock.UtcNow,
        };

        var status = await _repository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        return MapWriteStatus(status, device);
    }

    public async Task<DeviceMutationResult> MarkLastUsedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return DeviceMutationResult.NotFound();
        }

        var timestamp = _clock.UtcNow;
        var device = existing with
        {
            LastUsedAt = timestamp,
            UpdatedAt = timestamp,
        };

        var status = await _repository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        return MapWriteStatus(status, device);
    }

    public async Task<DeviceMutationResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return deleted ? DeviceMutationResult.Success() : DeviceMutationResult.NotFound();
    }

    private static DeviceMutationResult MapWriteStatus(
        DeviceStoreWriteStatus status,
        Device device)
    {
        return status switch
        {
            DeviceStoreWriteStatus.Saved => DeviceMutationResult.Success(device),
            DeviceStoreWriteStatus.Conflict => DeviceMutationResult.Conflict(),
            DeviceStoreWriteStatus.NotFound => DeviceMutationResult.NotFound(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }
}
