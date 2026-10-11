using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Material3Expressive.Components;

/// <summary>Ordered horizontal actions with a menu for the overflowing suffix.</summary>
public class AppBarRow : ItemsControl
{
    public static readonly StyledProperty<int> MaxItemCountProperty =
        AvaloniaProperty.Register<AppBarRow, int>(nameof(MaxItemCount), int.MaxValue);
    public static readonly StyledProperty<IDataTemplate?> OverflowIndicatorTemplateProperty =
        AvaloniaProperty.Register<AppBarRow, IDataTemplate?>(nameof(OverflowIndicatorTemplate));

    static AppBarRow()
    {
        TemplateProperty.OverrideDefaultValue<AppBarRow>(null);
        AffectsMeasure<AppBarRow>(MaxItemCountProperty, OverflowIndicatorTemplateProperty);
    }

    public int MaxItemCount { get => GetValue(MaxItemCountProperty); set => SetValue(MaxItemCountProperty, value); }
    public IDataTemplate? OverflowIndicatorTemplate { get => GetValue(OverflowIndicatorTemplateProperty); set => SetValue(OverflowIndicatorTemplateProperty, value); }
    public AppBarMenuState MenuState { get; } = new();
    internal AppBarOverflowLayout? Layout { get; private set; }
    internal bool IsVertical => this is AppBarColumn;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Layout?.Disconnect();
        base.OnApplyTemplate(e);
        Layout = e.NameScope.Find<AppBarOverflowLayout>("PART_Layout");
        Layout?.Connect(this);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new AppBarSlot();
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MaxItemCountProperty) Layout?.InvalidateItemsMeasure();
    }
}

/// <summary>Ordered vertical actions with a menu for the overflowing suffix.</summary>
public class AppBarColumn : AppBarRow
{
    protected override Type StyleKeyOverride => typeof(AppBarRow);
}

/// <summary>State owned by an app bar row or column and shared with its overflow indicator.</summary>
public sealed class AppBarMenuState : AvaloniaObject
{
    public static readonly DirectProperty<AppBarMenuState, bool> IsShowingProperty =
        AvaloniaProperty.RegisterDirect<AppBarMenuState, bool>(nameof(IsShowing), state => state.IsShowing);
    private bool _isShowing;
    public bool IsShowing => _isShowing;
    public void Show() => SetAndRaise(IsShowingProperty, ref _isShowing, true);
    public void Dismiss() => SetAndRaise(IsShowingProperty, ref _isShowing, false);
}

internal sealed class AppBarOverflowButton : IconButton
{
    protected override Type StyleKeyOverride => typeof(IconButton);
    protected override void OnClick()
    {
        if (DataContext is AppBarMenuState state)
        {
            if (state.IsShowing) state.Dismiss(); else state.Show();
        }
        base.OnClick();
    }
}

// The wrapper is hidden when unplaced, but its content remains measurable, just like an
// unplaced Compose measurable. Visibility belongs to the wrapper, never to caller content.
internal sealed class AppBarSlot : ContentPresenter
{
    internal Size MeasureContent(Size available)
    {
        ApplyTemplate();
        Child?.Measure(available);
        return Child?.DesiredSize ?? default;
    }
}
