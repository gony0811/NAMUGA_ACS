using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ACS.UI.Converters;

/// <summary>
/// Process/NIO 상태를 색상으로 변환
/// 매뉴얼 표 7.1.1.2 기반
/// </summary>
public class ProcessStateToColorConverter : IValueConverter
{
    public static readonly ProcessStateToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value as string;
        return state?.ToUpperInvariant() switch
        {
            // Control Server 상태
            "CS_ACTIVE" => Brushes.LimeGreen,
            "CS_INACTIVE" => Brushes.DarkGray,
            "CS_HANG" => Brushes.Red,
            "CS_STANDBY" => Brushes.Gold,

            // Process 상태
            "STATE_ACTIVE" => Brushes.Green,
            "STATE_INACTIVE" => Brushes.Gray,
            "STATE_HANG" => Brushes.Red,
            "STATE_STANDBY" => Brushes.Gold,

            // NIO 상태
            "NIO_CONNECTED" => Brushes.DodgerBlue,
            "NIO_DISCONNECTED" => Brushes.DarkGray,
            "NIO_CONNECTING" => Brushes.Orange,

            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
