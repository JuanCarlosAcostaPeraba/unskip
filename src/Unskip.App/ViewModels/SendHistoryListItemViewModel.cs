using Unskip.App.Localization;
using Unskip.Core.Messaging.History;

namespace Unskip.App.ViewModels;

public sealed class SendHistoryListItemViewModel(SendHistoryRecord record)
{
    public SendHistoryRecord Record { get; } = record;

    public Guid Id => Record.Id;

    public string Alias => Record.AliasSnapshot;

    public string Destination => Record.DestinationSnapshot;

    public string Status => Record.DeliveryStatus switch
    {
        Core.Messaging.MessageDeliveryStatus.Sending => UiText.Get("DeliverySending"),
        Core.Messaging.MessageDeliveryStatus.Sent => UiText.Get("DeliverySent"),
        Core.Messaging.MessageDeliveryStatus.Rejected => UiText.Get("DeliveryRejected"),
        Core.Messaging.MessageDeliveryStatus.TimedOut => UiText.Get("DeliveryTimedOut"),
        Core.Messaging.MessageDeliveryStatus.Cancelled => UiText.Get("DeliveryCancelled"),
        Core.Messaging.MessageDeliveryStatus.Failed => UiText.Get("DeliveryFailed"),
        _ => throw new ArgumentOutOfRangeException(nameof(Record), Record.DeliveryStatus, null),
    };

    public string Timestamp => Record.OccurredAt.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    public string Duration => $"{Record.Duration.TotalMilliseconds:N0} ms";

    public string MessageSummary => UiText.Format("HistoryMessageSummary", Record.MessageLength);
}
