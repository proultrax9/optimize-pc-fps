using System.Globalization;
using System.Windows.Data;

namespace FpsGodPc.App.Converters;

public sealed class CountFormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int count || values[1] is not string format || string.IsNullOrWhiteSpace(format))
        {
            return string.Empty;
        }

        return string.Format(culture, format, count);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
