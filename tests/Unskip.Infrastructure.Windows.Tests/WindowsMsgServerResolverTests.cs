using System.Net;
using System.Net.Sockets;
using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class WindowsMsgServerResolverTests
{
    [Fact]
    public async Task HostnamePassesThroughWithoutDnsLookup()
    {
        var dns = new StubDnsLookup();
        var resolver = new WindowsMsgServerResolver(dns);

        var result = await resolver.ResolveAsync(
            new MessageTarget("desktop-01", MessageTargetKind.Hostname),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("desktop-01", result.ServerName);
        Assert.Equal(0, dns.ReverseLookupCount);
        Assert.Equal(0, dns.ForwardLookupCount);
    }

    [Fact]
    public async Task Ipv4UsesForwardVerifiedReverseDnsName()
    {
        var dns = new StubDnsLookup
        {
            ReverseResult = Entry("HOST-25.EXAMPLE.TEST", "192.0.2.25"),
            ForwardResult = [IPAddress.Parse("192.0.2.25")],
        };
        var resolver = new WindowsMsgServerResolver(dns);

        var result = await resolver.ResolveAsync(
            new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("host-25.example.test", result.ServerName);
        Assert.Equal(1, dns.ReverseLookupCount);
        Assert.Equal(1, dns.ForwardLookupCount);
    }

    [Fact]
    public async Task MissingPtrResultFailsCleanly()
    {
        var dns = new StubDnsLookup
        {
            ReverseException = new SocketException((int)SocketError.HostNotFound),
        };
        var resolver = new WindowsMsgServerResolver(dns);

        var result = await resolver.ResolveAsync(
            new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.ServerName);
        Assert.Contains("could not resolve", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, dns.ForwardLookupCount);
    }

    [Fact]
    public async Task ReverseLookupReturningOnlyAddressIsRejected()
    {
        var dns = new StubDnsLookup
        {
            ReverseResult = Entry("192.0.2.25", "192.0.2.25"),
        };
        var resolver = new WindowsMsgServerResolver(dns);

        var result = await resolver.ResolveAsync(
            new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("valid computer name", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, dns.ForwardLookupCount);
    }

    [Fact]
    public async Task ForwardMismatchIsRejected()
    {
        var dns = new StubDnsLookup
        {
            ReverseResult = Entry("host-25.example.test", "192.0.2.25"),
            ForwardResult = [IPAddress.Parse("192.0.2.26")],
        };
        var resolver = new WindowsMsgServerResolver(dns);

        var result = await resolver.ResolveAsync(
            new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("did not resolve back", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        var dns = new StubDnsLookup { WaitForCancellation = true };
        var resolver = new WindowsMsgServerResolver(dns);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => resolver.ResolveAsync(
                new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
                cancellationSource.Token));
    }

    [Fact]
    public async Task LookupTimeoutReturnsFailure()
    {
        var dns = new StubDnsLookup { WaitForCancellation = true };
        var resolver = new WindowsMsgServerResolver(dns, TimeSpan.FromMilliseconds(20));

        var result = await resolver.ResolveAsync(
            new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("timeout", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    private static IPHostEntry Entry(string hostName, string address)
    {
        return new IPHostEntry
        {
            HostName = hostName,
            Aliases = [],
            AddressList = [IPAddress.Parse(address)],
        };
    }

    private sealed class StubDnsLookup : IDnsLookup
    {
        public IPHostEntry ReverseResult { get; init; } = Entry(
            "host-25.example.test",
            "192.0.2.25");

        public IPAddress[] ForwardResult { get; init; } = [IPAddress.Parse("192.0.2.25")];

        public SocketException? ReverseException { get; init; }

        public bool WaitForCancellation { get; init; }

        public int ReverseLookupCount { get; private set; }

        public int ForwardLookupCount { get; private set; }

        public async Task<IPHostEntry> GetHostEntryAsync(
            IPAddress address,
            CancellationToken cancellationToken)
        {
            ReverseLookupCount++;
            if (WaitForCancellation)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            if (ReverseException is not null)
            {
                throw ReverseException;
            }

            return ReverseResult;
        }

        public Task<IPAddress[]> GetHostAddressesAsync(
            string hostName,
            CancellationToken cancellationToken)
        {
            ForwardLookupCount++;
            return Task.FromResult(ForwardResult);
        }
    }
}
