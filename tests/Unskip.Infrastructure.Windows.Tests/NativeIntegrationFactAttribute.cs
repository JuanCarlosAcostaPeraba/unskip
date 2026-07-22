namespace Unskip.Infrastructure.Windows.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NativeIntegrationFactAttribute : FactAttribute
{
    public const string EnableVariable = "UNSKIP_RUN_NATIVE_INTEGRATION_TESTS";

    public NativeIntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(EnableVariable),
                "1",
                StringComparison.Ordinal))
        {
            Skip = $"Set {EnableVariable}=1 to run native integration tests.";
        }
    }
}
