namespace Unskip.App.Services;

public interface ILanguagePreferenceStore
{
    string? Load();

    bool TrySave(string language);
}
