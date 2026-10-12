using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3Expressive.Components;

// Hosts NumericUpDown's PART_Spinner inside the Material TextField's icon slots.
//
// Fluent nests PART_TextBox inside the ButtonSpinner, so the spinner's own Up/Down and wheel
// handling sees the editor's bubbling input. Here the spinner sits inside the TextBox (trailing
// or leading slot), so the editor's unhandled Up/Down and wheel input is forwarded to the spinner
// with the same AllowSpin / ValidSpinDirection rules ButtonSpinner applies.
internal static class NumericUpDownTextField
{
    public static readonly AttachedProperty<ButtonSpinner?> SpinnerProperty =
        AvaloniaProperty.RegisterAttached<TextBox, ButtonSpinner?>("Spinner", typeof(NumericUpDownTextField));
    public static ButtonSpinner? GetSpinner(TextBox owner) => owner.GetValue(SpinnerProperty);
    public static void SetSpinner(TextBox owner, ButtonSpinner? value) => owner.SetValue(SpinnerProperty, value);

    public static readonly AttachedProperty<Location> SpinnerLocationProperty =
        AvaloniaProperty.RegisterAttached<TextBox, Location>("SpinnerLocation", typeof(NumericUpDownTextField), Location.Right);
    public static Location GetSpinnerLocation(TextBox owner) => owner.GetValue(SpinnerLocationProperty);
    public static void SetSpinnerLocation(TextBox owner, Location value) => owner.SetValue(SpinnerLocationProperty, value);

    public static readonly AttachedProperty<bool> ShowSpinnerProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("ShowSpinner", typeof(NumericUpDownTextField), true);
    public static bool GetShowSpinner(TextBox owner) => owner.GetValue(ShowSpinnerProperty);
    public static void SetShowSpinner(TextBox owner, bool value) => owner.SetValue(ShowSpinnerProperty, value);

    // The owner's forwarded leading icon; it shares the leading slot with a left spinner.
    public static readonly AttachedProperty<object?> LeadingIconProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>("LeadingIcon", typeof(NumericUpDownTextField));
    public static object? GetLeadingIcon(TextBox owner) => owner.GetValue(LeadingIconProperty);
    public static void SetLeadingIcon(TextBox owner, object? value) => owner.SetValue(LeadingIconProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> LeadingIconTemplateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IDataTemplate?>("LeadingIconTemplate", typeof(NumericUpDownTextField));
    public static IDataTemplate? GetLeadingIconTemplate(TextBox owner) => owner.GetValue(LeadingIconTemplateProperty);
    public static void SetLeadingIconTemplate(TextBox owner, IDataTemplate? value) => owner.SetValue(LeadingIconTemplateProperty, value);

    static NumericUpDownTextField()
    {
        SpinnerProperty.Changed.AddClassHandler<TextBox>((box, e) =>
        {
            if (e.OldValue is ButtonSpinner && e.NewValue is null)
            {
                box.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
                box.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
            }
            else if (e.OldValue is null && e.NewValue is ButtonSpinner)
            {
                box.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
                box.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, handledEventsToo: true);
            }
            UpdateSlots(box);
        });
        SpinnerLocationProperty.Changed.AddClassHandler<TextBox>((box, _) => UpdateSlots(box));
        ShowSpinnerProperty.Changed.AddClassHandler<TextBox>((box, _) => UpdateSlots(box));
        LeadingIconProperty.Changed.AddClassHandler<TextBox>((box, _) => UpdateSlots(box));
        LeadingIconTemplateProperty.Changed.AddClassHandler<TextBox>((box, _) => UpdateSlots(box));
    }

    static void UpdateSlots(TextBox box)
    {
        var spinner = GetSpinner(box);
        var show = spinner is not null && GetShowSpinner(box);
        var left = show && GetSpinnerLocation(box) == Location.Left;
        object? leading = left ? spinner : GetLeadingIcon(box);
        var leadingTemplate = left ? null : GetLeadingIconTemplate(box);
        object? trailing = show && !left ? spinner : null;
        // Take the spinner out of its old slot before the other slot claims it.
        if (left)
        {
            Set(MaterialTextField.TrailingIconProperty, trailing);
            Set(MaterialTextField.LeadingIconTemplateProperty, leadingTemplate);
            Set(MaterialTextField.LeadingIconProperty, leading);
        }
        else
        {
            Set(MaterialTextField.LeadingIconProperty, leading);
            Set(MaterialTextField.LeadingIconTemplateProperty, leadingTemplate);
            Set(MaterialTextField.TrailingIconProperty, trailing);
        }

        // Template priority keeps these below application styles and local values on the part.
        void Set<T>(StyledProperty<T> property, T value) => box.SetValue(property, value, BindingPriority.Template);
    }

    static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || sender is not TextBox box || GetSpinner(box) is not { } spinner) return;
        SpinDirection? direction = e.Key switch { Key.Up => SpinDirection.Increase, Key.Down => SpinDirection.Decrease, _ => null };
        if (direction is { } value && spinner.AllowSpin)
        {
            Spin(spinner, value, false);
            e.Handled = true;
        }
    }

    static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // The editor's (smooth) ScrollViewer may mark vertical wheel input handled even though a
        // single-line NumericUpDown editor never scrolls vertically, so handled input is accepted.
        if (sender is not TextBox box || GetSpinner(box) is not { } spinner) return;
        if (spinner.AllowSpin && box.IsKeyboardFocusWithin && e.Delta.Y != 0)
        {
            Spin(spinner, e.Delta.Y < 0 ? SpinDirection.Decrease : SpinDirection.Increase, true);
            e.Handled = true;
        }
    }

    // Spinner.OnSpin: raise only for a currently valid direction.
    static void Spin(ButtonSpinner spinner, SpinDirection direction, bool usingMouseWheel)
    {
        var valid = direction == SpinDirection.Increase ? ValidSpinDirections.Increase : ValidSpinDirections.Decrease;
        if ((spinner.ValidSpinDirection & valid) == valid)
            spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, direction, usingMouseWheel));
    }
}
