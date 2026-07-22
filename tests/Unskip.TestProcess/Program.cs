using System.Globalization;

namespace Unskip.TestProcess;

public static class Program
{
    public static async Task<int> Main(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return 64;
        }

        switch (arguments[0])
        {
            case "emit" when arguments.Length == 4:
                await Console.Out.WriteAsync(arguments[1]).ConfigureAwait(false);
                await Console.Error.WriteAsync(arguments[2]).ConfigureAwait(false);
                return int.Parse(arguments[3], CultureInfo.InvariantCulture);

            case "delay" when arguments.Length == 2:
                var milliseconds = int.Parse(arguments[1], CultureInfo.InvariantCulture);
                await Task.Delay(milliseconds).ConfigureAwait(false);
                return 0;

            default:
                return 64;
        }
    }
}
