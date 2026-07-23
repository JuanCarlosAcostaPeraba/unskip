using System.Text.RegularExpressions;

namespace Unskip.Core.Updates;

public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    private static readonly Regex Pattern = new(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<pre>(?:(?:0|[1-9]\d*)|(?:\d*[A-Za-z-][0-9A-Za-z-]*))(?:\.(?:(?:0|[1-9]\d*)|(?:\d*[A-Za-z-][0-9A-Za-z-]*)))*))?$",
        RegexOptions.CultureInvariant);

    private readonly string[] _preReleaseIdentifiers;

    private SemanticVersion(int major, int minor, int patch, string[] preReleaseIdentifiers)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        _preReleaseIdentifiers = preReleaseIdentifiers;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public bool IsPreRelease => _preReleaseIdentifiers.Length > 0;

    public static SemanticVersion Parse(string value)
    {
        if (!TryParse(value, out var version))
        {
            throw new FormatException("The value is not a supported semantic version.");
        }

        return version;
    }

    public static bool TryParse(string? value, out SemanticVersion version)
    {
        version = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = Pattern.Match(value);
        if (!match.Success
            || !int.TryParse(match.Groups["major"].Value, out var major)
            || !int.TryParse(match.Groups["minor"].Value, out var minor)
            || !int.TryParse(match.Groups["patch"].Value, out var patch))
        {
            return false;
        }

        var preRelease = match.Groups["pre"].Success
            ? match.Groups["pre"].Value.Split('.')
            : [];
        version = new SemanticVersion(major, minor, patch, preRelease);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var coreComparison = Major.CompareTo(other.Major);
        if (coreComparison == 0)
        {
            coreComparison = Minor.CompareTo(other.Minor);
        }

        if (coreComparison == 0)
        {
            coreComparison = Patch.CompareTo(other.Patch);
        }

        if (coreComparison != 0)
        {
            return coreComparison;
        }

        if (!IsPreRelease || !other.IsPreRelease)
        {
            return IsPreRelease.CompareTo(other.IsPreRelease) * -1;
        }

        for (var index = 0; index < Math.Min(_preReleaseIdentifiers.Length, other._preReleaseIdentifiers.Length); index++)
        {
            var identifierComparison = CompareIdentifier(
                _preReleaseIdentifiers[index],
                other._preReleaseIdentifiers[index]);
            if (identifierComparison != 0)
            {
                return identifierComparison;
            }
        }

        return _preReleaseIdentifiers.Length.CompareTo(other._preReleaseIdentifiers.Length);
    }

    public bool Equals(SemanticVersion? other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        Equals(left, right);

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) =>
        !Equals(left, right);

    public static bool operator <(SemanticVersion? left, SemanticVersion? right) =>
        Comparer<SemanticVersion>.Default.Compare(left, right) < 0;

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) =>
        Comparer<SemanticVersion>.Default.Compare(left, right) <= 0;

    public static bool operator >(SemanticVersion? left, SemanticVersion? right) =>
        Comparer<SemanticVersion>.Default.Compare(left, right) > 0;

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) =>
        Comparer<SemanticVersion>.Default.Compare(left, right) >= 0;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Major);
        hash.Add(Minor);
        hash.Add(Patch);
        foreach (var identifier in _preReleaseIdentifiers)
        {
            hash.Add(identifier, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var core = $"{Major}.{Minor}.{Patch}";
        return IsPreRelease ? $"{core}-{string.Join('.', _preReleaseIdentifiers)}" : core;
    }

    private static int CompareIdentifier(string left, string right)
    {
        var leftIsNumeric = left.All(char.IsDigit);
        var rightIsNumeric = right.All(char.IsDigit);
        if (leftIsNumeric && rightIsNumeric)
        {
            var lengthComparison = left.Length.CompareTo(right.Length);
            return lengthComparison != 0 ? lengthComparison : string.CompareOrdinal(left, right);
        }

        if (leftIsNumeric != rightIsNumeric)
        {
            return leftIsNumeric ? -1 : 1;
        }

        return string.CompareOrdinal(left, right);
    }
}
