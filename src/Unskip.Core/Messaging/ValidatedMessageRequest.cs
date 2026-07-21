namespace Unskip.Core.Messaging;

public sealed record ValidatedMessageRequest(MessageTarget Target, string Message);
