using System.Globalization;
using System.Text;
using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.Core.Devices;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;

namespace Unskip.App.ViewModels;

public sealed class MessageComposerViewModel : ObservableObject
{
    private readonly IMessageSender _sender;
    private readonly SendHistoryService _history;
    private readonly IUrgentAttentionPreviewService _urgentAttentionPreview;
    private DeviceDestinationKind _destinationKind;
    private string? _computerName;
    private string? _ipv4Address;
    private string _destination = string.Empty;
    private string _destinationAlias = string.Empty;
    private string _destinationKindLabel = string.Empty;
    private string _message = string.Empty;
    private string? _messageError;
    private string _resultMessage = UiText.Get("MessageReady");
    private string? _statusLabel;
    private string? _technicalDetails;
    private bool _isSending;
    private bool _isTechnicalDetailsExpanded;
    private bool _canRetry;
    private bool _isPreviewing;
    private string _previewStatus = UiText.Get("PreviewLocalOnly");

    public MessageComposerViewModel(
        IMessageSender sender,
        SendHistoryService history,
        IUrgentAttentionPreviewService urgentAttentionPreview)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _urgentAttentionPreview = urgentAttentionPreview
            ?? throw new ArgumentNullException(nameof(urgentAttentionPreview));
        SendCommand = new AsyncRelayCommand(_ => SendAsync(), _ => CanSend);
        PreviewUrgentOverlayCommand = new AsyncRelayCommand(
            _ => PreviewUrgentOverlayAsync(),
            _ => CanPreviewUrgentOverlay);
        RetryCommand = new AsyncRelayCommand(_ => SendAsync(), _ => CanRetry && !IsSending);
        ToggleTechnicalDetailsCommand = new RelayCommand(
            _ => IsTechnicalDetailsExpanded = !IsTechnicalDetailsExpanded,
            _ => HasTechnicalDetails);
        BackCommand = new RelayCommand(
            _ => BackRequested?.Invoke(this, EventArgs.Empty),
            _ => !IsSending && !IsPreviewing);
    }

    public event EventHandler? BackRequested;

    public event EventHandler? HistoryChanged;

    public AsyncRelayCommand SendCommand { get; }

    public AsyncRelayCommand PreviewUrgentOverlayCommand { get; }

    public AsyncRelayCommand RetryCommand { get; }

    public RelayCommand ToggleTechnicalDetailsCommand { get; }

    public RelayCommand BackCommand { get; }

    public string DestinationAlias
    {
        get => _destinationAlias;
        private set => SetProperty(ref _destinationAlias, value);
    }

    public string Destination
    {
        get => _destination;
        private set => SetProperty(ref _destination, value);
    }

    public string DestinationKindLabel
    {
        get => _destinationKindLabel;
        private set => SetProperty(ref _destinationKindLabel, value);
    }

    public Guid? DeviceId { get; private set; }

    public string Message
    {
        get => _message;
        set
        {
            if (SetProperty(ref _message, value ?? string.Empty))
            {
                MessageError = GetMessageValidationError();
                OnPropertyChanged(nameof(CharacterCountLabel));
                OnPropertyChanged(nameof(IsMessageOverLimit));
                OnPropertyChanged(nameof(CanSend));
                OnPropertyChanged(nameof(CanPreviewUrgentOverlay));
                NotifyCommandStates();
            }
        }
    }

    public string CharacterCountLabel => $"{Message.Length:N0} / {MessagePolicy.MaximumMessageLength:N0}";

    public bool IsMessageOverLimit => Message.Length > MessagePolicy.MaximumMessageLength;

    public string? MessageError
    {
        get => _messageError;
        private set => SetProperty(ref _messageError, value);
    }

    public bool IsSending
    {
        get => _isSending;
        private set
        {
            if (SetProperty(ref _isSending, value))
            {
                OnPropertyChanged(nameof(CanSend));
                NotifyCommandStates();
            }
        }
    }

    public bool CanSend => !IsSending
        && !IsPreviewing
        && !string.IsNullOrWhiteSpace(Destination)
        && !string.IsNullOrWhiteSpace(Message)
        && !IsMessageOverLimit;

    public bool IsPreviewing
    {
        get => _isPreviewing;
        private set
        {
            if (SetProperty(ref _isPreviewing, value))
            {
                OnPropertyChanged(nameof(CanPreviewUrgentOverlay));
                OnPropertyChanged(nameof(CanSend));
                NotifyCommandStates();
            }
        }
    }

    public bool CanPreviewUrgentOverlay => !IsSending
        && !IsPreviewing
        && GetMessageValidationError() is null;

    public string PreviewStatus
    {
        get => _previewStatus;
        private set => SetProperty(ref _previewStatus, value);
    }

    public bool CanRetry
    {
        get => _canRetry;
        private set
        {
            if (SetProperty(ref _canRetry, value))
            {
                RetryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? StatusLabel
    {
        get => _statusLabel;
        private set
        {
            if (SetProperty(ref _statusLabel, value))
            {
                OnPropertyChanged(nameof(HasResult));
            }
        }
    }

    public bool HasResult => StatusLabel is not null;

    public string ResultMessage
    {
        get => _resultMessage;
        private set => SetProperty(ref _resultMessage, value);
    }

    public string? TechnicalDetails
    {
        get => _technicalDetails;
        private set
        {
            if (SetProperty(ref _technicalDetails, value))
            {
                OnPropertyChanged(nameof(HasTechnicalDetails));
                ToggleTechnicalDetailsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasTechnicalDetails => !string.IsNullOrWhiteSpace(TechnicalDetails);

    public bool IsTechnicalDetailsExpanded
    {
        get => _isTechnicalDetailsExpanded;
        private set => SetProperty(ref _isTechnicalDetailsExpanded, value);
    }

    public void Prepare(MessagePreparationRequestedEventArgs destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        DestinationAlias = destination.Alias;
        Destination = destination.Destination;
        DestinationKindLabel = destination.DestinationKind == DeviceDestinationKind.Ipv4
            ? UiText.Get("Ipv4Address")
            : UiText.Get("ComputerName");
        DeviceId = destination.DeviceId;
        _destinationKind = destination.DestinationKind;
        _computerName = destination.ComputerName;
        _ipv4Address = destination.Ipv4Address;
        ClearResult();
        MessageError = null;
        NotifyCommandStates();
    }

    public void ClearPreparation()
    {
        DestinationAlias = string.Empty;
        Destination = string.Empty;
        DestinationKindLabel = string.Empty;
        DeviceId = null;
        _computerName = null;
        _ipv4Address = null;
        ClearResult();
        NotifyCommandStates();
    }

    private async Task SendAsync()
    {
        var request = new MessageRequest(Destination, Message);
        var validation = MessageRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            ApplyValidation(validation);
            await RecordAsync(new MessageSendResult(
                MessageDeliveryStatus.Rejected,
                MessageFailureCategory.Validation,
                null,
                string.Empty,
                string.Empty,
                TimeSpan.Zero,
                validation.Errors[0].Message)).ConfigureAwait(true);
            return;
        }

        IsSending = true;
        CanRetry = false;
        StatusLabel = UiText.Get("DeliverySending");
        ResultMessage = UiText.Format("DeliveryRequesting", Destination);
        TechnicalDetails = null;
        IsTechnicalDetailsExpanded = false;

        try
        {
            var result = await _sender.SendAsync(request).ConfigureAwait(true);
            ApplyResult(result);
            await RecordAsync(result).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusLabel = UiText.Get("DeliveryFailed");
            ResultMessage = UiText.Get("DeliveryUnexpectedFailure");
            TechnicalDetails = $"Unexpected application error: {exception.GetType().Name}";
            CanRetry = true;
            await RecordAsync(new MessageSendResult(
                MessageDeliveryStatus.Failed,
                MessageFailureCategory.ProcessFailure,
                null,
                string.Empty,
                string.Empty,
                TimeSpan.Zero,
                "Unskip could not complete the Windows messaging request.")).ConfigureAwait(true);
        }
        finally
        {
            IsSending = false;
        }
    }

    private async Task PreviewUrgentOverlayAsync()
    {
        var validationError = GetMessageValidationError();
        if (validationError is not null)
        {
            MessageError = validationError;
            PreviewStatus = UiText.Get("PreviewEnterValidMessage");
            return;
        }

        IsPreviewing = true;
        PreviewStatus = UiText.Get("PreviewOpening");
        try
        {
            await _urgentAttentionPreview.ShowAsync(Message).ConfigureAwait(true);
            PreviewStatus = UiText.Get("PreviewClosed");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            PreviewStatus = UiText.Format("PreviewOpenFailed", exception.GetType().Name);
        }
        finally
        {
            IsPreviewing = false;
        }
    }

    private string? GetMessageValidationError()
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            return UiText.Get("MessageRequired");
        }

        if (Message.Length > MessagePolicy.MaximumMessageLength)
        {
            return UiText.Format("MessageTooLong", MessagePolicy.MaximumMessageLength);
        }

        var validation = MessageRequestValidator.Validate(new MessageRequest(Destination, Message));
        return validation.Errors.Any(error => error.Field == "Message")
            ? UiText.Get("MessageInvalidCharacters")
            : null;
    }

    private void ApplyValidation(MessageValidationResult validation)
    {
        var messageError = validation.Errors.FirstOrDefault(error => error.Field == "Message");
        MessageError = messageError is null ? null : GetMessageValidationError();
        StatusLabel = UiText.Get("DeliveryRejected");
        ResultMessage = messageError is not null
            ? GetMessageValidationError() ?? UiText.Get("DeliveryRejectedMessage")
            : UiText.Get("DeliveryRejectedMessage");
        TechnicalDetails = null;
        IsTechnicalDetailsExpanded = false;
        CanRetry = false;
    }

    private void ApplyResult(MessageSendResult result)
    {
        StatusLabel = result.Status switch
        {
            MessageDeliveryStatus.Sending => UiText.Get("DeliverySending"),
            MessageDeliveryStatus.Sent => UiText.Get("DeliverySent"),
            MessageDeliveryStatus.Rejected => UiText.Get("DeliveryRejected"),
            MessageDeliveryStatus.TimedOut => UiText.Get("DeliveryTimedOut"),
            MessageDeliveryStatus.Cancelled => UiText.Get("DeliveryCancelled"),
            MessageDeliveryStatus.Failed => UiText.Get("DeliveryFailed"),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null),
        };
        ResultMessage = result.Status switch
        {
            MessageDeliveryStatus.Sending => UiText.Get("DeliverySendingMessage"),
            MessageDeliveryStatus.Sent => UiText.Get("DeliverySentMessage"),
            MessageDeliveryStatus.Rejected => UiText.Get("DeliveryRejectedMessage"),
            MessageDeliveryStatus.TimedOut => UiText.Get("DeliveryTimedOutMessage"),
            MessageDeliveryStatus.Cancelled => UiText.Get("DeliveryCancelledMessage"),
            MessageDeliveryStatus.Failed => UiText.Get("DeliveryFailedMessage"),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null),
        };
        TechnicalDetails = BuildTechnicalDetails(result);
        IsTechnicalDetailsExpanded = false;
        CanRetry = result.Status is MessageDeliveryStatus.Failed or MessageDeliveryStatus.TimedOut;
    }

    private static string? BuildTechnicalDetails(MessageSendResult result)
    {
        var details = new StringBuilder();
        if (result.ExitCode is int exitCode)
        {
            details.AppendLine(CultureInfo.InvariantCulture, $"Windows exit code: {exitCode}");
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            details.AppendLine(result.StandardError.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            details.AppendLine(result.StandardOutput.Trim());
        }

        return details.Length == 0 ? null : details.ToString().TrimEnd();
    }

    private void ClearResult()
    {
        StatusLabel = null;
        ResultMessage = UiText.Get("MessageReady");
        TechnicalDetails = null;
        IsTechnicalDetailsExpanded = false;
        CanRetry = false;
    }

    private async Task RecordAsync(MessageSendResult result)
    {
        try
        {
            await _history.RecordAsync(new SendHistoryAttempt(
                DeviceId,
                DestinationAlias,
                _computerName,
                _ipv4Address,
                _destinationKind,
                Destination,
                result.Status,
                result.FailureCategory,
                result.Duration,
                result.ExitCode,
                BuildTechnicalDetails(result),
                Message.Length)).ConfigureAwait(true);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ResultMessage += $" {UiText.Get("HistoryRecordFailed")}";
        }
    }

    private void NotifyCommandStates()
    {
        SendCommand.NotifyCanExecuteChanged();
        PreviewUrgentOverlayCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }
}
