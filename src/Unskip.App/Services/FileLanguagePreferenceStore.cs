using System.IO;
using System.Text;

namespace Unskip.App.Services;

internal sealed class FileLanguagePreferenceStore(string path) : ILanguagePreferenceStore
{
    private readonly string _path = ValidatePath(path);

    public static FileLanguagePreferenceStore ForCurrentUser()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unskip");
        return new FileLanguagePreferenceStore(Path.Combine(directory, "language.txt"));
    }

    public string? Load()
    {
        try
        {
            return File.Exists(_path)
                ? File.ReadAllText(_path, Encoding.UTF8).Trim()
                : null;
        }
        catch (Exception exception) when (IsExpectedFileFailure(exception))
        {
            return null;
        }
    }

    public bool TrySave(string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        try
        {
            var directory = Path.GetDirectoryName(_path)
                ?? throw new InvalidOperationException("The language preference path has no directory.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(_path, language, new UTF8Encoding(false));
            return true;
        }
        catch (Exception exception) when (IsExpectedFileFailure(exception))
        {
            return false;
        }
    }

    private static string ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("The language preference path must be absolute.", nameof(path));
        }

        return path;
    }

    private static bool IsExpectedFileFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException;
}
