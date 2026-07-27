namespace Unskip.Core.Messaging.Lan;

public sealed class LanReceiverAdmissionService(
    LanMessageRequestValidator requestValidator,
    IdentityRateLimiter rateLimiter,
    ReplayProtectionService replayProtection)
{
    private readonly LanMessageRequestValidator _requestValidator =
        requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
    private readonly IdentityRateLimiter _rateLimiter =
        rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    private readonly ReplayProtectionService _replayProtection =
        replayProtection ?? throw new ArgumentNullException(nameof(replayProtection));

    public LanReceiverAdmission Evaluate(
        AuthenticatedSessionContext session,
        LanMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);

        var sessionValidation = AuthenticatedSessionValidator.Validate(session);
        if (!sessionValidation.IsValid)
        {
            return LanReceiverAdmission.Rejected(
                request.MessageId,
                LanReceiverResponseCode.AuthenticationRequired);
        }

        var requestValidation = _requestValidator.Validate(request);
        if (!requestValidation.IsValid)
        {
            var code = requestValidation.Error switch
            {
                LanRequestValidationError.UnsupportedVersion =>
                    LanReceiverResponseCode.UnsupportedVersion,
                LanRequestValidationError.Expired => LanReceiverResponseCode.Expired,
                _ => LanReceiverResponseCode.InvalidRequest,
            };
            return LanReceiverAdmission.Rejected(request.MessageId, code);
        }

        var sender = sessionValidation.Sender!;
        var rateResult = _rateLimiter.TryAcquire(sender);
        if (rateResult != IdentityRateLimitResult.Accepted)
        {
            var code = rateResult == IdentityRateLimitResult.RateLimited
                ? LanReceiverResponseCode.RateLimitExceeded
                : LanReceiverResponseCode.RateLimitCapacityExceeded;
            return LanReceiverAdmission.Rejected(request.MessageId, code);
        }

        var replayResult = _replayProtection.TryAccept(sender, request);
        if (replayResult != ReplayProtectionResult.Accepted)
        {
            var code = replayResult == ReplayProtectionResult.ReplayDetected
                ? LanReceiverResponseCode.ReplayDetected
                : LanReceiverResponseCode.ReplayCapacityExceeded;
            return LanReceiverAdmission.Rejected(request.MessageId, code);
        }

        return LanReceiverAdmission.Accepted(sender, request.MessageId);
    }
}

public sealed record LanReceiverAdmission(
    bool IsAcceptedForLocalHandling,
    AuthenticatedSender? Sender,
    LanReceiverResponse Response)
{
    public static LanReceiverAdmission Accepted(AuthenticatedSender sender, Guid messageId)
    {
        return new(true, sender, LanReceiverResponse.Accepted(messageId));
    }

    public static LanReceiverAdmission Rejected(Guid messageId, LanReceiverResponseCode code)
    {
        return new(false, null, LanReceiverResponse.Rejected(messageId, code));
    }
}
