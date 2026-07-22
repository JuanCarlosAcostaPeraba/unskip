using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Unskip.Core.Networking;

public static class NetworkAddressValidator
{
    public const int MaximumHostnameLength = 253;

    public static bool IsIpv4Address(string value)
    {
        return IPAddress.TryParse(value, out var address)
            && address.AddressFamily == AddressFamily.InterNetwork;
    }

    public static bool TryNormalizeHostname(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (candidate.Length is 0 or > MaximumHostnameLength
            || candidate[0] == '.'
            || candidate[^1] == '.')
        {
            return false;
        }

        foreach (var label in candidate.Split('.'))
        {
            if (label.Length is 0 or > 63
                || !IsAsciiLetterOrDigit(label[0])
                || !IsAsciiLetterOrDigit(label[^1])
                || label.Any(character => !IsAsciiLetterOrDigit(character) && character != '-'))
            {
                return false;
            }
        }

        normalized = candidate.ToLowerInvariant();
        return true;
    }

    public static bool TryNormalizeCanonicalIpv4(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        var segments = candidate.Split('.');
        if (segments.Length != 4)
        {
            return false;
        }

        Span<byte> octets = stackalloc byte[4];
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (segment.Length is 0 or > 3
                || (segment.Length > 1 && segment[0] == '0')
                || segment.Any(character => character is < '0' or > '9')
                || !byte.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out octets[index]))
            {
                return false;
            }
        }

        normalized = string.Create(
            CultureInfo.InvariantCulture,
            $"{octets[0]}.{octets[1]}.{octets[2]}.{octets[3]}");
        return true;
    }

    private static bool IsAsciiLetterOrDigit(char value)
    {
        return value is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z'
            or >= '0' and <= '9';
    }
}
