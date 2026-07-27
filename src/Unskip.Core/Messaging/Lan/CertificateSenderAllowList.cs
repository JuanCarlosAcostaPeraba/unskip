namespace Unskip.Core.Messaging.Lan;

public sealed class CertificateSenderAllowList
{
    private readonly HashSet<CertificateFingerprint> _allowedFingerprints;

    public CertificateSenderAllowList(IEnumerable<CertificateFingerprint> allowedFingerprints)
    {
        ArgumentNullException.ThrowIfNull(allowedFingerprints);
        _allowedFingerprints = new HashSet<CertificateFingerprint>(allowedFingerprints);
    }

    public CertificateAuthorizationResult Authorize(CertificateFingerprint fingerprint)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        return _allowedFingerprints.Contains(fingerprint)
            ? CertificateAuthorizationResult.Authorized
            : CertificateAuthorizationResult.Unauthorized;
    }
}

public enum CertificateAuthorizationResult
{
    Authorized,
    Unauthorized,
}
