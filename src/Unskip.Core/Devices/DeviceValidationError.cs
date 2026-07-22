namespace Unskip.Core.Devices;

public sealed record DeviceValidationError(
    string Field,
    DeviceValidationErrorCode Code,
    string Message);
