using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace Material3Expressive.Foundation.Typography;

/// <summary>Explicit first/last text baselines for custom content consumed by Material layouts.</summary>
/// <remarks>Offsets are DIP from the control's top edge, excluding its margin. They are not
/// inherited. A first baseline without a last baseline describes single-line content.</remarks>
public static class MaterialTextBaseline
{
    public static readonly AttachedProperty<double?> FirstProperty =
        AvaloniaProperty.RegisterAttached<Control, double?>("First", typeof(MaterialTextBaseline),
            validate: value => value is null || double.IsFinite(value.Value));
    public static readonly AttachedProperty<double?> LastProperty =
        AvaloniaProperty.RegisterAttached<Control, double?>("Last", typeof(MaterialTextBaseline),
            validate: value => value is null || double.IsFinite(value.Value));

    public static double? GetFirst(Control control) => control.GetValue(FirstProperty);
    public static void SetFirst(Control control, double? value) => control.SetValue(FirstProperty, value);
    public static double? GetLast(Control control) => control.GetValue(LastProperty);
    public static void SetLast(Control control, double? value) => control.SetValue(LastProperty, value);

    static MaterialTextBaseline()
    {
        FirstProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateConsumers(control));
        LastProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateConsumers(control));
    }

    private static void InvalidateConsumers(Control control)
    {
        // A baseline can change without changing DesiredSize. Invalidate the consuming layout,
        // including intervening presenters, rather than waiting for a size-change notification.
        control.InvalidateMeasure();
        foreach (var ancestor in control.GetVisualAncestors().OfType<Control>()) ancestor.InvalidateMeasure();
    }

    internal static (int First, int Last) Read(Control? control, double scale)
    {
        if (control is null) return (int.MinValue, int.MinValue);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var first = GetFirst(control);
        var last = GetLast(control);
        // Keep the existing native single-baseline entry usable for custom tooltip content.
        if (first is null && control.IsSet(TextBlock.BaselineOffsetProperty))
            first = TextBlock.GetBaselineOffset(control);
        if (first is { } explicitFirst)
            return (Px(control.Margin.Top + explicitFirst), Px(control.Margin.Top + (last ?? explicitFirst)));

        var result = (First: int.MinValue, Last: int.MinValue);
        if (control is TextBlock text && text.TextLayout.TextLines is { Count: > 0 } lines)
        {
            var y = text.Margin.Top + text.Padding.Top;
            result = (Px(y + lines[0].Baseline), Px(y + lines.Take(lines.Count - 1).Sum(line => line.Height) + lines[^1].Baseline));
        }
        else if (control is ContentPresenter { Child: { } child } presenter)
        {
            result = Read(child, scale);
            var y = Px(presenter.Margin.Top + presenter.Padding.Top);
            if (result.First != int.MinValue) result.First += y;
            if (result.Last != int.MinValue) result.Last += y;
        }
        if (last is { } explicitLast) result.Last = Px(control.Margin.Top + explicitLast);
        return result;
    }
}
