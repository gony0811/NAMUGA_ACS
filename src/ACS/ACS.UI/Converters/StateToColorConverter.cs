using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ACS.UI.Converters;

public class StateToColorConverter : IValueConverter
{
    public static readonly StateToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var state = value as string;
        return state?.ToUpperInvariant() switch
        {
            "IDLE" => Brushes.DodgerBlue,
            "RUN" => Brushes.LimeGreen,
            "CHARGE" => Brushes.Gold,
            "DOWN" => Brushes.Red,
            "MANUAL" => Brushes.Orange,
            "EXITMAP" => Brushes.Gray,
            "OFF" => Brushes.DarkGray,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
