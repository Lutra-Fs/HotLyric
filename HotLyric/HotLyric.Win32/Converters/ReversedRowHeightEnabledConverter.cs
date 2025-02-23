﻿using System;
using System.Globalization;
using System.Windows;

namespace HotLyric.Win32.Converters
{
    public class ReversedRowHeightEnabledConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is false)
            {
                try
                {
                    return new GridLengthConverter().ConvertFrom(parameter) ?? new GridLength(0, GridUnitType.Star);
                }
                catch { }
            }

            return new GridLength(0, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
