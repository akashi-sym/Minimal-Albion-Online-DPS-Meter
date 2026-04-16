using AlbionDpsMeter.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AlbionDpsMeter.Converters;

public class PercentageToWidthConverter : IValueConverter
{
    // Converts a percentage (0-100) to a width value for progress bars
    // The max width is bound by the container; we use a relative scale
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percentage)
        {
            // Clamp to a max pixel width for the bars (relative to ~350px container)
            return Math.Max(0, Math.Min(350, percentage * 3.5));
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        if (value is string s)
            return !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class NumberToShortStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long l) return l.ToShortNumberString();
        if (value is double d) return d.ToShortNumberString();
        if (value is int i) return i.ToShortNumberString();
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
