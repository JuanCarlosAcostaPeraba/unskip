namespace Unskip.Core.Devices;

public sealed record DeviceMutationResult(
    DeviceMutationStatus Status,
    Device? Device,
    IReadOnlyList<DeviceValidationError> ValidationErrors)
{
    public bool IsSuccessful => Status == DeviceMutationStatus.Succeeded;

    public static DeviceMutationResult Success(Device? device = null)
    {
        return new DeviceMutationResult(DeviceMutationStatus.Succeeded, device, []);
    }

    public static DeviceMutationResult ValidationFailure(
        IReadOnlyList<DeviceValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new DeviceMutationResult(DeviceMutationStatus.ValidationFailed, null, errors);
    }

    public static DeviceMutationResult Conflict()
    {
        return new DeviceMutationResult(DeviceMutationStatus.Conflict, null, []);
    }

    public static DeviceMutationResult NotFound()
    {
        return new DeviceMutationResult(DeviceMutationStatus.NotFound, null, []);
    }
}
