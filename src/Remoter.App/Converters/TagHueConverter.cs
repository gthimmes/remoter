using System.Globalization;
using System.Windows.Data;
using Remoter.Core.Models;

namespace Remoter.App.Converters;

/// <summary>
/// Turns a tag into its hue index. It deliberately returns an index rather than a brush: a brush
/// handed back from a converter is resolved once and would not repaint on a theme swap, so the
/// template maps the index to a token itself.
/// </summary>
public sealed class TagHueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        TagHue.IndexFor(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
