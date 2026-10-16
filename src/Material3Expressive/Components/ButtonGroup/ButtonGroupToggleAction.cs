using Avalonia;

namespace Material3Expressive.Components;

/// <summary>The application owns IsChecked; both representations request its inverse.</summary>
public class ButtonGroupToggleAction : ButtonGroupAction
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ButtonGroupToggleAction, bool>(nameof(IsChecked));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public event EventHandler<IconCheckedChangeEventArgs>? CheckedChangeRequested;

    internal override void Activate()
    {
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.Activate();
    }
}
