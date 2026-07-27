using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Unskip.Infrastructure.Windows.Messaging;

internal sealed class SystemRemoteCertificateValidator : IRemoteCertificateValidator
{
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    public bool Validate(
        X509Certificate2 certificate,
        X509Chain? chain,
        SslPolicyErrors policyErrors,
        RemoteCertificateRole role)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (policyErrors != SslPolicyErrors.None)
        {
            return false;
        }

        var expectedPurpose = role == RemoteCertificateRole.Client
            ? ClientAuthenticationOid
            : ServerAuthenticationOid;
        return certificate.Extensions
            .OfType<X509EnhancedKeyUsageExtension>()
            .SelectMany(extension => extension.EnhancedKeyUsages.Cast<Oid>())
            .Any(oid => string.Equals(oid.Value, expectedPurpose, StringComparison.Ordinal));
    }
}
