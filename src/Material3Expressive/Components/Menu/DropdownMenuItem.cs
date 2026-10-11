/* Menu.kt: DropdownMenuItem, CheckableDropdownMenuItem, SelectableDropdownMenuItem.
 * Copyright 2022-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Material3Expressive.Components;

public sealed record MenuItemShapes(CornerRadius? Shape = null, CornerRadius? SelectedShape = null);
public sealed record MenuGroupShapes(CornerRadius? Shape = null, CornerRadius? InactiveShape = null)
{
    internal bool UsesMiddleShape { get; init; }
}

public static class MenuDefaults
{
    public const double GroupSpacing = 2;
    // Leading/trailing corners use reference ShapeTokens values in the fixed upstream.
    // Null keeps the standalone/middle and selected shapes bound to live System tokens.
    public static MenuItemShapes ItemShape(int index, int count) => new(count <= 1 ? null : index == 0
        ? new CornerRadius(12, 12, 4, 4) : index == count - 1 ? new CornerRadius(4, 4, 12, 12) : null);
    public static MenuGroupShapes GroupShape(int index, int count) => new(count <= 1 ? null : index == 0
        ? new CornerRadius(16, 16, 8, 8) : index == count - 1 ? new CornerRadius(8, 8, 16, 16) : null)
        { UsesMiddleShape = count > 1 && index != 0 && index != count - 1 };
}

/// <summary>A menu action. Content is the text slot; activation never closes or selects implicitly.</summary>
public class DropdownMenuItem : Button
{
    public static readonly StyledProperty<object?> LeadingIconProperty = AvaloniaProperty.Register<DropdownMenuItem, object?>(nameof(LeadingIcon));
    public static readonly StyledProperty<IDataTemplate?> LeadingIconTemplateProperty = AvaloniaProperty.Register<DropdownMenuItem, IDataTemplate?>(nameof(LeadingIconTemplate));
    public static readonly StyledProperty<object?> SelectedLeadingIconProperty = AvaloniaProperty.Register<DropdownMenuItem, object?>(nameof(SelectedLeadingIcon));
    public static readonly StyledProperty<IDataTemplate?> SelectedLeadingIconTemplateProperty = AvaloniaProperty.Register<DropdownMenuItem, IDataTemplate?>(nameof(SelectedLeadingIconTemplate));
    public static readonly StyledProperty<object?> TrailingContentProperty = AvaloniaProperty.Register<DropdownMenuItem, object?>(nameof(TrailingContent));
    public static readonly StyledProperty<IDataTemplate?> TrailingContentTemplateProperty = AvaloniaProperty.Register<DropdownMenuItem, IDataTemplate?>(nameof(TrailingContentTemplate));
    public static readonly StyledProperty<object?> SupportingTextProperty = AvaloniaProperty.Register<DropdownMenuItem, object?>(nameof(SupportingText));
    public static readonly StyledProperty<IDataTemplate?> SupportingTextTemplateProperty = AvaloniaProperty.Register<DropdownMenuItem, IDataTemplate?>(nameof(SupportingTextTemplate));
    public static readonly StyledProperty<MenuItemShapes?> ShapesProperty = AvaloniaProperty.Register<DropdownMenuItem, MenuItemShapes?>(nameof(Shapes));
    public static readonly StyledProperty<IBrush?> LeadingIconColorProperty = AvaloniaProperty.Register<DropdownMenuItem, IBrush?>(nameof(LeadingIconColor));
    public static readonly StyledProperty<IBrush?> TrailingContentColorProperty = AvaloniaProperty.Register<DropdownMenuItem, IBrush?>(nameof(TrailingContentColor));
    public static readonly StyledProperty<double> LeadingSpacingProperty = AvaloniaProperty.Register<DropdownMenuItem, double>(nameof(LeadingSpacing));
    public static readonly StyledProperty<double> TrailingSpacingProperty = AvaloniaProperty.Register<DropdownMenuItem, double>(nameof(TrailingSpacing));
    internal static readonly StyledProperty<bool> IsExpressiveProperty = AvaloniaProperty.Register<DropdownMenuItem, bool>(nameof(IsExpressive));
    internal static readonly StyledProperty<bool> SelectedProperty = AvaloniaProperty.Register<DropdownMenuItem, bool>(nameof(Selected));
    public object? LeadingIcon { get => GetValue(LeadingIconProperty); set => SetValue(LeadingIconProperty, value); }
    public IDataTemplate? LeadingIconTemplate { get => GetValue(LeadingIconTemplateProperty); set => SetValue(LeadingIconTemplateProperty, value); }
    public object? SelectedLeadingIcon { get => GetValue(SelectedLeadingIconProperty); set => SetValue(SelectedLeadingIconProperty, value); }
    public IDataTemplate? SelectedLeadingIconTemplate { get => GetValue(SelectedLeadingIconTemplateProperty); set => SetValue(SelectedLeadingIconTemplateProperty, value); }
    public object? TrailingContent { get => GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }
    public IDataTemplate? TrailingContentTemplate { get => GetValue(TrailingContentTemplateProperty); set => SetValue(TrailingContentTemplateProperty, value); }
    public object? SupportingText { get => GetValue(SupportingTextProperty); set => SetValue(SupportingTextProperty, value); }
    public IDataTemplate? SupportingTextTemplate { get => GetValue(SupportingTextTemplateProperty); set => SetValue(SupportingTextTemplateProperty, value); }
    public MenuItemShapes? Shapes { get => GetValue(ShapesProperty); set => SetValue(ShapesProperty, value); }
    public IBrush? LeadingIconColor { get => GetValue(LeadingIconColorProperty); set => SetValue(LeadingIconColorProperty, value); }
    public IBrush? TrailingContentColor { get => GetValue(TrailingContentColorProperty); set => SetValue(TrailingContentColorProperty, value); }
    public double LeadingSpacing { get => GetValue(LeadingSpacingProperty); set => SetValue(LeadingSpacingProperty, value); }
    public double TrailingSpacing { get => GetValue(TrailingSpacingProperty); set => SetValue(TrailingSpacingProperty, value); }
    internal bool IsExpressive { get => GetValue(IsExpressiveProperty); set => SetValue(IsExpressiveProperty, value); }
    internal bool Selected { get => GetValue(SelectedProperty); private set => SetValue(SelectedProperty, value); }
    internal bool HasLeading => LeadingIcon is not null || LeadingIconTemplate is not null;
    internal bool HasSelectedLeading => SelectedLeadingIcon is not null || SelectedLeadingIconTemplate is not null;
    internal bool HasTrailing => TrailingContent is not null || TrailingContentTemplate is not null;
    internal bool HasSupporting => SupportingText is not null || SupportingTextTemplate is not null;
    private DropdownMenuItemLayout? _layout;
    private bool _enterPressed;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _layout?.SetOwner(null);
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<DropdownMenuItemLayout>("PART_Layout");
        _layout?.SetOwner(this);
        Refresh();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LeadingIconProperty || change.Property == SelectedLeadingIconProperty
            || change.Property == TrailingContentProperty || change.Property == SupportingTextProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical child) LogicalChildren.Add(child);
        }
        if (change.Property == LeadingIconProperty || change.Property == LeadingIconTemplateProperty
            || change.Property == SelectedLeadingIconProperty || change.Property == SelectedLeadingIconTemplateProperty
            || change.Property == TrailingContentProperty || change.Property == TrailingContentTemplateProperty
            || change.Property == SupportingTextProperty || change.Property == SupportingTextTemplateProperty
            || change.Property == SelectedProperty || change.Property == IsExpressiveProperty
            || change.Property == LeadingSpacingProperty || change.Property == TrailingSpacingProperty) Refresh();
    }
    internal void RefreshSelection(bool selected) => Selected = selected;
    private void Refresh()
    {
        PseudoClasses.Set(":selected", Selected);
        PseudoClasses.Set(":supporting", HasSupporting);
        PseudoClasses.Set(":leading", HasLeading || HasSelectedLeading);
        PseudoClasses.Set(":trailing", HasTrailing);
        _layout?.Refresh();
    }

    // Compose clickable activates both Enter and Space on release. Native Button already
    // handles Space; intercept its Enter-on-down branch while retaining Click/Command.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ReferenceEquals(e.Source, this))
        { _enterPressed = true; e.Handled = true; }
        else base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ReferenceEquals(e.Source, this))
        {
            var activate = _enterPressed && IsEffectivelyEnabled;
            _enterPressed = false; e.Handled = true;
            if (activate) OnClick();
        }
        else base.OnKeyUp(e);
    }
    protected override void OnLostFocus(FocusChangedEventArgs e)
    { _enterPressed = false; base.OnLostFocus(e); }
}

/// <summary>Checkbox-role item. CheckedChangeRequested supplies !IsChecked; the application owns state.</summary>
public class CheckableDropdownMenuItem : DropdownMenuItem
{
    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<CheckableDropdownMenuItem, bool>(nameof(IsChecked));
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public event EventHandler<MenuCheckedChangeEventArgs>? CheckedChangeRequested;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    { base.OnPropertyChanged(change); if (change.Property == IsCheckedProperty) RefreshSelection(IsChecked); }
    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled) return;
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.OnClick();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer : ButtonAutomationPeer, IToggleProvider
    {
        public Peer(CheckableDropdownMenuItem owner) : base(owner)
        {
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsCheckedProperty)
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                        e.OldValue is true ? ToggleState.On : ToggleState.Off,
                        e.NewValue is true ? ToggleState.On : ToggleState.Off);
            };
        }
        public ToggleState ToggleState => ((CheckableDropdownMenuItem)Owner).IsChecked ? ToggleState.On : ToggleState.Off;
        public void Toggle() => ((IInvokeProvider)this).Invoke();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;
    }
}
public sealed class MenuCheckedChangeEventArgs(bool value) : EventArgs { public bool Value { get; } = value; }

/// <summary>Radio-role item; selection and mutual exclusion belong to the application.</summary>
public class SelectableDropdownMenuItem : DropdownMenuItem
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SelectableDropdownMenuItem, bool>(nameof(IsSelected));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    { base.OnPropertyChanged(change); if (change.Property == IsSelectedProperty) RefreshSelection(IsSelected); }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer : ButtonAutomationPeer, ISelectionItemProvider
    {
        public Peer(SelectableDropdownMenuItem owner) : base(owner)
        {
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsSelectedProperty)
                    RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, e.OldValue is true, e.NewValue is true);
            };
        }
        public bool IsSelected => ((SelectableDropdownMenuItem)Owner).IsSelected;
        public ISelectionProvider? SelectionContainer => null;
        public void AddToSelection() => Select();
        public void RemoveFromSelection() { }
        public void Select() => ((IInvokeProvider)this).Invoke();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;
    }
}
