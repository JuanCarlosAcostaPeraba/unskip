namespace Unskip.Core.Messaging;

public sealed class MessageValidationResult
{
    private MessageValidationResult(
        ValidatedMessageRequest? request,
        IReadOnlyList<ValidationError> errors)
    {
        Request = request;
        Errors = errors;
    }

    public bool IsValid => Request is not null;

    public ValidatedMessageRequest? Request { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public static MessageValidationResult Success(ValidatedMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new MessageValidationResult(request, Array.Empty<ValidationError>());
    }

    public static MessageValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorSnapshot = errors.ToArray();
        if (errorSnapshot.Length == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        return new MessageValidationResult(null, Array.AsReadOnly(errorSnapshot));
    }
}
