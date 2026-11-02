using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

// Keep the native template's fixed row/column structure while accepting live token values
// on a StyledElement. Updating the definitions also invalidates Grid's layout caches.
internal sealed class NativePickerPresenterGrid : Grid
{
    protected override Type StyleKeyOverride => typeof(Grid);

    public static readonly StyledProperty<double> ActionRowHeightProperty =
        AvaloniaProperty.Register<NativePickerPresenterGrid, double>(nameof(ActionRowHeight), 64);

    public double ActionRowHeight { get => GetValue(ActionRowHeightProperty); set => SetValue(ActionRowHeightProperty, value); }

    public NativePickerPresenterGrid() => RowDefinitions = new RowDefinitions("*,64");

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActionRowHeightProperty)
            RowDefinitions[1].Height = new GridLength(ActionRowHeight);
    }
}

internal sealed class NativeCalendarItemGrid : Grid
{
    protected override Type StyleKeyOverride => typeof(Grid);

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<NativeCalendarItemGrid, double>(nameof(HeaderHeight), 48);

    public double HeaderHeight { get => GetValue(HeaderHeightProperty); set => SetValue(HeaderHeightProperty, value); }

    public NativeCalendarItemGrid() => RowDefinitions = new RowDefinitions("48,*");

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HeaderHeightProperty)
            RowDefinitions[0].Height = new GridLength(HeaderHeight);
    }
}

internal sealed class NativeCalendarHeaderGrid : Grid
{
    protected override Type StyleKeyOverride => typeof(Grid);

    public static readonly StyledProperty<double> NavigationColumnWidthProperty =
        AvaloniaProperty.Register<NativeCalendarHeaderGrid, double>(nameof(NavigationColumnWidth), 48);

    public double NavigationColumnWidth { get => GetValue(NavigationColumnWidthProperty); set => SetValue(NavigationColumnWidthProperty, value); }

    public NativeCalendarHeaderGrid() => ColumnDefinitions = new ColumnDefinitions("48,*,48");

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NavigationColumnWidthProperty)
        {
            ColumnDefinitions[0].Width = new GridLength(NavigationColumnWidth);
            ColumnDefinitions[2].Width = new GridLength(NavigationColumnWidth);
        }
    }
}

internal sealed class NativeCalendarMonthGrid : Grid
{
    protected override Type StyleKeyOverride => typeof(Grid);

    public static readonly StyledProperty<double> WeekdayRowHeightProperty =
        AvaloniaProperty.Register<NativeCalendarMonthGrid, double>(nameof(WeekdayRowHeight), 40);
    public static readonly StyledProperty<double> DayRowHeightProperty =
        AvaloniaProperty.Register<NativeCalendarMonthGrid, double>(nameof(DayRowHeight), 48);

    public double WeekdayRowHeight { get => GetValue(WeekdayRowHeightProperty); set => SetValue(WeekdayRowHeightProperty, value); }
    public double DayRowHeight { get => GetValue(DayRowHeightProperty); set => SetValue(DayRowHeightProperty, value); }

    public NativeCalendarMonthGrid()
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*");
        RowDefinitions = new RowDefinitions("40,48,48,48,48,48,48");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WeekdayRowHeightProperty)
            RowDefinitions[0].Height = new GridLength(WeekdayRowHeight);
        else if (change.Property == DayRowHeightProperty)
        {
            for (var row = 1; row <= 6; row++)
                RowDefinitions[row].Height = new GridLength(DayRowHeight);
        }
    }
}
