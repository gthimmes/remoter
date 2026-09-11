using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Remoter.App.Converters;

/// <summary>
/// Shows a row's live-session dot. Bound as a MultiBinding over the row's Id and the tracker's
/// Version: the Version value itself is ignored, but including it makes every row re-ask when
/// a session opens or closes, without regenerating rows and springing collapsed groups open.
/// Hidden rather than collapsed, so the favourite star beside it never shifts.
/// </summary>
public sealed class LiveSessionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length > 0 && values[0] is Guid id && App.LiveSessions.IsLive(id)
            ? Visibility.Visible
            : Visibility.Hidden;

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
