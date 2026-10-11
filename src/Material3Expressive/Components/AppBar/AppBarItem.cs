using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Material3Expressive.Components;

/// <summary>An item with separate app bar and overflow menu presentations.</summary>
public abstract class AppBarItem : ContentControl
{
    internal abstract Control CreateMenuContent(AppBarMenuState state);
}

/// <summary>An icon action in the bar and a text action in its overflow menu.</summary>
public class AppBarAction : AppBarItem
{
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<AppBarAction, string?>(nameof(Label));
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<AppBarAction, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<AppBarAction, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<AppBarAction, ICommand?>(nameof(Command));
    public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<AppBarAction, object?>(nameof(CommandParameter));
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<AppBarAction, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event EventHandler<RoutedEventArgs>? Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }
    private Button? _button;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_button is not null)
        {
            _button.Click -= BarClick;
            _button.ClearValue(Button.CommandProperty);
            _button.ClearValue(Button.CommandParameterProperty);
        }
        base.OnApplyTemplate(e);
        _button = e.NameScope.Find<Button>("PART_Action");
        if (_button is not null)
        {
            _button.Bind(Button.CommandProperty, new Binding(nameof(Command)) { Source = this, Converter = AppBarCanExecuteConverter.Instance });
            _button.Bind(Button.CommandParameterProperty, new Binding(nameof(CommandParameter)) { Source = this });
            _button.Click += BarClick;
        }
    }

    private void BarClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Activate();
    }

    internal virtual void Activate()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent));
        if (Command is { } command && command.CanExecute(CommandParameter)) command.Execute(CommandParameter);
    }

    internal override Control CreateMenuContent(AppBarMenuState state)
    {
        var item = new DropdownMenuItem();
        item.Bind(ContentProperty, new Binding(nameof(Label)) { Source = this });
        item.Bind(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = this });
        // Command remains on the actual Button so native CanExecute participates in enabled state.
        item.Bind(Button.CommandParameterProperty, new Binding(nameof(CommandParameter)) { Source = this });
        item.Click += (_, e) =>
        {
            e.Handled = true;
            Activate();
            state.Dismiss();
        };
        // The descriptor executes the command before dismissing, rather than Button's later
        // command phase. Keep only CanExecute on the menu Button via a forwarding command.
        item.Bind(Button.CommandProperty, new Binding(nameof(Command)) { Source = this, Converter = AppBarCanExecuteConverter.Instance });
        return item;
    }
}

/// <summary>A controlled icon toggle; the overflow representation is a plain text menu item.</summary>
public class AppBarToggleAction : AppBarAction
{
    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<AppBarToggleAction, bool>(nameof(IsChecked));
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public event EventHandler<IconCheckedChangeEventArgs>? CheckedChangeRequested;

    internal override void Activate()
    {
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.Activate();
    }
}

/// <summary>Custom app bar and menu content. Custom menu actions decide when to dismiss.</summary>
public class AppBarCustomItem : AppBarItem
{
    public static readonly StyledProperty<object?> MenuContentProperty = AvaloniaProperty.Register<AppBarCustomItem, object?>(nameof(MenuContent));
    public static readonly StyledProperty<IDataTemplate?> MenuContentTemplateProperty = AvaloniaProperty.Register<AppBarCustomItem, IDataTemplate?>(nameof(MenuContentTemplate));
    public object? MenuContent { get => GetValue(MenuContentProperty); set => SetValue(MenuContentProperty, value); }
    public IDataTemplate? MenuContentTemplate { get => GetValue(MenuContentTemplateProperty); set => SetValue(MenuContentTemplateProperty, value); }

    internal override Control CreateMenuContent(AppBarMenuState state)
    {
        var presenter = new ContentControl();
        presenter.Bind(ContentProperty, new Binding(nameof(MenuContent)) { Source = this });
        presenter.Bind(ContentTemplateProperty, new Binding(nameof(MenuContentTemplate)) { Source = this });
        presenter.Bind(DataContextProperty, new Binding(nameof(DataContext)) { Source = this });
        presenter.Bind(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = this });
        return presenter;
    }
}

internal sealed class AppBarCanExecuteConverter : Avalonia.Data.Converters.IValueConverter
{
    internal static readonly AppBarCanExecuteConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        value is ICommand command ? new CanExecuteCommand(command) : null;
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();

    private sealed class CanExecuteCommand(ICommand command) : ICommand
    {
        public bool CanExecute(object? parameter) => command.CanExecute(parameter);
        public void Execute(object? parameter) { }
        public event EventHandler? CanExecuteChanged { add => command.CanExecuteChanged += value; remove => command.CanExecuteChanged -= value; }
    }
}
