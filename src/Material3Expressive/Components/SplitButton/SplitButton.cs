using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Components;

/// <summary>Compose SplitButtonLayout: two independent content slots with trailing width priority.</summary>
public sealed class SplitButton : TemplatedControl
{
    public static readonly StyledProperty<Control?> LeadingButtonProperty = AvaloniaProperty.Register<SplitButton, Control?>(nameof(LeadingButton));
    public static readonly StyledProperty<Control?> TrailingButtonProperty = AvaloniaProperty.Register<SplitButton, Control?>(nameof(TrailingButton));
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<SplitButton, double>(nameof(Spacing));
    public Control? LeadingButton { get => GetValue(LeadingButtonProperty); set => SetValue(LeadingButtonProperty, value); }
    public Control? TrailingButton { get => GetValue(TrailingButtonProperty); set => SetValue(TrailingButtonProperty, value); }
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LeadingButtonProperty || change.Property == TrailingButtonProperty)
        {
            if (change.OldValue is Control previous) LogicalChildren.Remove(previous);
            if (change.NewValue is Control next) LogicalChildren.Add(next);
        }
    }
}
