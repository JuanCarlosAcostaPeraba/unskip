using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;

namespace Unskip.App.Tests;

public sealed class SendHistoryViewModelTests
{
    [Fact]
    public async Task CompletedSendStoresSnapshotsButNotMessageBody()
    {
        var context = ViewModelTestContext.Create();
        context.Directory.ManualDestination = "front-desk";
        context.Directory.UseManualDestinationCommand.Execute(null);
        context.Directory.PrepareMessageCommand.Execute(null);
        context.Main.Composer.Message = "Fictitious private message";

        await context.Main.Composer.SendCommand.ExecuteAsync();

        var record = Assert.Single(context.HistoryRepository.Records);
        Assert.Equal("front-desk", record.ComputerNameSnapshot);
        Assert.Equal("front-desk", record.DestinationSnapshot);
        Assert.Equal(26, record.MessageLength);
        Assert.DoesNotContain("Fictitious private message", record.DiagnosticSummary ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusFilterAndSearchNarrowHistory()
    {
        var context = ViewModelTestContext.Create();
        context.HistoryRepository.Records.AddRange([
            Record("Reception", MessageDeliveryStatus.Sent),
            Record("Workshop", MessageDeliveryStatus.Failed),
        ]);
        await context.Main.History.ReloadAsync();

        context.Main.History.SelectedFilter = context.Main.History.Filters.Single(filter => filter.Status == MessageDeliveryStatus.Failed);
        context.Main.History.SearchText = "work";

        Assert.Equal("Workshop", Assert.Single(context.Main.History.FilteredEntries).Alias);
    }

    [Fact]
    public async Task RetryRestoresOnlyDestinationAndClearRequiresConfirmation()
    {
        var context = ViewModelTestContext.Create();
        context.HistoryRepository.Records.Add(Record("Reception", MessageDeliveryStatus.Failed));
        await context.Main.History.ReloadAsync();
        context.Main.History.SelectedEntry = Assert.Single(context.Main.History.FilteredEntries);
        context.Main.Composer.Message = "Stale draft";

        context.Main.History.RetryCommand.Execute(null);

        Assert.True(context.Main.IsComposerVisible);
        Assert.Equal("front-desk", context.Main.Composer.Destination);
        Assert.Empty(context.Main.Composer.Message);

        context.HistoryConfirmation.Response = false;
        await context.Main.History.ClearCommand.ExecuteAsync();
        Assert.Single(context.HistoryRepository.Records);
        context.HistoryConfirmation.Response = true;
        await context.Main.History.ClearCommand.ExecuteAsync();
        Assert.Empty(context.HistoryRepository.Records);
    }

    private static SendHistoryRecord Record(string alias, MessageDeliveryStatus status) => new(
        Guid.NewGuid(), null, alias, "front-desk", null, DeviceDestinationKind.Hostname,
        "front-desk", status, MessageFailureCategory.ProcessFailure,
        new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero),
        TimeSpan.FromMilliseconds(15), 5, "Sanitized", 18);
}
