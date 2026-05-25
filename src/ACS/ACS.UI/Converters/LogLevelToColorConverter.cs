using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ACS.UI.Converters;

/// <summary>
/// 로그 레벨 문자열 → 텍스트 색상. ERROR/FATAL은 적색, WARN은 주황, INFO/FINE는 녹/기본, DEBUG는 흐림.
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    public static readonly LogLevelToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var level = (value as string)?.ToUpperInvariant();
        return level switch
        {
            "FATAL" => Brushes.Magenta,
            "ERROR" => Brushes.Tomato,
            "WARN" or "WARNING" => Brushes.Orange,
            "INFO" or "INFORMATION" => Brushes.LimeGreen,
            "FINE" or "WELL" => Brushes.MediumSeaGreen,
            "DEBUG" => Brushes.Gray,
            _ => Brushes.LightGray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
