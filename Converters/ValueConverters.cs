using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OmniScribe.Converters;

/// <summary>
/// Converts a bool to opacity (true=1.0, false=0.3).
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.3;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts audio level (0-1) and parent width to pixel width for the level meter.
/// </summary>
public class LevelToWidthConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 &&
            values[0] is float level &&
            values[1] is double parentWidth)
        {
            return Math.Max(0, Math.Min(parentWidth, level * parentWidth));
        }
        return 0.0;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}
