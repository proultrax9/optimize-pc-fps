using System.Globalization;
using System.Windows.Data;

namespace FpsGodPc.App.Converters;

public sealed class NullableFpsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float fps)
        {
            return fps.ToString("F1", culture);
        }

        if (value is double d)
        {
            return d.ToString("F1", culture);
        }

        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
