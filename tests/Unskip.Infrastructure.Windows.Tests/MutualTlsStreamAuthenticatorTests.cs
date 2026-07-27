using System.IO.Pipes;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Unskip.Core.Messaging.Lan;
using Unskip.Infrastructure.Windows.Messaging;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class MutualTlsStreamAuthenticatorTests
{
    private const string ExpectedHost = "receiver.example.test";

    [Fact]
    public async Task EphemeralMutualTlsHandshakeTransfersEncryptedData()
    {
        using var certificates = TestCertificates.Create();
        await using var streams = await ConnectedPipePair.CreateAsync();
        var serverAuthenticator = CreateAuthenticator(
            certificates,
            Fingerprint(certificates.Client));
        var clientValidator = new TestRemoteCertificateValidator(certificates.Root);
        var clientAuthenticator = CreateAuthenticator(
            clientValidator,
            Fingerprint(certificates.Server));

        var serverTask = serverAuthenticator.AuthenticateServerAsync(
            streams.Server,
            certificates.Server);
        var clientTask = clientAuthenticator.AuthenticateClientAsync(
            streams.Client,
            ExpectedHost,
            certificates.Client);
        try
        {
            await Task.WhenAll(serverTask, clientTask);
        }
        catch
        {
            throw new InvalidOperationException(
                $"Server: {DescribeTask(serverTask)} Client: {DescribeTask(clientTask)}");
        }
        await using var server = await serverTask;
        await using var client = await clientTask;

        var payload = "protected test payload"u8.ToArray();
        using var transferTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new byte[payload.Length];
        var readTask = server.ProtectedStream
            .ReadExactlyAsync(received, transferTimeout.Token)
            .AsTask();
        await client.ProtectedStream.WriteAsync(payload, transferTimeout.Token);
        await client.ProtectedStream.FlushAsync(transferTimeout.Token);
        await readTask;

        Assert.Equal(payload, received);
        Assert.True(server.ProtectedStream.IsEncrypted);
        Assert.True(server.ProtectedStream.IsMutuallyAuthenticated);
        Assert.StartsWith("mtls-sha256:", server.Session.RemoteIdentityKey, StringComparison.Ordinal);
        Assert.Equal(AuthenticationScheme.MutualTls, server.Session.Scheme);
    }

    [Fact]
    public async Task UnauthorizedClientIsRejectedAfterCertificateValidation()
    {
        using var certificates = TestCertificates.Create();
        await using var streams = await ConnectedPipePair.CreateAsync();
        var serverAuthenticator = CreateAuthenticator(
            certificates,
            TestCertificates.CreateUnrelatedFingerprint());
        var clientAuthenticator = CreateAuthenticator(
            certificates,
            Fingerprint(certificates.Server));

        var serverTask = serverAuthenticator.AuthenticateServerAsync(
            streams.Server,
            certificates.Server);
        var clientTask = clientAuthenticator.AuthenticateClientAsync(
            streams.Client,
            ExpectedHost,
            certificates.Client);

        await Assert.ThrowsAsync<MutualTlsAuthenticationException>(() => serverTask);
        await DisposeSuccessfulResultAsync(clientTask);
    }

    [Fact]
    public async Task WrongReceiverHostnameIsRejected()
    {
        using var certificates = TestCertificates.Create();
        await using var streams = await ConnectedPipePair.CreateAsync();
        var serverAuthenticator = CreateAuthenticator(
            certificates,
            Fingerprint(certificates.Client));
        var clientValidator = new TestRemoteCertificateValidator(certificates.Root);
        var clientAuthenticator = CreateAuthenticator(
            clientValidator,
            Fingerprint(certificates.Server));

        var serverTask = serverAuthenticator.AuthenticateServerAsync(
            streams.Server,
            certificates.Server);
        var clientTask = clientAuthenticator.AuthenticateClientAsync(
            streams.Client,
            "wrong.example.test",
            certificates.Client);
        var abortTask = AbortFailedHandshakeAsync(streams);

        await AssertAuthenticationRejectedAsync(clientTask);
        Assert.True(clientValidator.SawNameMismatch);
        await ObserveFailureOrDisposeAsync(serverTask);
        await abortTask;
    }

    [Fact]
    public async Task MissingClientCertificateIsRejected()
    {
        using var certificates = TestCertificates.Create();
        await using var streams = await ConnectedPipePair.CreateAsync();
        var (serverAuthenticator, clientAuthenticator) = CreateAuthenticators(certificates);

        var serverTask = serverAuthenticator.AuthenticateServerAsync(
            streams.Server,
            certificates.Server);
        var clientTask = clientAuthenticator.AuthenticateClientAsync(
            streams.Client,
            ExpectedHost,
            clientCertificate: null);
        var abortTask = AbortFailedHandshakeAsync(streams);

        await AssertAuthenticationRejectedAsync(serverTask);
        await ObserveFailureOrDisposeAsync(clientTask);
        await abortTask;
    }

    [Fact]
    public async Task ExpiredClientCertificateIsRejected()
    {
        using var certificates = TestCertificates.Create(expiredClient: true);
        await using var streams = await ConnectedPipePair.CreateAsync();
        var serverValidator = new TestRemoteCertificateValidator(certificates.Root);
        var serverAuthenticator = CreateAuthenticator(
            serverValidator,
            Fingerprint(certificates.Client));
        var clientAuthenticator = CreateAuthenticator(
            certificates,
            Fingerprint(certificates.Server));

        var serverTask = serverAuthenticator.AuthenticateServerAsync(
            streams.Server,
            certificates.Server);
        var clientTask = clientAuthenticator.AuthenticateClientAsync(
            streams.Client,
            ExpectedHost,
            certificates.Client);
        var abortTask = AbortFailedHandshakeAsync(streams);

        await AssertAuthenticationRejectedAsync(serverTask);
        Assert.True(serverValidator.SawChainFailure);
        await ObserveFailureOrDisposeAsync(clientTask);
        await abortTask;
    }

    private static (
        MutualTlsStreamAuthenticator Server,
        MutualTlsStreamAuthenticator Client)
        CreateAuthenticators(TestCertificates certificates)
    {
        return (
            CreateAuthenticator(certificates, Fingerprint(certificates.Client)),
            CreateAuthenticator(certificates, Fingerprint(certificates.Server)));
    }

    private static MutualTlsStreamAuthenticator CreateAuthenticator(
        TestCertificates certificates,
        CertificateFingerprint allowedFingerprint)
    {
        return CreateAuthenticator(
            new TestRemoteCertificateValidator(certificates.Root),
            allowedFingerprint);
    }

    private static MutualTlsStreamAuthenticator CreateAuthenticator(
        TestRemoteCertificateValidator validator,
        CertificateFingerprint allowedFingerprint)
    {
        return new(
            new CertificateSenderAllowList([allowedFingerprint]),
            validator,
            TimeSpan.FromSeconds(5),
            X509RevocationMode.NoCheck,
            SslProtocols.Tls12);
    }

    private static CertificateFingerprint Fingerprint(X509Certificate2 certificate)
    {
        return CertificateFingerprint.FromSha256Bytes(SHA256.HashData(certificate.RawData));
    }

    private static async Task DisposeSuccessfulResultAsync(
        Task<MutualTlsAuthenticationResult> task)
    {
        try
        {
            var result = await task;
            result.ProtectedStream.Dispose();
        }
        catch (MutualTlsAuthenticationException)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    private static async Task AssertAuthenticationRejectedAsync(
        Task<MutualTlsAuthenticationResult> task)
    {
        var exception = await Record.ExceptionAsync(() => task);
        Assert.True(
            exception is MutualTlsAuthenticationException or TimeoutException,
            exception?.ToString());
    }

    private static async Task AbortFailedHandshakeAsync(ConnectedPipePair streams)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        streams.Client.Dispose();
        streams.Server.Dispose();
    }

    private static string DescribeTask(Task task)
    {
        return task.IsCompletedSuccessfully
            ? "success"
            : task.Exception?.GetBaseException().ToString() ?? task.Status.ToString();
    }

    private static async Task ObserveFailureOrDisposeAsync(
        Task<MutualTlsAuthenticationResult> task)
    {
        try
        {
            var result = await task;
            result.ProtectedStream.Dispose();
        }
        catch (MutualTlsAuthenticationException)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    private sealed class TestRemoteCertificateValidator(X509Certificate2 root)
        : IRemoteCertificateValidator
    {
        internal bool SawChainFailure { get; private set; }

        internal bool SawNameMismatch { get; private set; }

        public bool Validate(
            X509Certificate2 certificate,
            X509Chain? chain,
            SslPolicyErrors policyErrors,
            RemoteCertificateRole role)
        {
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                SawNameMismatch = true;
                return false;
            }

            using var customChain = new X509Chain();
            customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            customChain.ChainPolicy.CustomTrustStore.Add(root);
            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            customChain.ChainPolicy.DisableCertificateDownloads = true;
            customChain.ChainPolicy.ApplicationPolicy.Add(
                new Oid(
                    role == RemoteCertificateRole.Client
                        ? TestCertificates.ClientAuthenticationOid
                        : TestCertificates.ServerAuthenticationOid));
            var isValid = customChain.Build(certificate);
            SawChainFailure = !isValid;
            return isValid;
        }
    }

    private sealed class TestCertificates : IDisposable
    {
        internal const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";
        internal const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

        private TestCertificates(
            X509Certificate2 root,
            X509Certificate2 server,
            X509Certificate2 client)
        {
            Root = root;
            Server = server;
            Client = client;
        }

        internal X509Certificate2 Root { get; }

        internal X509Certificate2 Server { get; }

        internal X509Certificate2 Client { get; }

        internal static TestCertificates Create(bool expiredClient = false)
        {
            var now = DateTimeOffset.UtcNow;
            var root = CreateRoot(now);
            var server = CreateLeaf(
                root,
                "CN=Unskip Test Receiver",
                ServerAuthenticationOid,
                now.AddDays(-1),
                now.AddDays(2),
                ExpectedHost);
            var client = CreateLeaf(
                root,
                "CN=Unskip Test Sender",
                ClientAuthenticationOid,
                expiredClient ? now.AddDays(-2) : now.AddDays(-1),
                expiredClient ? now.AddDays(-1) : now.AddDays(2),
                dnsName: null);
            return new(root, server, client);
        }

        internal static CertificateFingerprint CreateUnrelatedFingerprint()
        {
            return CertificateFingerprint.FromSha256Bytes(
                Enumerable.Repeat((byte)0xA5, 32).ToArray());
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
            Root.Dispose();
        }

        private static X509Certificate2 CreateRoot(DateTimeOffset now)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=Unskip Test Root",
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    true));
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            return request.CreateSelfSigned(now.AddDays(-3), now.AddDays(3));
        }

        private static X509Certificate2 CreateLeaf(
            X509Certificate2 root,
            string subject,
            string enhancedKeyUsage,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            string? dnsName)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest(
                subject,
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new(enhancedKeyUsage) },
                    true));
            if (dnsName is not null)
            {
                var names = new SubjectAlternativeNameBuilder();
                names.AddDnsName(dnsName);
                request.CertificateExtensions.Add(names.Build());
            }

            var serial = RandomNumberGenerator.GetBytes(16);
            using var publicCertificate = request.Create(
                root,
                notBefore,
                notAfter,
                serial);
            using var certificateWithKey = publicCertificate.CopyWithPrivateKey(key);
            const string password = "ephemeral-test-only";
            var pkcs12 = certificateWithKey.Export(X509ContentType.Pkcs12, password);
            return X509CertificateLoader.LoadPkcs12(
                pkcs12,
                password,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        }
    }

    private sealed class ConnectedPipePair : IAsyncDisposable
    {
        private ConnectedPipePair(
            NamedPipeServerStream server,
            NamedPipeClientStream client)
        {
            Server = server;
            Client = client;
        }

        internal NamedPipeServerStream Server { get; }

        internal NamedPipeClientStream Client { get; }

        internal static async Task<ConnectedPipePair> CreateAsync()
        {
            var pipeName = $"unskip-test-{Guid.NewGuid():N}";
            var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            await Task.WhenAll(
                server.WaitForConnectionAsync(),
                client.ConnectAsync());
            return new(server, client);
        }

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            Server.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
