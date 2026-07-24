namespace Unskip.Infrastructure.Windows;

internal sealed record WindowsMsgServerResolution(
    bool IsSuccess,
    string? ServerName,
    string UserMessage,
    string Diagnostic)
{
    public static WindowsMsgServerResolution Success(string serverName)
    {
        return new WindowsMsgServerResolution(
            true,
            serverName,
            string.Empty,
            string.Empty);
    }

    public static WindowsMsgServerResolution Failure(string diagnostic)
    {
        return new WindowsMsgServerResolution(
            false,
            null,
            "Windows needs a verified computer name for this IPv4 destination. Check DNS or use the computer name.",
            diagnostic);
    }
}
