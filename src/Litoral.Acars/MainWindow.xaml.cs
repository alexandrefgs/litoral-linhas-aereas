using System.ComponentModel;
using System.Windows;
using Litoral.Acars.Ui;

namespace Litoral.Acars;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettingsCommand.Execute(KeyBox.Password);
        KeyBox.Clear();
    }

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
        base.OnClosing(e);
    }
}