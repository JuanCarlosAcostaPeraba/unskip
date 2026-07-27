using Unskip.Core.Messaging.Lan;

namespace Unskip.Core.Tests.Messaging.Lan;

public sealed class CertificateIdentityTests
{
    private const string CanonicalFingerprint =
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f";

    [Theory]
    [InlineData(CanonicalFingerprint)]
    [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]
    [InlineData("00:01:02:03:04:05:06:07:08:09:0A:0B:0C:0D:0E:0F:10:11:12:13:14:15:16:17:18:19:1A:1B:1C:1D:1E:1F")]
    [InlineData("00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F-10-11-12-13-14-15-16-17-18-19-1A-1B-1C-1D-1E-1F")]
    public void SupportedFingerprintFormatsNormalizeCanonically(string candidate)
    {
        var parsed = CertificateFingerprint.TryParse(candidate, out var fingerprint);

        Assert.True(parsed);
        Assert.Equal(CanonicalFingerprint, fingerprint!.Value);
        Assert.Equal(
            "mtls-sha256:" + CanonicalFingerprint,
            AuthenticatedIdentityKey.FromCertificateFingerprint(fingerprint));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0001")]
    [InlineData("gg0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f")]
    [InlineData("00:01-02:03:04:05:06:07:08:09:0A:0B:0C:0D:0E:0F:10:11:12:13:14:15:16:17:18:19:1A:1B:1C:1D:1E:1F")]
    public void InvalidFingerprintIsRejected(string? candidate)
    {
        Assert.False(CertificateFingerprint.TryParse(candidate, out var fingerprint));
        Assert.Null(fingerprint);
    }

    [Fact]
    public void FingerprintEqualityUsesCanonicalBytes()
    {
        var lower = Parse(CanonicalFingerprint);
        var upper = Parse(CanonicalFingerprint.ToUpperInvariant());
        var different = Parse(new string('f', 64));

        Assert.Equal(lower, upper);
        Assert.NotEqual(lower, different);
    }

    [Fact]
    public void AllowListTakesAnImmutableSnapshotAndMatchesExactly()
    {
        var allowed = Parse(CanonicalFingerprint);
        var denied = Parse(new string('f', 64));
        var source = new List<CertificateFingerprint> { allowed };
        var policy = new CertificateSenderAllowList(source);
        source.Clear();
        source.Add(denied!);

        Assert.Equal(
            CertificateAuthorizationResult.Authorized,
            policy.Authorize(allowed));
        Assert.Equal(
            CertificateAuthorizationResult.Unauthorized,
            policy.Authorize(denied));
    }

    [Fact]
    public void MutualTlsIdentityKeyIsNormalizedAndSchemeBound()
    {
        var valid = AuthenticatedIdentityKey.TryNormalize(
            AuthenticationScheme.MutualTls,
            "MTLS-SHA256:" + CanonicalFingerprint.ToUpperInvariant(),
            out var normalized);
        var wrongScheme = AuthenticatedIdentityKey.TryNormalize(
            AuthenticationScheme.WindowsNegotiate,
            "mtls-sha256:" + CanonicalFingerprint,
            out _);

        Assert.True(valid);
        Assert.Equal("mtls-sha256:" + CanonicalFingerprint, normalized);
        Assert.False(wrongScheme);
    }

    private static CertificateFingerprint Parse(string candidate)
    {
        Assert.True(CertificateFingerprint.TryParse(candidate, out var fingerprint));
        return fingerprint!;
    }
}
