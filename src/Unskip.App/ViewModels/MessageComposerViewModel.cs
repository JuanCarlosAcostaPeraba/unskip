using System.Globalization;
using System.Text;
using Unskip.App.Commands;
using Unskip.Core.Devices;
using Unskip.Core.Messaging;

namespace Unskip.App.ViewModels;

public sealed class MessageComposerViewModel : ObservableObject
{
    private readonly IMessageSender _sender;
    private string _destination = string.Empty;
    private string _destinationAlias = string.Empty;
    private string _destinationKindLabel = string.Empty;
    private string _message = string.Empty;
    private string? _messageError;
    private string _resultMessage = "Write a message when you are ready.";
    private string? _statusLabel;
    private string? _technicalDetails;
    private bool _isSending;
    private bool _isTechnicalDetailsExpanded;
    private bool _canRetry;

    public MessageComposerViewModel(IMessageSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        SendCommand = new AsyncRelayCommand(_ => SendAsync(), _ => CanSend);
        RetryCommand = new AsyncRelayCommand(_ => SendAsync(), _ => CanRetry && !IsSending);
        ToggleTechnicalDetailsCommand = new RelayCommand(
            _ => IsTechnicalDetailsExpanded = !IsTechnicalDetailsExpanded,
            _ => HasTechnicalDetails);
        BackCommand = new RelayCommand(_ => BackRequested?.Invoke(this, EventArgs.Empty), _ => !IsSending);
    }

    public event EventHandler? BackRequested;

    public AsyncRelayCommand SendCommand { get; }

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
                MessageError = IsMessageOverLimit
                    ? $"Messages can contain at most {MessagePolicy.MaximumMessageLength:N0} characters."
                    : null;
                OnPropertyChanged(nameof(CharacterCountLabel));
                OnPropertyChanged(nameof(IsMessageOverLimit));
                OnPropertyChanged(nameof(CanSend));
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
        && !string.IsNullOrWhiteSpace(Destination)
        && !string.IsNullOrWhiteSpace(Message)
        && !IsMessageOverLimit;

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
            ? "IPv4 address"
            : "Computer name";
        DeviceId = destination.DeviceId;
        ClearResult();
        MessageError = null;
        NotifyCommandStates();
    }

    private async Task SendAsync()
    {
        var request = new MessageRequest(Destination, Message);
        var validation = MessageRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            ApplyValidation(validation);
            return;
        }

        IsSending = true;
        CanRetry = false;
        StatusLabel = "Sending";
        ResultMessage = $"Asking Windows to send the message to {Destination}.";
        TechnicalDetails = null;
        IsTechnicalDetailsExpanded = false;

        try
        {
            var result = await _sender.SendAsync(request).ConfigureAwait(true);
            ApplyResult(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusLabel = "Failed";
            ResultMessage = "Unskip could not complete the Windows messaging request. You can retry without re-entering the message.";
            TechnicalDetails = $"Unexpected application error: {exception.GetType().Name}";
            CanRetry = true;
        }
        finally
        {
            IsSending = false;
        }
    }

    private void ApplyValidation(MessageValidationResult validation)
    {
        var messageError = validation.Errors.FirstOrDefault(error => error.Field == "Message");
        MessageError = messageError?.Message;
        StatusLabel = "Rejected";
        ResultMessage = validation.Errors[0].Message;
        TechnicalDetails = null;
        IsTechnicalDetailsExpanded = false;
        CanRetry = false;
    }

    private void ApplyResult(MessageSendResult result)
    {
        StatusLabel = result.Status switch
        {
            MessageDeliveryStatus.Sending => "Sending",
            MessageDeliveryStatus.Sent => "Sent",
            MessageDeliveryStatus.Rejected => "Rejected",
            MessageDeliveryStatus.TimedOut => "Timed out",
            MessageDeliveryStatus.Cancelled => "Cancelled",
            MessageDeliveryStatus.Failed => "Failed",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null),
        };
        ResultMessage = result.UserMessage;
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
        ResultMessage = "Write a message when you are ready.";
        TechnicalDetails = null;
        IsTechnicalDetailsExpanded = false;
        CanRetry = false;
    }

    private void NotifyCommandStates()
    {
        SendCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }
}
