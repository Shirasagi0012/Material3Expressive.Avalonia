using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Components;

// Expander header helpers.
internal static class ExpanderHeader
{
    // The owning Expander's ExpandDirection, forwarded onto the header ToggleButton so the header
    // theme can state the chevron's resting angles without reaching into the Expander.
    public static readonly AttachedProperty<ExpandDirection> DirectionProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ExpandDirection>("Direction", typeof(ExpanderHeader));

    public static ExpandDirection GetDirection(ToggleButton header) => header.GetValue(DirectionProperty);
    public static void SetDirection(ToggleButton header, ExpandDirection value) => header.SetValue(DirectionProperty, value);

    // [color, opacity] -> brush: List's disabled label/trailing-icon color is OnSurface at the
    // disabled content opacity.
    public static IMultiValueConverter Alpha { get; } = new FuncMultiValueConverter<object?, IBrush?>(values =>
        values.ToArray() is [Color color, double opacity]
            ? new ImmutableSolidColorBrush(Color.FromArgb((byte)Math.Round(opacity * 255, MidpointRounding.AwayFromZero), color.R, color.G, color.B))
            : null);
}
