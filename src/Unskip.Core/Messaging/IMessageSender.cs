namespace Unskip.Core.Messaging;

public interface IMessageSender
{
    Task<MessageSendResult> SendAsync(
        MessageRequest request,
        CancellationToken cancellationToken = default);
}
