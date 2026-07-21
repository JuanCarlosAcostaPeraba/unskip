namespace Unskip.Core.Messaging;

public sealed record ValidationError(
    string Field,
    ValidationErrorCode Code,
    string Message);
