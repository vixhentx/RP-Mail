using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace RPMailUI.Converter;

public class DictionaryValueConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) => 
        values is [Dictionary<string, string> { Count: > 0 } dict, string key, ..]
            && dict.TryGetValue(string.IsNullOrEmpty(key) ? dict.Keys.First() : key,out var value) ?
            value : "No Property";
}