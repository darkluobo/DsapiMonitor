using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DsapiMonitor.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TabColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = value is true;
        return new SolidColorBrush(isActive ? Color.FromRgb(0x3B, 0x82, 0xF6) : Color.FromRgb(0x94, 0xA3, 0xB8));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TabBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = value is true;
        return new SolidColorBrush(isActive ? Color.FromArgb(0x20, 0x3B, 0x82, 0xF6) : Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
