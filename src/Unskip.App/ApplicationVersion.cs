using System.Reflection;
using Unskip.Core.Updates;

namespace Unskip.App;

internal static class ApplicationVersion
{
    public static string Value { get; } = GetVersion(typeof(App).Assembly);

    public static SemanticVersion Current { get; } = SemanticVersion.Parse(Value);

    private static string GetVersion(Assembly assembly)
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = informationalVersion?.Split('+', 2)[0]
            ?? assembly.GetName().Version?.ToString(3)
            ?? "development";

        return version;
    }
}
