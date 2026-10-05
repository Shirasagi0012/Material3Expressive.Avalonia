using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

/// <summary>One complete immutable configuration of Card shadow heights in DIP.</summary>
public sealed record CardElevation
{
    public double Default { get; init; }
    public double Pressed { get; init; }
    public double Focused { get; init; }
    public double Hovered { get; init; }
    public double Dragged { get; init; }
    public double Disabled { get; init; }

    /// <summary>Combines bindings in Default, Pressed, Focused, Hovered, Dragged, Disabled order.</summary>
    public static IMultiValueConverter FromValues { get; } = new ElevationConverter();

    private sealed class ElevationConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [double rest, double pressed, double focused, double hovered, double dragged, double disabled]
                ? new CardElevation { Default = rest, Pressed = pressed, Focused = focused,
                    Hovered = hovered, Dragged = dragged, Disabled = disabled }
                : AvaloniaProperty.UnsetValue;
    }
}
