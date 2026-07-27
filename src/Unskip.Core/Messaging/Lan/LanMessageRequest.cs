using System.Text.Json.Serialization;

namespace Unskip.Core.Messaging.Lan;

public sealed record LanMessageRequest(
    [property: JsonRequired, JsonPropertyOrder(0)] int Version,
    [property: JsonRequired, JsonPropertyOrder(1)] Guid MessageId,
    [property: JsonRequired, JsonPropertyOrder(2)] DateTimeOffset SentAtUtc,
    [property: JsonRequired, JsonPropertyOrder(3)] DateTimeOffset ExpiresAtUtc,
    [property: JsonRequired, JsonPropertyOrder(4)] string Nonce,
    [property: JsonRequired, JsonPropertyOrder(5)] LanMessageKind Kind,
    [property: JsonRequired, JsonPropertyOrder(6)] string Message);
