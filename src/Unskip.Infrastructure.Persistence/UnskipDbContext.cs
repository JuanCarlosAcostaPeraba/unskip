using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Unskip.Core.Devices;
using Unskip.Core.Messaging.History;
using Unskip.Core.Networking;

namespace Unskip.Infrastructure.Persistence;

public sealed class UnskipDbContext(DbContextOptions<UnskipDbContext> options) : DbContext(options)
{
    internal DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

    internal DbSet<SendHistoryEntity> SendHistoryRecords => Set<SendHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var timestampConverter = new ValueConverter<DateTimeOffset, long>(
            timestamp => timestamp.UtcDateTime.Ticks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        var nullableTimestampConverter = new ValueConverter<DateTimeOffset?, long?>(
            timestamp => timestamp.HasValue ? timestamp.Value.UtcDateTime.Ticks : null,
            ticks => ticks.HasValue
                ? new DateTimeOffset(ticks.Value, TimeSpan.Zero)
                : null);

        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable(
                "Devices",
                table => table.HasCheckConstraint(
                    "CK_Devices_Destination",
                    "\"ComputerName\" IS NOT NULL OR \"Ipv4Address\" IS NOT NULL"));
            entity.HasKey(device => device.Id);
            entity.Property(device => device.Alias).HasMaxLength(DevicePolicy.MaximumAliasLength).IsRequired();
            entity.Property(device => device.AliasKey).HasMaxLength(DevicePolicy.MaximumAliasLength).IsRequired();
            entity.Property(device => device.ComputerName).HasMaxLength(NetworkAddressValidator.MaximumHostnameLength);
            entity.Property(device => device.Ipv4Address).HasMaxLength(15);
            entity.Property(device => device.Description).HasMaxLength(DevicePolicy.MaximumDescriptionLength);
            entity.Property(device => device.CreatedAt).HasConversion(timestampConverter);
            entity.Property(device => device.UpdatedAt).HasConversion(timestampConverter);
            entity.Property(device => device.LastUsedAt).HasConversion(nullableTimestampConverter);
            entity.HasIndex(device => device.AliasKey).IsUnique();
            entity.HasIndex(device => device.ComputerName)
                .IsUnique()
                .HasFilter("\"ComputerName\" IS NOT NULL");
            entity.HasIndex(device => device.Ipv4Address)
                .IsUnique()
                .HasFilter("\"Ipv4Address\" IS NOT NULL");
        });

        modelBuilder.Entity<SendHistoryEntity>(entity =>
        {
            entity.ToTable("SendHistoryRecords");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.AliasSnapshot)
                .HasMaxLength(DevicePolicy.MaximumAliasLength)
                .IsRequired();
            entity.Property(record => record.DestinationSnapshot)
                .HasMaxLength(NetworkAddressValidator.MaximumHostnameLength)
                .IsRequired();
            entity.Property(record => record.ComputerNameSnapshot)
                .HasMaxLength(NetworkAddressValidator.MaximumHostnameLength);
            entity.Property(record => record.Ipv4AddressSnapshot).HasMaxLength(15);
            entity.Property(record => record.DiagnosticSummary)
                .HasMaxLength(SendHistoryPolicy.MaximumDiagnosticSummaryLength);
            entity.Property(record => record.OccurredAt).HasConversion(timestampConverter);
            entity.HasOne(record => record.Device)
                .WithMany(device => device.SendHistoryRecords)
                .HasForeignKey(record => record.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(record => record.DeviceId);
            entity.HasIndex(record => record.OccurredAt);
        });
    }
}
