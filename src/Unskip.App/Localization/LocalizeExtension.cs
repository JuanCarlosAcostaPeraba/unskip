using System.Windows.Markup;

namespace Unskip.App.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        UiText.Get(Key);
}
