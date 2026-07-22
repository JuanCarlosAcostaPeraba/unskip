using System.Reflection;

namespace Unskip.App;

internal static class ApplicationVersion
{
    public static string DisplayLabel { get; } = CreateDisplayLabel(typeof(App).Assembly);

    private static string CreateDisplayLabel(Assembly assembly)
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = informationalVersion?.Split('+', 2)[0]
            ?? assembly.GetName().Version?.ToString(3)
            ?? "development";

        return $"Version {version}";
    }
}
