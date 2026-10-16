using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;

namespace Material3Expressive.Components;

public class ButtonGroupAction : ButtonGroupItem
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ButtonGroupAction, string?>(nameof(Label));
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<ButtonGroupAction, object?>(nameof(Icon), validate: icon => icon is not Control);
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty =
        AvaloniaProperty.Register<ButtonGroupAction, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<ButtonGroupAction, ICommand?>(nameof(Command));
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ButtonGroupAction, object?>(nameof(CommandParameter));
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<ButtonGroupAction, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private static readonly FuncValueConverter<object?, object?> CanExecuteConverter =
        new(value => value is ICommand command ? new CanExecuteCommand(command) : null);

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IDataTemplate? IconTemplate
    {
        get => GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    internal override double ResolvedCompressionLimit => CompressionLimit;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (ActionButton is { } previousButton)
        {
            previousButton.Click -= OnButtonClick;
            previousButton.ClearValue(Button.CommandProperty);
            previousButton.ClearValue(Button.CommandParameterProperty);
        }

        base.OnApplyTemplate(e);
        ActionButton = e.NameScope.Find<Button>("PART_Action");
        if (ActionButton is { } button)
        {
            button.Bind(Button.CommandProperty, new Binding(nameof(Command)) { Source = this, Converter = CanExecuteConverter });
            button.Bind(Button.CommandParameterProperty, new Binding(nameof(CommandParameter)) { Source = this });
            button.Click += OnButtonClick;
        }

        ReconnectPress();
    }
    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Activate();
    }

    internal virtual void Activate()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent));
        if (Command is { } command && command.CanExecute(CommandParameter))
            command.Execute(CommandParameter);
    }

    internal override Control CreateMenuContent(ButtonGroupMenuState state)
    {
        var item = new DropdownMenuItem();
        item.Bind(ContentProperty, new Binding(nameof(Label)) { Source = this });
        item.Bind(DropdownMenuItem.LeadingIconProperty, new Binding(nameof(Icon)) { Source = this });
        item.Bind(DropdownMenuItem.LeadingIconTemplateProperty, new Binding(nameof(IconTemplate)) { Source = this });
        item.Bind(IsEnabledProperty, new Binding(nameof(IsEffectivelyEnabled)) { Source = this });
        item.Bind(Button.CommandProperty, new Binding(nameof(Command)) { Source = this, Converter = CanExecuteConverter });
        item.Bind(Button.CommandParameterProperty, new Binding(nameof(CommandParameter)) { Source = this });
        item.Click += (_, e) =>
        {
            e.Handled = true;
            Activate();
            state.Dismiss();
        };
        return item;
    }

    private sealed class CanExecuteCommand(ICommand command) : ICommand
    {
        public bool CanExecute(object? parameter) => command.CanExecute(parameter);
        public void Execute(object? parameter) { }
        public event EventHandler? CanExecuteChanged
        {
            add => command.CanExecuteChanged += value;
            remove => command.CanExecuteChanged -= value;
        }
    }
}
