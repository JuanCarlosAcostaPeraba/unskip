using Unskip.Core.Messaging;

namespace Unskip.App.ViewModels;

public sealed record HistoryFilterOption(string Label, MessageDeliveryStatus? Status);
