using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace ACS.UI.Converters;

/// <summary>
/// MultiBinding 용 boolean AND 연산. 모든 입력이 true 일 때만 true.
/// 권한 게이트(UserSession.CanEdit)와 상태 기반 IsEnabled(CanStart 등) 를 AND 결합할 때 사용.
/// null/non-bool 은 false 취급 → 안전 기본값.
/// </summary>
public class BoolAndConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return false;
        return values.All(v => v is bool b && b);
    }
}
