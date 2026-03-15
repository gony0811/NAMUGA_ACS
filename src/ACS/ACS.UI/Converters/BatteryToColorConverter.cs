using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ACS.UI.Converters;

public class BatteryToColorConverter : IValueConverter
{
    public static readonly BatteryToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rate)
        {
            if (rate >= 70) return Brushes.LimeGreen;
            if (rate >= 30) return Brushes.Gold;
            return Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
