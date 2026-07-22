namespace Unskip.Core.Devices;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeviceStoreWriteStatus> AddAsync(
        Device device,
        CancellationToken cancellationToken = default);

    Task<DeviceStoreWriteStatus> UpdateAsync(
        Device device,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
