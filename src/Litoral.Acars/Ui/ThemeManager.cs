using System.Windows;

namespace Litoral.Acars.Ui;

public static class ThemeManager
{
    public static void Apply(bool dark)
    {
        if (Application.Current is not { } app) return;

        var theme = new ResourceDictionary
        {
            Source = new Uri(dark ? "pack://application:,,,/Themes/Dark.xaml" : "pack://application:,,,/Themes/Light.xaml", UriKind.Absolute)
        };

        var dictionaries = app.Resources.MergedDictionaries;
        if (dictionaries.Count > 0) dictionaries[0] = theme;
        else dictionaries.Add(theme);
    }
}