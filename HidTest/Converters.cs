using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}
