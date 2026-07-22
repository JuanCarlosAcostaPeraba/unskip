using Unskip.Core.Devices;

namespace Unskip.Core.Tests.Devices;

public sealed class DeviceValidatorTests
{
    [Fact]
    public void ValidDeviceIsNormalized()
    {
        var result = DeviceValidator.Validate(new DeviceInput(
            "  Joan  ",
            "  CHUC159  ",
            "10.198.198.4",
            "  Shared workstation  ",
            true,
            DeviceDestinationKind.Ipv4));

        Assert.True(result.IsValid);
        Assert.Equal("Joan", result.Value!.Alias);
        Assert.Equal("JOAN", result.Value.AliasKey);
        Assert.Equal("chuc159", result.Value.ComputerName);
        Assert.Equal("10.198.198.4", result.Value.Ipv4Address);
        Assert.Equal("Shared workstation", result.Value.Description);
        Assert.True(result.Value.IsFavorite);
        Assert.Equal(DeviceDestinationKind.Ipv4, result.Value.PreferredDestination);
    }

    [Fact]
    public void AliasAndDestinationAreRequired()
    {
        var result = DeviceValidator.Validate(new DeviceInput(null, null, null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == DeviceValidationErrorCode.Required);
        Assert.Contains(result.Errors, error => error.Code == DeviceValidationErrorCode.DestinationRequired);
    }

    [Theory]
    [InlineData("computer_name", null, DeviceValidationErrorCode.InvalidHostname)]
    [InlineData(null, "10.0.0", DeviceValidationErrorCode.InvalidIpv4)]
    [InlineData(null, "010.0.0.1", DeviceValidationErrorCode.InvalidIpv4)]
    [InlineData(null, "256.0.0.1", DeviceValidationErrorCode.InvalidIpv4)]
    public void InvalidDestinationsAreRejected(
        string? computerName,
        string? ipv4Address,
        DeviceValidationErrorCode expectedCode)
    {
        var result = DeviceValidator.Validate(new DeviceInput(
            "Device",
            computerName,
            ipv4Address,
            null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == expectedCode);
    }

    [Fact]
    public void PreferredDestinationMustExist()
    {
        var result = DeviceValidator.Validate(new DeviceInput(
            "Device",
            "workstation-7",
            null,
            null,
            PreferredDestination: DeviceDestinationKind.Ipv4));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.Code == DeviceValidationErrorCode.PreferredDestinationUnavailable);
    }

    [Fact]
    public void AliasUsesCanonicalUnicodeNormalization()
    {
        var result = DeviceValidator.Validate(new DeviceInput(
            "Cafe\u0301",
            "workstation-7",
            null,
            null));

        Assert.True(result.IsValid);
        Assert.Equal("Café", result.Value!.Alias);
        Assert.Equal("CAFÉ", result.Value.AliasKey);
    }

    [Fact]
    public void HostnameIsPreferredByDefaultWhenBothDestinationsExist()
    {
        var result = DeviceValidator.Validate(new DeviceInput(
            "Device",
            "workstation-7",
            "192.0.2.7",
            null));

        Assert.True(result.IsValid);
        Assert.Equal(DeviceDestinationKind.Hostname, result.Value!.PreferredDestination);
    }

    [Fact]
    public void OversizedAndControlCharacterFieldsAreRejected()
    {
        var result = DeviceValidator.Validate(new DeviceInput(
            new string('a', DevicePolicy.MaximumAliasLength + 1) + "\0",
            "workstation-7",
            null,
            new string('d', DevicePolicy.MaximumDescriptionLength + 1) + "\0"));

        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, error => error.Field == "Alias" && error.Code == DeviceValidationErrorCode.TooLong);
        Assert.Contains(result.Errors, error => error.Field == "Alias" && error.Code == DeviceValidationErrorCode.InvalidCharacters);
        Assert.Contains(result.Errors, error => error.Field == "Description" && error.Code == DeviceValidationErrorCode.TooLong);
        Assert.Contains(result.Errors, error => error.Field == "Description" && error.Code == DeviceValidationErrorCode.InvalidCharacters);
    }
}
