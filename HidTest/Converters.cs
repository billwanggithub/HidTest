using System;
using System.Globalization;
using System.Windows.Data;

namespace Converters
{
    public class NumberToHexString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value.GetType() == typeof(byte))
                {
                    return ((byte)value).ToString("X2");
                }
                else if (value.GetType() == typeof(int))
                {
                    return ((int)value).ToString("X4");
                }
                else if (value.GetType() == typeof(uint))
                {
                    return ((uint)value).ToString("X4");
                }
                else
                {
                    return "00";
                }
            }
            catch (Exception)
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var temp = uint.Parse(value.ToString()!.Replace("0x", ""), NumberStyles.HexNumber);
                return temp;

            }
            catch (Exception)
            {
                return value;
            }
        }
    }
    public class BoolInverterConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        #endregion
    }
}
