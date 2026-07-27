using System.Globalization;
using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;

namespace Unskip.App.Tests;

public sealed class MessageComposerViewModelTests
{
    [Fact]
    public void PreparedDestinationShowsAliasAndTechnicalTarget()
    {
        var composer = CreateComposer(new QueueMessageSender());

        composer.Prepare(Destination());

        Assert.Equal("Reception", composer.DestinationAlias);
        Assert.Equal("front-desk", composer.Destination);
        Assert.Equal("Computer name", composer.DestinationKindLabel);
    }

    [Fact]
    public void EmptyAndOversizedMessagesCannotBeSubmitted()
    {
        var composer = CreateComposer(new QueueMessageSender());
        composer.Prepare(Destination());

        Assert.False(composer.SendCommand.CanExecute(null));

        composer.Message = new string('x', MessagePolicy.MaximumMessageLength + 1);

        Assert.True(composer.IsMessageOverLimit);
        Assert.False(composer.SendCommand.CanExecute(null));
        Assert.Equal(
            $"{MessagePolicy.MaximumMessageLength + 1:N0} / {MessagePolicy.MaximumMessageLength:N0}",
            composer.CharacterCountLabel);
    }

    [Fact]
    public async Task DuplicateSubmissionIsIgnoredWhileSendIsPending()
    {
        var sender = new PendingMessageSender();
        var composer = CreateComposer(sender);
        composer.Prepare(Destination());
        composer.Message = "Fictitious test message";

        var firstSend = composer.SendCommand.ExecuteAsync();
        await sender.Started.Task;
        var duplicateSend = composer.SendCommand.ExecuteAsync();

        Assert.True(composer.IsSending);
        Assert.False(composer.SendCommand.CanExecute(null));
        Assert.Single(sender.Requests);

        sender.Complete(Sent());
        await Task.WhenAll(firstSend, duplicateSend);
        Assert.Single(sender.Requests);
    }

