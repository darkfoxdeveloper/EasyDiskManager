using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using EasyDiskManager.Models;

namespace EasyDiskManager.Converters;

public sealed class SegmentWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not long size || values[1] is not long diskSize ||
            values[2] is not double available || diskSize <= 0 || double.IsNaN(available)) return 100d;
        return Math.Max(82d, (available - 8) * size / diskSize);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SegmentBrushConverter : IValueConverter
{
    private static readonly Brush Primary = new SolidColorBrush(Color.FromRgb(68, 97, 242));
    private static readonly Brush Protected = new SolidColorBrush(Color.FromRgb(124, 105, 201));
    private static readonly Brush Unallocated = new SolidColorBrush(Color.FromRgb(215, 220, 230));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        PartitionInfo { IsProtected: true } => Protected,
        PartitionInfo => Primary,
        _ => Unallocated
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
