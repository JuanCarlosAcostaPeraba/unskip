using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unskip.Core.Messaging.Lan;

public sealed class LanProtocolFrameCodec(LanMessageRequestValidator requestValidator)
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly LanMessageRequestValidator _requestValidator =
        requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));

    public async Task WriteRequestAsync(
        Stream stream,
        LanMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(request);

        var validation = _requestValidator.Validate(request);
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                $"The LAN message request is invalid: {validation.Error}.",
                nameof(request));
        }

        await WritePayloadAsync(stream, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LanFrameReadResult<LanMessageRequest>> ReadRequestAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var payloadResult = await ReadPayloadAsync(stream, cancellationToken).ConfigureAwait(false);
        if (payloadResult.Status != LanFrameReadStatus.Success)
        {
            return new(payloadResult.Status, null, LanRequestValidationError.None);
        }

        LanMessageRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<LanMessageRequest>(
                payloadResult.Payload!,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return new(
                LanFrameReadStatus.MalformedPayload,
                null,
                LanRequestValidationError.None);
        }
        catch (NotSupportedException)
        {
            return new(
                LanFrameReadStatus.MalformedPayload,
                null,
                LanRequestValidationError.None);
        }

        if (request is null)
        {
            return new(
                LanFrameReadStatus.MalformedPayload,
                null,
                LanRequestValidationError.None);
        }

        var validation = _requestValidator.Validate(request);
        if (!validation.IsValid)
        {
            var status = validation.Error == LanRequestValidationError.UnsupportedVersion
                ? LanFrameReadStatus.UnsupportedVersion
                : LanFrameReadStatus.InvalidPayload;
            return new(status, null, validation.Error);
        }

        return new(LanFrameReadStatus.Success, request, LanRequestValidationError.None);
    }

    public static async Task WriteResponseAsync(
        Stream stream,
        LanReceiverResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(response);

        if (!IsValidResponse(response))
        {
            throw new ArgumentException("The LAN receiver response is invalid.", nameof(response));
        }

        await WritePayloadAsync(stream, response, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<LanFrameReadResult<LanReceiverResponse>> ReadResponseAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var payloadResult = await ReadPayloadAsync(stream, cancellationToken).ConfigureAwait(false);
        if (payloadResult.Status != LanFrameReadStatus.Success)
        {
            return new(payloadResult.Status, null, LanRequestValidationError.None);
        }

        LanReceiverResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<LanReceiverResponse>(
                payloadResult.Payload!,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return new(
                LanFrameReadStatus.MalformedPayload,
                null,
                LanRequestValidationError.None);
        }
        catch (NotSupportedException)
        {
            return new(
                LanFrameReadStatus.MalformedPayload,
                null,
                LanRequestValidationError.None);
        }

        return response is not null && IsValidResponse(response)
            ? new(LanFrameReadStatus.Success, response, LanRequestValidationError.None)
            : new(LanFrameReadStatus.InvalidPayload, null, LanRequestValidationError.None);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = false,
        };
        options.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private static async Task WritePayloadAsync<T>(
        Stream stream,
        T value,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        if (payload.Length is <= 0 or > LanProtocolPolicy.MaximumFramePayloadBytes)
        {
            throw new InvalidOperationException("The serialized LAN protocol payload is out of bounds.");
        }

        var prefix = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(prefix, payload.Length);
        await stream.WriteAsync(prefix, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<PayloadReadResult> ReadPayloadAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var prefix = new byte[sizeof(int)];
        var prefixBytes = await ReadExactAsync(stream, prefix, cancellationToken).ConfigureAwait(false);
        if (prefixBytes == 0)
        {
            return new(LanFrameReadStatus.EndOfStream, null);
        }

        if (prefixBytes != prefix.Length)
        {
            return new(LanFrameReadStatus.Truncated, null);
        }

        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(prefix);
        if (payloadLength is <= 0 or > LanProtocolPolicy.MaximumFramePayloadBytes)
        {
            return new(LanFrameReadStatus.InvalidLength, null);
        }

        var payload = new byte[payloadLength];
        var payloadBytes = await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return payloadBytes == payload.Length
            ? new(LanFrameReadStatus.Success, payload)
            : new(LanFrameReadStatus.Truncated, null);
    }

    private static async Task<int> ReadExactAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = await stream
                .ReadAsync(buffer[totalRead..], cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        return totalRead;
    }

    private static bool IsValidResponse(LanReceiverResponse response)
    {
        if (response.Version != LanProtocolPolicy.CurrentVersion
            || !Enum.IsDefined(response.Status)
            || !Enum.IsDefined(response.Code))
        {
            return false;
        }

        return response.Status switch
        {
            LanReceiverStatus.AcceptedForLocalHandling =>
                response.MessageId != Guid.Empty
                    && response.Code == LanReceiverResponseCode.Accepted,
            LanReceiverStatus.RateLimited =>
                response.Code == LanReceiverResponseCode.RateLimitExceeded,
            LanReceiverStatus.UnsupportedVersion =>
                response.Code == LanReceiverResponseCode.UnsupportedVersion,
            LanReceiverStatus.Rejected =>
                response.Code is not LanReceiverResponseCode.Accepted
                    and not LanReceiverResponseCode.RateLimitExceeded
                    and not LanReceiverResponseCode.UnsupportedVersion,
            _ => false,
        };
    }

    private sealed record PayloadReadResult(LanFrameReadStatus Status, byte[]? Payload);
}
