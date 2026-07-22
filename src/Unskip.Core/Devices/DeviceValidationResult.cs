namespace Unskip.Core.Devices;

public sealed class DeviceValidationResult
{
    private DeviceValidationResult(
        ValidatedDeviceInput? value,
        IReadOnlyList<DeviceValidationError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public bool IsValid => Value is not null;

    public ValidatedDeviceInput? Value { get; }

    public IReadOnlyList<DeviceValidationError> Errors { get; }

    public static DeviceValidationResult Success(ValidatedDeviceInput value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new DeviceValidationResult(value, []);
    }

    public static DeviceValidationResult Failure(IReadOnlyList<DeviceValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new DeviceValidationResult(null, errors);
    }
}
