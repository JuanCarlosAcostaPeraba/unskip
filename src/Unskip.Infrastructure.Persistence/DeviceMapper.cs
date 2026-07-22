using Unskip.Core.Devices;

namespace Unskip.Infrastructure.Persistence;

internal static class DeviceMapper
{
    public static Device ToDomain(DeviceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new Device(
            entity.Id,
            entity.Alias,
            entity.AliasKey,
            entity.ComputerName,
            entity.Ipv4Address,
            entity.Description,
            entity.IsFavorite,
            entity.PreferredDestination,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastUsedAt);
    }

    public static DeviceEntity ToEntity(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return new DeviceEntity
        {
            Id = device.Id,
            Alias = device.Alias,
            AliasKey = device.AliasKey,
            ComputerName = device.ComputerName,
            Ipv4Address = device.Ipv4Address,
            Description = device.Description,
            IsFavorite = device.IsFavorite,
            PreferredDestination = device.PreferredDestination,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt,
            LastUsedAt = device.LastUsedAt,
        };
    }

    public static void CopyToEntity(Device device, DeviceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(entity);

        entity.Alias = device.Alias;
        entity.AliasKey = device.AliasKey;
        entity.ComputerName = device.ComputerName;
        entity.Ipv4Address = device.Ipv4Address;
        entity.Description = device.Description;
        entity.IsFavorite = device.IsFavorite;
        entity.PreferredDestination = device.PreferredDestination;
        entity.UpdatedAt = device.UpdatedAt;
        entity.LastUsedAt = device.LastUsedAt;
    }
}
