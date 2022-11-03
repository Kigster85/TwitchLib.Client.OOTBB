using System.Globalization;

namespace TwitchBot.Converters
{
  public interface ICustomValueConverter
  {
    object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
  }

  public class MyCustomConverter : ICustomValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // Implement conversion logic here
      return value; // Placeholder
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // Implement conversion back logic here
      return value; // Placeholder
    }
  }


}
