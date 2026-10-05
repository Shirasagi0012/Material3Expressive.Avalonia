using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Attaches Compose-driven property motion to a <see cref="Visual"/>. Declarations in
/// <see cref="PropertiesProperty"/> describe how a styled property moves when its underlying
/// (non-animation) value changes. Intermediate samples are written at
/// <see cref="Avalonia.Data.BindingPriority.Animation"/>.
/// </summary>
/// <remarks>
/// Supports non-inherited styled properties with local/style destinations and default fallbacks.
/// Use OneWay presentation bindings; TwoWay bindings can write animation samples to their source.
/// Covered destination changes are coalesced and checked on animation frames.
/// </remarks>
public static class Animated
{
    /// <summary>
    /// Declarations applied to the visual. Shared instances (for example from a style setter)
    /// keep independent sessions per control.
    /// </summary>
    public static readonly AttachedProperty<AnimatedPropertyCollection?> PropertiesProperty =
        AvaloniaProperty.RegisterAttached<Visual, AnimatedPropertyCollection?>("Properties", typeof(Animated));

    /// <summary>Inherited duration multiplier for presentation playback; does not alter raw timestamps.</summary>
    public static readonly AttachedProperty<double> DurationScaleProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("DurationScale", typeof(Animated), 1, inherits: true,
            validate: value => double.IsFinite(value) && value >= 0);
    public static double GetDurationScale(Visual visual) => visual.GetValue(DurationScaleProperty);
    public static void SetDurationScale(Visual visual, double value) => visual.SetValue(DurationScaleProperty, value);

    /// <summary>Ends this property's current contribution immediately, exposing its current base value.</summary>
    public static Task SnapToTargetAsync<T>(Visual visual, StyledProperty<T> property)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(property);
        visual.VerifyAccess();
        AnimatedHostCoordinator.SnapToTarget(visual, property);
        return Task.CompletedTask;
    }

    static Animated()
    {
        PropertiesProperty.Changed.AddClassHandler<Visual>(OnPropertiesChanged);
    }

    public static AnimatedPropertyCollection? GetProperties(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return visual.GetValue(PropertiesProperty);
    }

    public static void SetProperties(Visual visual, AnimatedPropertyCollection? value)
    {
        ArgumentNullException.ThrowIfNull(visual);
        visual.SetValue(PropertiesProperty, value);
    }

    /// <summary>
    /// Returns the existing collection, or creates and attaches an empty one.
    /// </summary>
    public static AnimatedPropertyCollection GetOrCreateProperties(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        var list = GetProperties(visual);
        if (list is not null) return list;
        list = new AnimatedPropertyCollection();
        SetProperties(visual, list);
        return list;
    }

    /// <summary>
    /// Adds <paramref name="declaration"/> to the visual's collection, creating the collection
    /// if needed. The visual's ordinary property value remains the destination.
    /// </summary>
    public static TDeclaration Animate<TDeclaration>(this Visual visual, TDeclaration declaration)
        where TDeclaration : AnimatedProperty
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(declaration);
        GetOrCreateProperties(visual).Add(declaration);
        return declaration;
    }

    private static void OnPropertiesChanged(Visual visual, AvaloniaPropertyChangedEventArgs e)
    {
        var oldList = e.GetOldValue<AnimatedPropertyCollection?>();
        var newList = e.GetNewValue<AnimatedPropertyCollection?>();
        AnimatedHostCoordinator.Replace(visual, oldList, newList);
    }

}
