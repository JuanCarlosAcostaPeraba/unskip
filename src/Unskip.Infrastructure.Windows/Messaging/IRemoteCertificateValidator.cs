using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Unskip.Infrastructure.Windows.Messaging;

internal interface IRemoteCertificateValidator
{
    bool Validate(
        X509Certificate2 certificate,
        X509Chain? chain,
        SslPolicyErrors policyErrors,
        RemoteCertificateRole role);
}

internal enum RemoteCertificateRole
{
    Client,
    Server,
}