    [Fact]
    public async Task SentStatusNeverClaimsReadAcknowledgement()
    {
        var sender = new QueueMessageSender(Sent());
        var composer = CreateComposer(sender);
        composer.Prepare(Destination());
        composer.Message = "Fictitious test message";

        await composer.SendCommand.ExecuteAsync();

        Assert.Equal("Sent", composer.StatusLabel);
        Assert.Contains("does not prove", composer.ResultMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("was read", composer.ResultMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(composer.CanRetry);
    }

    [Theory]
    [InlineData(MessageDeliveryStatus.Failed, "Failed")]
    [InlineData(MessageDeliveryStatus.TimedOut, "Timed out")]
    public async Task FailedOrTimedOutSendKeepsDraftAndCanRetry(
        MessageDeliveryStatus initialStatus,
        string expectedLabel)
    {
        var sender = new QueueMessageSender(
            Result(initialStatus, "The request did not complete."),
            Sent());
        var composer = CreateComposer(sender);
        composer.Prepare(Destination());
        composer.Message = "Keep this fictitious draft";

        await composer.SendCommand.ExecuteAsync();

        Assert.Equal(expectedLabel, composer.StatusLabel);
        Assert.Equal("Keep this fictitious draft", composer.Message);
        Assert.True(composer.CanRetry);

        await composer.RetryCommand.ExecuteAsync();

        Assert.Equal("Sent", composer.StatusLabel);
        Assert.Equal(2, sender.Requests.Count);
    }

    [Fact]
    public async Task TechnicalDetailsAreOptionalAndExpandable()
    {
        var sender = new QueueMessageSender(new MessageSendResult(
            MessageDeliveryStatus.Rejected,
            MessageFailureCategory.NativeRejected,
            5,
            string.Empty,
            "Access denied",
            TimeSpan.FromMilliseconds(10),
            "Windows rejected the request."));
        var composer = CreateComposer(sender);
        composer.Prepare(Destination());
        composer.Message = "Fictitious test message";

        await composer.SendCommand.ExecuteAsync();
        composer.ToggleTechnicalDetailsCommand.Execute(null);

        Assert.True(composer.HasTechnicalDetails);
        Assert.True(composer.IsTechnicalDetailsExpanded);
        Assert.Contains("exit code: 5", composer.TechnicalDetails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Access denied", composer.TechnicalDetails, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CanonicalIpv4DestinationIsForwardedToSender()
    {
        var sender = new QueueMessageSender(Sent());
        var composer = CreateComposer(sender);
        composer.Prepare(new MessagePreparationRequestedEventArgs(
            "Manual destination",
            "192.0.2.7",
            DeviceDestinationKind.Ipv4,
            null));
        composer.Message = "Fictitious test message";

        await composer.SendCommand.ExecuteAsync();

        Assert.Equal("Sent", composer.StatusLabel);
        var request = Assert.Single(sender.Requests);
        Assert.Equal("192.0.2.7", request.Target);
    }

    [Fact]
    public async Task UnexpectedExceptionDoesNotExposeItsPotentiallySensitiveMessage()
    {
        var composer = CreateComposer(new ThrowingMessageSender());
        composer.Prepare(Destination());
        composer.Message = "Fictitious private draft";

        await composer.SendCommand.ExecuteAsync();

        Assert.Equal("Failed", composer.StatusLabel);
        Assert.True(composer.CanRetry);
        Assert.Contains(nameof(InvalidOperationException), composer.TechnicalDetails, StringComparison.Ordinal);
        Assert.DoesNotContain("Fictitious private draft", composer.TechnicalDetails, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocalPreviewDoesNotSendOrPersistMessageData()
    {
        var sender = new QueueMessageSender();
        var preview = new RecordingUrgentAttentionPreviewService();
        var clock = new ViewModelTestContext.MutableClock(
            new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero));
        var historyRepository = new ViewModelTestContext.InMemorySendHistoryRepository();
        var composer = new MessageComposerViewModel(
            sender,
            new SendHistoryService(historyRepository, clock),
            preview);
        composer.Prepare(Destination());
        composer.Message = "A draft that must stay local";

        await composer.PreviewUrgentOverlayCommand.ExecuteAsync();

        Assert.Equal(1, preview.ShowCount);
        Assert.Equal("A draft that must stay local", preview.Message);
        Assert.Empty(sender.Requests);
        Assert.Empty(historyRepository.Records);
        Assert.Contains("Nothing was sent", composer.PreviewStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewFailureDoesNotExposeSensitiveExceptionMessage()
    {
        var preview = new RecordingUrgentAttentionPreviewService
        {
            Exception = new InvalidOperationException("Sensitive local detail"),
        };
        var composer = CreateComposer(new QueueMessageSender(), preview);
        composer.Message = "Fictitious local draft";

        await composer.PreviewUrgentOverlayCommand.ExecuteAsync();

        Assert.Contains(nameof(InvalidOperationException), composer.PreviewStatus, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive local detail", composer.PreviewStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivePreviewDisablesSendingAndNavigationUntilItCloses()
    {
        var preview = new PendingUrgentAttentionPreviewService();
        var composer = CreateComposer(new QueueMessageSender(Sent()), preview);
        composer.Prepare(Destination());
        composer.Message = "Fictitious local draft";

        var previewTask = composer.PreviewUrgentOverlayCommand.ExecuteAsync();
        await preview.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(composer.IsPreviewing);
        Assert.False(composer.CanSend);
        Assert.False(composer.SendCommand.CanExecute(null));
        Assert.False(composer.BackCommand.CanExecute(null));

        preview.Complete();
        await previewTask;

        Assert.False(composer.IsPreviewing);
        Assert.True(composer.CanSend);
        Assert.True(composer.BackCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyDraftCannotBePreviewed(string message)
    {
        var composer = CreateComposer(new QueueMessageSender());
        composer.Prepare(Destination());
        composer.Message = message;

        Assert.False(composer.CanPreviewUrgentOverlay);
        Assert.False(composer.PreviewUrgentOverlayCommand.CanExecute(null));
        if (message.Length > 0)
        {
            Assert.Equal("Enter a message.", composer.MessageError);
        }
    }

    [Fact]
    public void OversizedDraftCannotBePreviewed()
    {
        var composer = CreateComposer(new QueueMessageSender());
        composer.Prepare(Destination());
        composer.Message = new string('x', MessagePolicy.MaximumMessageLength + 1);

        Assert.False(composer.CanPreviewUrgentOverlay);
        Assert.False(composer.PreviewUrgentOverlayCommand.CanExecute(null));
        Assert.Contains(
            MessagePolicy.MaximumMessageLength.ToString(CultureInfo.InvariantCulture),
            composer.MessageError,
            StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedControlCharacterCannotBePreviewed()
    {
        var composer = CreateComposer(new QueueMessageSender());
        composer.Prepare(Destination());
        composer.Message = "Visible\u0001hidden";

        Assert.False(composer.CanPreviewUrgentOverlay);
        Assert.False(composer.PreviewUrgentOverlayCommand.CanExecute(null));
        Assert.Contains("unsupported control character", composer.MessageError, StringComparison.OrdinalIgnoreCase);
    }

    private static MessagePreparationRequestedEventArgs Destination()
    {
        return new MessagePreparationRequestedEventArgs(
            "Reception",
            "front-desk",
            DeviceDestinationKind.Hostname,
            Guid.NewGuid());
    }

    private static MessageComposerViewModel CreateComposer(
        IMessageSender sender,
        IUrgentAttentionPreviewService? preview = null)
    {
        var clock = new ViewModelTestContext.MutableClock(
            new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero));
        var history = new SendHistoryService(
            new ViewModelTestContext.InMemorySendHistoryRepository(),
            clock);
        return new MessageComposerViewModel(
            sender,
            history,
            preview ?? new RecordingUrgentAttentionPreviewService());
    }

    private static MessageSendResult Sent()
    {
        return new MessageSendResult(
            MessageDeliveryStatus.Sent,
            MessageFailureCategory.None,
            0,
            string.Empty,
            string.Empty,
            TimeSpan.FromMilliseconds(10),
            "Windows accepted the message request. This does not confirm that a person read it.");
    }

    private static MessageSendResult Result(MessageDeliveryStatus status, string message)
    {
        return new MessageSendResult(
            status,
            status == MessageDeliveryStatus.TimedOut
                ? MessageFailureCategory.Timeout
                : MessageFailureCategory.ProcessFailure,
            null,
            string.Empty,
            string.Empty,
            TimeSpan.FromMilliseconds(10),
            message);
    }

    private sealed class QueueMessageSender(params MessageSendResult[] results) : IMessageSender
    {
        private readonly Queue<MessageSendResult> _results = new(results);

        public List<MessageRequest> Requests { get; } = [];

        public Task<MessageSendResult> SendAsync(
            MessageRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_results.Dequeue());
        }
    }

    private sealed class PendingMessageSender : IMessageSender
    {
        private readonly TaskCompletionSource<MessageSendResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<MessageRequest> Requests { get; } = [];

        public Task<MessageSendResult> SendAsync(
            MessageRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            Started.TrySetResult();
            return _completion.Task;
        }

        public void Complete(MessageSendResult result)
        {
            _completion.SetResult(result);
        }
    }

    private sealed class ThrowingMessageSender : IMessageSender
    {
        public Task<MessageSendResult> SendAsync(
            MessageRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException($"Failure while sending {request.Message}");
        }
    }

    private sealed class RecordingUrgentAttentionPreviewService : IUrgentAttentionPreviewService
    {
        public int ShowCount { get; private set; }

        public string? Message { get; private set; }

        public Exception? Exception { get; init; }

        public Task ShowAsync(string message, CancellationToken cancellationToken = default)
        {
            ShowCount++;
            Message = message;
            return Exception is null
                ? Task.CompletedTask
                : Task.FromException(Exception);
        }
    }

    private sealed class PendingUrgentAttentionPreviewService : IUrgentAttentionPreviewService
    {
        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ShowAsync(string message, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _completion.Task;
        }

        public void Complete()
        {
            _completion.SetResult();
        }
    }
}
