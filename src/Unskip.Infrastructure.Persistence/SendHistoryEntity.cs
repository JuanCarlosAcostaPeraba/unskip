using Unskip.Core.Devices;
using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Persistence;

internal sealed class SendHistoryEntity
{
    public Guid Id { get; set; }

    public Guid? DeviceId { get; set; }

    public DeviceEntity? Device { get; set; }

    public required string AliasSnapshot { get; set; }

    public string? ComputerNameSnapshot { get; set; }

    public string? Ipv4AddressSnapshot { get; set; }

    public DeviceDestinationKind DestinationKind { get; set; }

    public required string DestinationSnapshot { get; set; }

    public MessageDeliveryStatus DeliveryStatus { get; set; }

    public MessageFailureCategory FailureCategory { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public long DurationTicks { get; set; }

    public int? ExitCode { get; set; }

    public string? DiagnosticSummary { get; set; }

    public int MessageLength { get; set; }
}
