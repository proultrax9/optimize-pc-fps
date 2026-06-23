using System.Globalization;
using System.Windows.Data;

namespace FpsGodPc.App.Converters;

public sealed class BoolToEnableLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool enabled && enabled ? "Disable" : "Enable";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
