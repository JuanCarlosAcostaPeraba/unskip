using System.Text;
using Unskip.Core.Networking;

namespace Unskip.Core.Devices;

public static class DeviceValidator
{
    public static DeviceValidationResult Validate(DeviceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new List<DeviceValidationError>();
        var alias = ValidateAlias(input.Alias, errors);
        var computerName = ValidateComputerName(input.ComputerName, errors);
        var ipv4Address = ValidateIpv4Address(input.Ipv4Address, errors);
        var description = ValidateDescription(input.Description, errors);

        if (string.IsNullOrWhiteSpace(input.ComputerName)
            && string.IsNullOrWhiteSpace(input.Ipv4Address))
        {
            errors.Add(new DeviceValidationError(
                "Destination",
                DeviceValidationErrorCode.DestinationRequired,
                "Enter a computer name or an IPv4 address."));
        }

        var preferredDestination = ResolvePreferredDestination(
            input.PreferredDestination,
            computerName,
            ipv4Address,
            errors);

        if (errors.Count > 0)
        {
            return DeviceValidationResult.Failure(errors);
        }

        return DeviceValidationResult.Success(new ValidatedDeviceInput(
            alias!,
            CreateAliasKey(alias!),
            computerName,
            ipv4Address,
            description,
            input.IsFavorite,
            preferredDestination!.Value));
    }

    public static string CreateAliasKey(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        return alias.Trim().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static string? ValidateAlias(
        string? value,
        List<DeviceValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new DeviceValidationError(
                "Alias",
                DeviceValidationErrorCode.Required,
                "Enter an alias."));
            return null;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormC);
        if (normalized.Length > DevicePolicy.MaximumAliasLength)
        {
            errors.Add(new DeviceValidationError(
                "Alias",
                DeviceValidationErrorCode.TooLong,
                $"Use {DevicePolicy.MaximumAliasLength} characters or fewer."));
        }

        if (ContainsUnsupportedControlCharacter(normalized, allowMultiline: false))
        {
            errors.Add(new DeviceValidationError(
                "Alias",
                DeviceValidationErrorCode.InvalidCharacters,
                "Remove control characters from the alias."));
        }

        return normalized;
    }

    private static string? ValidateComputerName(
        string? value,
        List<DeviceValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (NetworkAddressValidator.TryNormalizeHostname(value, out var normalized))
        {
            return normalized;
        }

        errors.Add(new DeviceValidationError(
            "ComputerName",
            DeviceValidationErrorCode.InvalidHostname,
            "Use a computer or DNS name containing only letters, digits, hyphens, and dots."));
        return null;
    }

    private static string? ValidateIpv4Address(
        string? value,
        List<DeviceValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (NetworkAddressValidator.TryNormalizeCanonicalIpv4(value, out var normalized))
        {
            return normalized;
        }

        errors.Add(new DeviceValidationError(
            "Ipv4Address",
            DeviceValidationErrorCode.InvalidIpv4,
            "Enter a canonical IPv4 address with four numbers from 0 to 255."));
        return null;
    }

    private static string? ValidateDescription(
        string? value,
        List<DeviceValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > DevicePolicy.MaximumDescriptionLength)
        {
            errors.Add(new DeviceValidationError(
                "Description",
                DeviceValidationErrorCode.TooLong,
                $"Use {DevicePolicy.MaximumDescriptionLength} characters or fewer."));
        }

        if (ContainsUnsupportedControlCharacter(normalized, allowMultiline: true))
        {
            errors.Add(new DeviceValidationError(
                "Description",
                DeviceValidationErrorCode.InvalidCharacters,
                "Remove unsupported control characters from the description."));
        }

        return normalized;
    }

    private static DeviceDestinationKind? ResolvePreferredDestination(
        DeviceDestinationKind? preferred,
        string? computerName,
        string? ipv4Address,
        List<DeviceValidationError> errors)
    {
        if (computerName is null && ipv4Address is null)
        {
            return null;
        }

        var resolved = preferred
            ?? (computerName is not null ? DeviceDestinationKind.Hostname : DeviceDestinationKind.Ipv4);

        var isAvailable = resolved switch
        {
            DeviceDestinationKind.Hostname => computerName is not null,
            DeviceDestinationKind.Ipv4 => ipv4Address is not null,
            _ => false,
        };

        if (isAvailable)
        {
            return resolved;
        }

        errors.Add(new DeviceValidationError(
            "PreferredDestination",
            DeviceValidationErrorCode.PreferredDestinationUnavailable,
            "Choose a preferred destination that is present on the device."));
        return null;
    }

    private static bool ContainsUnsupportedControlCharacter(string value, bool allowMultiline)
    {
        return value.Any(character => char.IsControl(character)
            && (!allowMultiline || character is not ('\r' or '\n' or '\t')));
    }
}
