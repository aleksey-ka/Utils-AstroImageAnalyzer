using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AstroImageAnalyzer.UI.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;
        bool isNull = value == null;
        
        if (isInverse)
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
