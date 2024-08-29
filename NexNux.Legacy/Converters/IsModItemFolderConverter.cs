using System;
using System.Globalization;
using Avalonia.Data.Converters;
using NexNux.Legacy.Utilities;

namespace NexNux.Legacy.Converters;

public class IsModItemFolderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ModFolderItem;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}