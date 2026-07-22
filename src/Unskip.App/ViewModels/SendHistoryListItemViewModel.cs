using Unskip.Core.Messaging.History;

namespace Unskip.App.ViewModels;

public sealed class SendHistoryListItemViewModel(SendHistoryRecord record)
{
    public SendHistoryRecord Record { get; } = record;

    public Guid Id => Record.Id;

    public string Alias => Record.AliasSnapshot;

    public string Destination => Record.DestinationSnapshot;

    public string Status => Record.DeliveryStatus == Core.Messaging.MessageDeliveryStatus.TimedOut
        ? "Timed out"
        : Record.DeliveryStatus.ToString();

    public string Timestamp => Record.OccurredAt.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    public string Duration => $"{Record.Duration.TotalMilliseconds:N0} ms";

    public string MessageSummary => $"Message body not stored · {Record.MessageLength:N0} characters";
}
