using System.Buffers.Binary;
using System.Text;
using Unskip.Core.Messaging.Lan;

namespace Unskip.Core.Tests.Messaging.Lan;

public sealed class LanProtocolFrameCodecTests
{
    private readonly MutableClock _clock = new(LanProtocolTestData.Now);

    [Fact]
    public async Task RequestRoundTripUsesDeterministicFraming()
    {
        var codec = CreateCodec();
        var request = LanProtocolTestData.CreateRequest();
        await using var first = new MemoryStream();
        await using var second = new MemoryStream();

        await codec.WriteRequestAsync(first, request);
        await codec.WriteRequestAsync(second, request);
        first.Position = 0;

        var result = await codec.ReadRequestAsync(first);

        Assert.True(result.IsSuccess);
        Assert.Equal(request, result.Value);
        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.True(BinaryPrimitives.ReadInt32BigEndian(first.ToArray()) > 0);
    }

    [Fact]
    public async Task HonestReceiverResponseRoundTrips()
    {
        var response = LanReceiverResponse.Accepted(Guid.NewGuid());
        await using var stream = new MemoryStream();

        await LanProtocolFrameCodec.WriteResponseAsync(stream, response);
        stream.Position = 0;
        var result = await LanProtocolFrameCodec.ReadResponseAsync(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal(LanReceiverStatus.AcceptedForLocalHandling, result.Value!.Status);
        Assert.DoesNotContain(
            "read",
            result.Value.Status.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OversizedLengthIsRejectedBeforePayloadRead()
    {
        var prefix = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(
            prefix,
            LanProtocolPolicy.MaximumFramePayloadBytes + 1);
        await using var stream = new MemoryStream(prefix);

        var result = await CreateCodec().ReadRequestAsync(stream);

        Assert.Equal(LanFrameReadStatus.InvalidLength, result.Status);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    public async Task TruncatedFrameIsRejected(int bytesToKeep)
    {
        var codec = CreateCodec();
        await using var complete = new MemoryStream();
        await codec.WriteRequestAsync(complete, LanProtocolTestData.CreateRequest());
        var truncated = complete.ToArray()[..bytesToKeep];
        await using var stream = new MemoryStream(truncated);

        var result = await codec.ReadRequestAsync(stream);

        Assert.Equal(LanFrameReadStatus.Truncated, result.Status);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{} trailing")]
    [InlineData("{\"version\":1,\"unknown\":true}")]
    [InlineData("{\"version\":1,\"messageId\":\"bff8f9ef-cc1e-4f76-b028-77903ae39787\",\"sentAtUtc\":\"2026-07-27T09:30:00+00:00\",\"expiresAtUtc\":\"2026-07-27T09:31:00+00:00\",\"nonce\":\"AQIDBAUGBwgJCgsMDQ4PEA==\",\"message\":\"Missing kind\"}")]
    public async Task MalformedOrTrailingJsonIsRejected(string json)
    {
        await using var stream = CreateRawFrame(json);

        var result = await CreateCodec().ReadRequestAsync(stream);

        Assert.Equal(LanFrameReadStatus.MalformedPayload, result.Status);
    }

    [Fact]
    public async Task UnsupportedVersionHasExplicitResult()
    {
        var codec = CreateCodec();
        var valid = LanProtocolTestData.CreateRequest();
        await using var stream = new MemoryStream();
        await codec.WriteRequestAsync(stream, valid);
        var json = Encoding.UTF8.GetString(stream.ToArray()[sizeof(int)..])
            .Replace("\"version\":1", "\"version\":2", StringComparison.Ordinal);
        await using var unsupported = CreateRawFrame(json);

        var result = await codec.ReadRequestAsync(unsupported);

        Assert.Equal(LanFrameReadStatus.UnsupportedVersion, result.Status);
        Assert.Equal(LanRequestValidationError.UnsupportedVersion, result.ValidationError);
    }

    [Fact]
    public async Task EndOfStreamIsDistinctFromMalformedInput()
    {
        await using var stream = new MemoryStream();

        var result = await CreateCodec().ReadRequestAsync(stream);

        Assert.Equal(LanFrameReadStatus.EndOfStream, result.Status);
    }

    private LanProtocolFrameCodec CreateCodec()
    {
        return new(new LanMessageRequestValidator(_clock));
    }

    private static MemoryStream CreateRawFrame(string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        var frame = new byte[sizeof(int) + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame, payload.Length);
        payload.CopyTo(frame.AsSpan(sizeof(int)));
        return new MemoryStream(frame);
    }
}
