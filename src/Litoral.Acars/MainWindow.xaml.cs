using System.ComponentModel;
using System.IO;
using System.Windows;
using Litoral.Acars.Core.Map;
using Litoral.Acars.Map;
using Litoral.Acars.Ui;
using Microsoft.Web.WebView2.Core;

namespace Litoral.Acars;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private bool _mapStarted;
    private bool _mapReady;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        FitToScreen();
        StateChanged += (_, _) => UpdateMaximized();
        _vm.PageChanged += async page =>
        {
            if (page == "LiveMap") await StartMapAsync();
        };
        _vm.LiveMap.MessageReady += PostToMap;
    }

    private void FitToScreen()
    {
        var area = SystemParameters.WorkArea;
        if (Height > area.Height - 20) Height = area.Height - 20;
        if (Width > area.Width - 20) Width = area.Width - 20;
    }

    private void UpdateMaximized()
    {
        Root.Padding = WindowState == WindowState.Maximized ? new Thickness(8) : new Thickness(0);
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }

    private async Task StartMapAsync()
    {
        if (_mapStarted) return;
        _mapStarted = true;

        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Litoral ACARS");
            var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(root, "WebView2"));
            await MapView.EnsureCoreWebView2Async(environment);

            var mapFolder = Path.Combine(root, "Map");
            Directory.CreateDirectory(mapFolder);
            await File.WriteAllTextAsync(Path.Combine(mapFolder, "index.html"), LiveMapPage.Build(MapSettings.CartoApiKey));
            MapView.CoreWebView2.SetVirtualHostNameToFolderMapping(MapTiles.Host, mapFolder, CoreWebView2HostResourceAccessKind.Deny);

            MapView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            MapView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            MapView.CoreWebView2.NavigationCompleted += (_, _) =>
            {
                _mapReady = true;
                PostToMap(LiveMapMessages.Theme(_vm.IsDark));
                if (_vm.LiveMap.LastFlightsMessage is { } flights) PostToMap(flights);
            };
            MapView.CoreWebView2.Navigate($"https://{MapTiles.Host}/index.html");
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or InvalidOperationException or IOException)
        {
            _mapStarted = false;
            MessageBox.Show("The live map needs the Microsoft Edge WebView2 Runtime. Install it from microsoft.com and open the ACARS again.",
                "Litoral ACARS", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void PostToMap(string json)
    {
        if (_mapReady && MapView.CoreWebView2 is { } web) web.PostWebMessageAsJson(json);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettingsCommand.Execute(KeyBox.Password);
        KeyBox.Clear();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_vm.FlightInProgress)
        {
            var answer = MessageBox.Show(
                "A flight is in progress. If you close the ACARS, the rest of the flight will not be recorded.\n\nClose anyway?",
                "Litoral ACARS", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        _vm.Dispose();
        MapView.Dispose();
        base.OnClosing(e);
    }
}