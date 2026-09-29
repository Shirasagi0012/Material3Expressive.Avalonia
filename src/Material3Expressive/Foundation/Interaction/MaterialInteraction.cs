using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation.Interaction;

/// <summary>
/// The opt-in switch that turns a templated control into an interactive Material component.
/// </summary>
/// <remarks>
/// Compose makes the split explicit: a Surface with <c>onClick = null</c> creates no
/// InteractionSource and draws no feedback, while the clickable overload hosts the ripple node.
/// Here the control's ControlTheme sets <c>MaterialInteraction.IsInteractive</c> on the owner; a
/// MaterialSurface at the template root then attaches to the owner's lazily created
/// <see cref="MaterialInteractionController" /> and paints ripples and timed state-layer fades.
/// Themes that leave the property unset keep the static styled state layer — and since the stock
/// Avalonia Button cannot grow a constructor, an attached property is also the only way to opt it in.
/// </remarks>
public static class MaterialInteraction
{
    /// <summary>Whether the control produces Material interaction feedback for its template.</summary>
    public static readonly AttachedProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsInteractive", typeof(MaterialInteraction));

    private static readonly ConditionalWeakTable<Control, MaterialInteractionController> Controllers = new();

    public static bool GetIsInteractive(Control control) => control.GetValue(IsInteractiveProperty);

    public static void SetIsInteractive(Control control, bool value) =>
        control.SetValue(IsInteractiveProperty, value);

    /// <summary>The owner's interaction controller, created on first use and shared for its lifetime.</summary>
    internal static MaterialInteractionController GetOrCreate(Control owner) =>
        Controllers.GetValue(owner, static o => new MaterialInteractionController(o));
}
