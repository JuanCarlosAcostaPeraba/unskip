using Unskip.App.Services;
using Unskip.App.ViewModels;

namespace Unskip.App.Tests;

public sealed class LanguageSettingsViewModelTests
{
    [Fact]
    public void CurrentLanguageCannotBeSelectedAgain()
    {
        var context = CreateContext("en");

        Assert.False(context.ViewModel.ChangeLanguageCommand.CanExecute("en"));
        Assert.True(context.ViewModel.ChangeLanguageCommand.CanExecute("es"));
    }

    [Fact]
    public void CancelledChangeDoesNotSaveOrRestart()
    {
        var context = CreateContext("en");
        context.Confirmation.Result = false;

        context.ViewModel.ChangeLanguageCommand.Execute("es");

        Assert.Null(context.Store.SavedLanguage);
        Assert.Equal(0, context.Restart.RequestCount);
    }

    [Fact]
    public void ConfirmedChangePersistsLanguageAndRestarts()
    {
        var context = CreateContext("en");

        context.ViewModel.ChangeLanguageCommand.Execute("es");

        Assert.Equal("Español", context.Confirmation.LanguageName);
        Assert.Equal("es", context.Store.SavedLanguage);
        Assert.Equal(1, context.Restart.RequestCount);
    }

    [Fact]
    public void SaveFailureDoesNotRestart()
    {
        var context = CreateContext("en");
        context.Store.SaveResult = false;

        context.ViewModel.ChangeLanguageCommand.Execute("es");

        Assert.Equal(0, context.Restart.RequestCount);
        Assert.Equal("The language preference could not be saved.", context.ViewModel.StatusMessage);
    }

    [Fact]
    public void RestartFailureLeavesActionableStatus()
    {
        var context = CreateContext("en");
        context.Restart.Result = false;

        context.ViewModel.ChangeLanguageCommand.Execute("es");

        Assert.Equal("es", context.Store.SavedLanguage);
        Assert.Equal(
            "The language was saved, but Unskip could not restart. Reopen it to apply the change.",
            context.ViewModel.StatusMessage);
    }

    private static TestContext CreateContext(string language)
    {
        var store = new StubLanguagePreferenceStore();
        var confirmation = new StubLanguageChangeConfirmation();
        var restart = new StubApplicationRestart();
        var viewModel = new LanguageSettingsViewModel(
            language,
            store,
            confirmation,
            restart);
        return new TestContext(viewModel, store, confirmation, restart);
    }

    private sealed record TestContext(
        LanguageSettingsViewModel ViewModel,
        StubLanguagePreferenceStore Store,
        StubLanguageChangeConfirmation Confirmation,
        StubApplicationRestart Restart);

    private sealed class StubLanguagePreferenceStore : ILanguagePreferenceStore
    {
        public bool SaveResult { get; set; } = true;

        public string? SavedLanguage { get; private set; }

        public string? Load() => SavedLanguage;

        public bool TrySave(string language)
        {
            SavedLanguage = language;
            return SaveResult;
        }
    }

    private sealed class StubLanguageChangeConfirmation : ILanguageChangeConfirmation
    {
        public bool Result { get; set; } = true;

        public string? LanguageName { get; private set; }

        public bool Confirm(string languageName)
        {
            LanguageName = languageName;
            return Result;
        }
    }

    private sealed class StubApplicationRestart : IApplicationRestart
    {
        public bool Result { get; set; } = true;

        public int RequestCount { get; private set; }

        public bool TryRestart()
        {
            RequestCount++;
            return Result;
        }
    }
}
