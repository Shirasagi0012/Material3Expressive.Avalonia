using Avalonia;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// View-declared policy for how one styled property moves. Mutable playback state is not stored
/// here so a declaration can be shared across controls.
/// </summary>
public abstract class AnimatedProperty : AvaloniaObject
{
    public static readonly DirectProperty<AnimatedProperty, AvaloniaProperty?> PropertyProperty =
        AvaloniaProperty.RegisterDirect<AnimatedProperty, AvaloniaProperty?>(
            nameof(Property),
            o => o._property,
            (o, v) => o._property = v);

    private AvaloniaProperty? _property;

    /// <summary>
    /// The host property to animate. In XAML assign it with
    /// <c>Property="{x:Static Visual.OpacityProperty}"</c>; a bare name is not resolved.
    /// </summary>
    public AvaloniaProperty? Property
    {
        get => _property;
        set => SetAndRaise(PropertyProperty, ref _property, value);
    }

    internal abstract IPropertySession CreateSession(Visual host);
}

/// <summary>
/// Typed declaration. <see cref="Spec"/> is read when a motion starts; changing it does not
/// interrupt a run already in flight.
/// </summary>
public abstract class AnimatedProperty<T> : AnimatedProperty
{
    private readonly SpringSpec<T> _defaultSpec;

    protected AnimatedProperty(ITwoWayConverter<T> converter, T? visibilityThreshold = default)
    {
        ArgumentNullException.ThrowIfNull(converter);
        Converter = converter;
        _defaultSpec = new SpringSpec<T>(visibilityThreshold: visibilityThreshold);
    }

    /// <summary>
    /// Spec used the next time the underlying value changes. Changing it does not interrupt
    /// a run that is already in flight.
    /// </summary>
    public IFiniteAnimationSpec<T>? Spec { get; set; }

    internal ITwoWayConverter<T> Converter { get; }

    internal IFiniteAnimationSpec<T> ResolveSpec() => Spec ?? _defaultSpec;

    internal override IPropertySession CreateSession(Visual host)
        => new AnimatedPropertySession<T>(host, this);
}

public sealed class AnimatedDouble() : AnimatedProperty<double>(new DoubleConverter(), VisibilityThresholds.Double);

public sealed class AnimatedFloat() : AnimatedProperty<float>(new FloatConverter(), VisibilityThresholds.Float);

public sealed class AnimatedInt32() : AnimatedProperty<int>(new Int32Converter(), VisibilityThresholds.Int32);

public sealed class AnimatedPoint() : AnimatedProperty<Point>(new PointConverter(), VisibilityThresholds.Point);

public sealed class AnimatedSize() : AnimatedProperty<Size>(new SizeConverter(), VisibilityThresholds.Size);

public sealed class AnimatedVector() : AnimatedProperty<Vector>(new VectorConverter(), VisibilityThresholds.Vector);

public sealed class AnimatedRect() : AnimatedProperty<Rect>(new RectConverter(), VisibilityThresholds.Rect);

public sealed class AnimatedThickness() : AnimatedProperty<Thickness>(new ThicknessConverter(), VisibilityThresholds.Thickness);

public sealed class AnimatedCornerRadius() : AnimatedProperty<CornerRadius>(new CornerRadiusConverter(), VisibilityThresholds.CornerRadius);

public sealed class AnimatedColor() : AnimatedProperty<Color>(new ColorConverter());
