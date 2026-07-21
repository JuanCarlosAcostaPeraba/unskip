using System.Globalization;
using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows;

internal static class NativeFailureMapper
{
    public static (MessageFailureCategory Category, string UserMessage) Map(
        int exitCode,
        string standardOutput,
        string standardError)
    {
        var diagnostic = string.Concat(standardOutput, Environment.NewLine, standardError);
        if (exitCode == 5 || ContainsErrorCode(diagnostic, 5))
        {
            return (
                MessageFailureCategory.PermissionDenied,
                "Windows rejected the request because the sender does not have permission to message sessions on that computer.");
        }

        if (exitCode is 53 or 1_722 or 1_726
            || ContainsErrorCode(diagnostic, 53)
            || ContainsErrorCode(diagnostic, 1_722)
            || ContainsErrorCode(diagnostic, 1_726))
        {
            return (
                MessageFailureCategory.TargetUnavailable,
                "Windows could not contact the target computer or its remote session service.");
        }

        return (
            MessageFailureCategory.NativeRejected,
            "Windows rejected the message request. Review the technical details and the target computer prerequisites.");
    }

    private static bool ContainsErrorCode(string value, int errorCode)
    {
        var token = errorCode.ToString(CultureInfo.InvariantCulture);
        var searchFrom = 0;
        while (searchFrom < value.Length)
        {
            var index = value.IndexOf(token, searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            var beginsAtBoundary = index == 0 || !char.IsDigit(value[index - 1]);
            var afterIndex = index + token.Length;
            var endsAtBoundary = afterIndex == value.Length || !char.IsDigit(value[afterIndex]);
            if (beginsAtBoundary && endsAtBoundary)
            {
                return true;
            }

            searchFrom = index + token.Length;
        }

        return false;
    }
}
