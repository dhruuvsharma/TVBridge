using System.Windows;

namespace TVBridge.App.Services;

public sealed class ThemeService
{
    private const string DarkThemeUri = "Themes/NeonDark.xaml";
    private const string LightThemeUri = "Themes/NeonLight.xaml";

    public bool IsDarkTheme { get; private set; } = true;

    public void ApplyTheme(bool dark)
    {
        IsDarkTheme = dark;
        var dict = Application.Current.Resources.MergedDictionaries;

        // Remove existing theme dictionary (always at index 0)
        if (dict.Count > 0 && dict[0].Source?.OriginalString.Contains("Neon") == true)
            dict.RemoveAt(0);

        var uri = new Uri(dark ? DarkThemeUri : LightThemeUri, UriKind.Relative);
        dict.Insert(0, new ResourceDictionary { Source = uri });
    }

    public void ToggleTheme()
    {
        ApplyTheme(!IsDarkTheme);
    }
}
