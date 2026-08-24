using System.Collections;
using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Localization;

namespace personal_website_blazor;

// Manual IStringLocalizer<SharedResources> implementation.
//
// Microsoft.Extensions.Localization's convention-based factory derives the resource
// base name from Assembly.GetName().Name ("personal-website-blazor"), but MSBuild embeds
// .resx manifest resources using RootNamespace ("personal_website_blazor") since the two
// differ for this project. That mismatch makes the convention-based lookup silently miss
// every key, so the exact known-good manifest base name is used directly instead.
public sealed class SharedStringLocalizer : IStringLocalizer<SharedResources>
{
    private const string BaseName = "personal_website_blazor.Resources.SharedResources";
    private readonly ResourceManager _resourceManager = new(BaseName, typeof(SharedResources).Assembly);

    public LocalizedString this[string name]
    {
        get
        {
            var value = _resourceManager.GetString(name, CultureInfo.CurrentUICulture);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = _resourceManager.GetString(name, CultureInfo.CurrentUICulture);
            var value = format is null ? name : string.Format(CultureInfo.CurrentUICulture, format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format is null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var resourceSet = _resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, includeParentCultures);
        if (resourceSet is null)
        {
            yield break;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            yield return new LocalizedString((string)entry.Key, entry.Value?.ToString() ?? string.Empty);
        }
    }
}
