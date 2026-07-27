using System.Security.Cryptography;

namespace Unskip.Core.Messaging.Lan;

public sealed class CertificateFingerprint : IEquatable<CertificateFingerprint>
{
    private const int Sha256ByteLength = 32;
    private readonly byte[] _bytes;

    private CertificateFingerprint(byte[] bytes)
    {
        _bytes = bytes;
        Value = Convert.ToHexStringLower(bytes);
    }

    public string Value { get; }

    public static CertificateFingerprint FromSha256Bytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Sha256ByteLength)
        {
            throw new ArgumentException(
                $"A SHA-256 certificate fingerprint must contain {Sha256ByteLength} bytes.",
                nameof(bytes));
        }

        return new CertificateFingerprint(bytes.ToArray());
    }

    public static bool TryParse(string? candidate, out CertificateFingerprint? fingerprint)
    {
        fingerprint = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var normalized = NormalizeHex(candidate.Trim());
        if (normalized is null)
        {
            return false;
        }

        var bytes = Convert.FromHexString(normalized);
        fingerprint = new CertificateFingerprint(bytes);
        return true;
    }

    public bool Equals(CertificateFingerprint? other)
    {
        return other is not null
            && CryptographicOperations.FixedTimeEquals(_bytes, other._bytes);
    }

    public override bool Equals(object? obj)
    {
        return obj is CertificateFingerprint other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            BitConverter.ToUInt64(_bytes, 0),
            BitConverter.ToUInt64(_bytes, sizeof(ulong)));
    }

    public override string ToString()
    {
        return Value;
    }

    private static string? NormalizeHex(string candidate)
    {
        if (candidate.Length == Sha256ByteLength * 2
            && candidate.All(char.IsAsciiHexDigit))
        {
            return candidate;
        }

        var expectedSeparatedLength = (Sha256ByteLength * 3) - 1;
        if (candidate.Length != expectedSeparatedLength)
        {
            return null;
        }

        var separator = candidate[2];
        if (separator is not ':' and not '-')
        {
            return null;
        }

        Span<char> normalized = stackalloc char[Sha256ByteLength * 2];
        for (var byteIndex = 0; byteIndex < Sha256ByteLength; byteIndex++)
        {
            var candidateIndex = byteIndex * 3;
            if (!char.IsAsciiHexDigit(candidate[candidateIndex])
                || !char.IsAsciiHexDigit(candidate[candidateIndex + 1])
                || (byteIndex < Sha256ByteLength - 1
                    && candidate[candidateIndex + 2] != separator))
            {
                return null;
            }

            normalized[byteIndex * 2] = candidate[candidateIndex];
            normalized[(byteIndex * 2) + 1] = candidate[candidateIndex + 1];
        }

        return normalized.ToString();
    }
}
