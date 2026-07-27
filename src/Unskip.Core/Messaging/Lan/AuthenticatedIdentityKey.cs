namespace Unskip.Core.Messaging.Lan;

public static class AuthenticatedIdentityKey
{
    private const string MutualTlsPrefix = "mtls-sha256:";
    private const string WindowsSidPrefix = "windows-sid:";
    private const int MaximumWindowsSidLength = 184;

    public static bool TryNormalize(
        AuthenticationScheme scheme,
        string? candidate,
        out string? identityKey)
    {
        identityKey = scheme switch
        {
            AuthenticationScheme.MutualTls => NormalizeMutualTls(candidate),
            AuthenticationScheme.WindowsNegotiate => NormalizeWindowsSid(candidate),
            _ => null,
        };
        return identityKey is not null;
    }

    public static string FromCertificateFingerprint(CertificateFingerprint fingerprint)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        return MutualTlsPrefix + fingerprint.Value;
    }

    private static string? NormalizeMutualTls(string? candidate)
    {
        if (candidate is null
            || !candidate.StartsWith(MutualTlsPrefix, StringComparison.OrdinalIgnoreCase)
            || !CertificateFingerprint.TryParse(
                candidate[MutualTlsPrefix.Length..],
                out var fingerprint))
        {
            return null;
        }

        return FromCertificateFingerprint(fingerprint!);
    }

    private static string? NormalizeWindowsSid(string? candidate)
    {
        if (candidate is null
            || !candidate.StartsWith(WindowsSidPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var sid = candidate[WindowsSidPrefix.Length..];
        if (sid.Length is < 5 or > MaximumWindowsSidLength
            || !sid.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase)
            || sid[^1] == '-'
            || sid[2..].Any(character => !char.IsAsciiDigit(character) && character != '-')
            || sid.Contains("--", StringComparison.Ordinal))
        {
            return null;
        }

        return WindowsSidPrefix + sid.ToUpperInvariant();
    }
}
