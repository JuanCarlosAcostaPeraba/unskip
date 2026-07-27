using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Unskip.Core.Messaging.Lan;

namespace Unskip.Infrastructure.Windows.Messaging;

public sealed class MutualTlsStreamAuthenticator
{
    private static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(15);
    private readonly CertificateSenderAllowList _allowedRemoteCertificates;
    private readonly SslProtocols _enabledSslProtocols;
    private readonly TimeSpan _handshakeTimeout;
    private readonly IRemoteCertificateValidator _remoteCertificateValidator;
    private readonly X509RevocationMode _revocationMode;

    public MutualTlsStreamAuthenticator(
        CertificateSenderAllowList allowedRemoteCertificates,
        TimeSpan? handshakeTimeout = null)
        : this(
            allowedRemoteCertificates,
            new SystemRemoteCertificateValidator(),
            handshakeTimeout ?? DefaultHandshakeTimeout,
            X509RevocationMode.Online,
            SslProtocols.None)
    {
    }

    internal MutualTlsStreamAuthenticator(
        CertificateSenderAllowList allowedRemoteCertificates,
        IRemoteCertificateValidator remoteCertificateValidator,
        TimeSpan handshakeTimeout,
        X509RevocationMode revocationMode,
        SslProtocols enabledSslProtocols)
    {
        _allowedRemoteCertificates = allowedRemoteCertificates
            ?? throw new ArgumentNullException(nameof(allowedRemoteCertificates));
        _remoteCertificateValidator = remoteCertificateValidator
            ?? throw new ArgumentNullException(nameof(remoteCertificateValidator));
        _revocationMode = revocationMode;
        _enabledSslProtocols = enabledSslProtocols;
        if (handshakeTimeout <= TimeSpan.Zero || handshakeTimeout > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(nameof(handshakeTimeout));
        }

        _handshakeTimeout = handshakeTimeout;
    }

    public async Task<MutualTlsAuthenticationResult> AuthenticateServerAsync(
        Stream connectedStream,
        X509Certificate2 serverCertificate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectedStream);
        ArgumentNullException.ThrowIfNull(serverCertificate);

        var sslStream = CreateSslStream(connectedStream, RemoteCertificateRole.Client);
        var options = new SslServerAuthenticationOptions
        {
            ServerCertificate = serverCertificate,
            ClientCertificateRequired = true,
            CertificateRevocationCheckMode = _revocationMode,
            EncryptionPolicy = EncryptionPolicy.RequireEncryption,
            EnabledSslProtocols = _enabledSslProtocols,
        };

        return await AuthenticateAsync(
            sslStream,
            token => sslStream.AuthenticateAsServerAsync(options, token),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MutualTlsAuthenticationResult> AuthenticateClientAsync(
        Stream connectedStream,
        string targetHost,
        X509Certificate2? clientCertificate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectedStream);
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            throw new ArgumentException("Enter the expected receiver DNS name.", nameof(targetHost));
        }

        var certificates = new X509CertificateCollection();
        if (clientCertificate is not null)
        {
            certificates.Add(clientCertificate);
        }

        var sslStream = CreateSslStream(connectedStream, RemoteCertificateRole.Server);
        var options = new SslClientAuthenticationOptions
        {
            TargetHost = targetHost,
            ClientCertificates = certificates,
            LocalCertificateSelectionCallback =
                (_, _, _, _, _) => clientCertificate,
            CertificateRevocationCheckMode = _revocationMode,
            EncryptionPolicy = EncryptionPolicy.RequireEncryption,
            EnabledSslProtocols = _enabledSslProtocols,
        };

        return await AuthenticateAsync(
            sslStream,
            token => sslStream.AuthenticateAsClientAsync(options, token),
            cancellationToken).ConfigureAwait(false);
    }

    private SslStream CreateSslStream(Stream connectedStream, RemoteCertificateRole role)
    {
        return new SslStream(
            connectedStream,
            leaveInnerStreamOpen: false,
            (_, certificate, chain, errors) =>
            {
                if (certificate is null)
                {
                    return false;
                }

                using var certificate2 = X509CertificateLoader.LoadCertificate(
                    certificate.Export(X509ContentType.Cert));
                return _remoteCertificateValidator.Validate(certificate2, chain, errors, role);
            });
    }

    private async Task<MutualTlsAuthenticationResult> AuthenticateAsync(
        SslStream sslStream,
        Func<CancellationToken, Task> authenticate,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_handshakeTimeout);

        try
        {
            await authenticate(timeout.Token).ConfigureAwait(false);
            var session = CreateSession(sslStream);
            var validation = AuthenticatedSessionValidator.Validate(session);
            if (!validation.IsValid)
            {
                throw new MutualTlsAuthenticationException(
                    $"The mutual-TLS session protection is invalid: {validation.Error}.");
            }

            return new MutualTlsAuthenticationResult(sslStream, session);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sslStream.Dispose();
            throw new TimeoutException("The mutual-TLS handshake timed out.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            sslStream.Dispose();
            throw exception is MutualTlsAuthenticationException
                ? exception
                : new MutualTlsAuthenticationException(
                    "The mutual-TLS handshake was rejected.",
                    exception);
        }
    }

    private AuthenticatedSessionContext CreateSession(SslStream sslStream)
    {
        if (!sslStream.IsAuthenticated
            || !sslStream.IsMutuallyAuthenticated
            || !sslStream.IsEncrypted
            || !sslStream.IsSigned
            || sslStream.RemoteCertificate is null)
        {
            throw new MutualTlsAuthenticationException(
                "The remote endpoint did not establish the required protected session.");
        }

        using var remoteCertificate = X509CertificateLoader.LoadCertificate(
            sslStream.RemoteCertificate.Export(X509ContentType.Cert));
        var fingerprint = CertificateFingerprint.FromSha256Bytes(
            SHA256.HashData(remoteCertificate.RawData));
        if (_allowedRemoteCertificates.Authorize(fingerprint)
            != CertificateAuthorizationResult.Authorized)
        {
            throw new MutualTlsAuthenticationException(
                "The remote certificate is not authorized.");
        }

        var displayName = remoteCertificate.GetNameInfo(X509NameType.SimpleName, false);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = remoteCertificate.Subject;
        }

        return new(
            sslStream.IsAuthenticated,
            sslStream.IsMutuallyAuthenticated,
            sslStream.IsEncrypted,
            sslStream.IsSigned,
            AuthenticationScheme.MutualTls,
            AuthenticatedIdentityKey.FromCertificateFingerprint(fingerprint),
            displayName);
    }
}
