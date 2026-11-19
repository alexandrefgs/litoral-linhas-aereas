using System.Globalization;
using System.Windows.Data;

namespace Litoral.Acars.Ui;

public sealed class NetworkLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        "Vatsim" => "VATSIM",
        "Ivao" => "IVAO",
        string text => text,
        _ => ""
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}