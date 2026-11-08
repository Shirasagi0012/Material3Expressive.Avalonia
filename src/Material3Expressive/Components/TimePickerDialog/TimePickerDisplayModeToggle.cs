using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>A clock/input mode request button with the upstream tooltip. DisplayMode is application-owned.</summary>
public class TimePickerDisplayModeToggle : TemplatedControl
{
    public static readonly StyledProperty<TimePickerDisplayMode> DisplayModeProperty =
        TimePickerDialogTitle.DisplayModeProperty.AddOwner<TimePickerDisplayModeToggle>();
    public static readonly StyledProperty<CultureInfo?> CultureProperty =
        TimePickerDialogTitle.CultureProperty.AddOwner<TimePickerDisplayModeToggle>();
    public static readonly StyledProperty<ICommand?> CommandProperty = Button.CommandProperty.AddOwner<TimePickerDisplayModeToggle>();
    public static readonly StyledProperty<object?> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<TimePickerDisplayModeToggle>();
    // The actual IconButton raises this same bubbling event, preserving native ordering and command semantics.
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = Button.ClickEvent;
    internal static readonly DirectProperty<TimePickerDisplayModeToggle, string> DescriptionProperty =
        AvaloniaProperty.RegisterDirect<TimePickerDisplayModeToggle, string>(nameof(Description), x => x.Description);
    internal static readonly DirectProperty<TimePickerDisplayModeToggle, Geometry> IconGeometryProperty =
        AvaloniaProperty.RegisterDirect<TimePickerDisplayModeToggle, Geometry>(nameof(IconGeometry), x => x.IconGeometry);
    private string _description = "";
    private Geometry _iconGeometry = TimePickerModeIcons.Keyboard;
    internal string Description => _description;
    internal Geometry IconGeometry => _iconGeometry;
    protected virtual bool IsScrollToggle => false;
    public TimePickerDisplayMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }
    public CultureInfo? Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }
    public ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event EventHandler<RoutedEventArgs>? Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }
    public TimePickerDisplayModeToggle() => UpdateMode();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayModeProperty || change.Property == CultureProperty) UpdateMode();
    }
    private void UpdateMode()
    {
        var keyboard = DisplayMode == (IsScrollToggle ? TimePickerDisplayMode.Scroll : TimePickerDisplayMode.Picker);
        SetAndRaise(DescriptionProperty, ref _description, DateTimeStrings.Get(keyboard ? "TimePickerToggleKeyboard"
            : IsScrollToggle ? "TimePickerToggleScroll" : "TimePickerToggleTouch", Culture));
        SetAndRaise(IconGeometryProperty, ref _iconGeometry, keyboard ? TimePickerModeIcons.Keyboard
            : IsScrollToggle ? TimePickerModeIcons.SwipeVertical : TimePickerModeIcons.Schedule);
    }
}

/// <summary>The scroll/input variant of the same controlled mode request.</summary>
public class TimeScrollDisplayModeToggle : TimePickerDisplayModeToggle
{
    protected override Type StyleKeyOverride => typeof(TimePickerDisplayModeToggle);
    protected override bool IsScrollToggle => true;
    static TimeScrollDisplayModeToggle() => DisplayModeProperty.OverrideDefaultValue<TimeScrollDisplayModeToggle>(TimePickerDisplayMode.Scroll);
}
