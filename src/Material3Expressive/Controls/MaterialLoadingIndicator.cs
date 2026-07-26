using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialShapes;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 Expressive loading indicator: a shape that morphs through the Material catalog
/// while it turns.
/// </summary>
/// <remarks>
/// The one component in the library that genuinely morphs polygons. Button, FAB and chip press
/// feedback interpolates corner sizes, and the wavy progress indicator samples a sine — none of
/// them need <c>graphics-shapes</c>. This does, which is what brings that port into the build.
/// <para>
/// Upstream cycles a fixed list of seven shapes for the indeterminate form and a shorter list for
/// the determinate one, morphing between consecutive pairs and rotating throughout. The lists are
/// <c>LoadingIndicatorDefaults.IndeterminateIndicatorPolygons</c> and
/// <c>DeterminateIndicatorPolygons</c>, transcribed below.
/// </para>
/// </remarks>
public class MaterialLoadingIndicator : MaterialControl
{
    /// <inheritdoc cref="Variant" />
    public static readonly StyledProperty<LoadingIndicatorVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialLoadingIndicator, LoadingIndicatorVariant>(nameof(Variant));

    /// <inheritdoc cref="Value" />
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MaterialLoadingIndicator, double>(nameof(Value));

    /// <inheritdoc cref="IsIndeterminate" />
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MaterialLoadingIndicator, bool>(nameof(IsIndeterminate), true);

    /// <summary>One full pass through the shape list.</summary>
    private static readonly TimeSpan MorphPeriod = TimeSpan.FromSeconds(4.5);

    /// <summary>One full turn.</summary>
    private static readonly TimeSpan RotationPeriod = TimeSpan.FromSeconds(3);

    /// <summary>Transcribed from <c>LoadingIndicatorDefaults.IndeterminateIndicatorPolygons</c>.</summary>
    private static readonly RoundedPolygon[] IndeterminateShapes =
    [
        MaterialShapeCatalog.SoftBurst,
        MaterialShapeCatalog.Cookie9Sided,
        MaterialShapeCatalog.Pentagon,
        MaterialShapeCatalog.Pill,
        MaterialShapeCatalog.Sunny,
        MaterialShapeCatalog.Cookie4Sided,
        MaterialShapeCatalog.Oval
    ];

    /// <summary>Transcribed from <c>LoadingIndicatorDefaults.DeterminateIndicatorPolygons</c>.</summary>
    private static readonly RoundedPolygon[] DeterminateShapes =
    [
        MaterialShapeCatalog.Circle,
        MaterialShapeCatalog.SoftBurst
    ];

    private static readonly Dictionary<(int List, int Index), Morph> MorphCache = new();

    private LoopingAnimation? _morph;
    private LoopingAnimation? _rotation;
    private SpringAnimation? _progress;

    private LoadingIndicatorTokens _tokens = LoadingIndicatorTokens.Standard;
    private LoadingIndicatorColors _colors = LoadingIndicatorTokens.Standard.Resolve(null, false);

    static MaterialLoadingIndicator()
    {
        AffectsRender<MaterialLoadingIndicator>(VariantProperty, IsIndeterminateProperty);
    }

    /// <summary>Whether the shape sits on a container of its own.</summary>
    public LoadingIndicatorVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Progress from 0 to 1, used only when <see cref="IsIndeterminate" /> is cleared.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Cycles the shapes on a timer rather than following <see cref="Value" />.</summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    private LoopingAnimation MorphPhase => _morph ??= Motion.Add(new LoopingAnimation(MorphPeriod));

    private LoopingAnimation Rotation => _rotation ??= Motion.Add(new LoopingAnimation(RotationPeriod));

    private SpringAnimation Progress =>
        _progress ??= Motion.AddSpring(Clamp(Value), Spring(MotionRole.DefaultSpatial));

    public override void Render(DrawingContext context)
    {
        var box = Math.Min(Bounds.Width, Bounds.Height);
        if (box <= 0)
            return;

        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);

        if (_tokens.HasContainer)
        {
            var containerSize = Math.Min(box, _tokens.ContainerSize);
            var corners = ResolvedTheme.Shapes.GetCornerRadius(
                _tokens.ContainerShape, new Size(containerSize, containerSize));

            using (context.PushTransform(Matrix.CreateTranslation(
                       centre.X - containerSize / 2, centre.Y - containerSize / 2)))
            {
                MaterialContainer.Draw(context, new Size(containerSize, containerSize), corners, _colors.ContainerColor);
            }
        }

        var shapes = IsIndeterminate ? IndeterminateShapes : DeterminateShapes;
        var (morph, progress) = CurrentMorph(shapes);

        // The catalog's shapes are normalised into a 1×1 box, so the geometry scales straight up to
        // the active size.
        var active = Math.Min(box, _tokens.ActiveSize);
        var geometry = morph.ToGeometry(progress);

        var transform =
            Matrix.CreateScale(active, active)
            * Matrix.CreateTranslation(-active / 2, -active / 2)
            * Matrix.CreateRotation(Rotation.Value * Math.PI * 2)
            * Matrix.CreateTranslation(centre.X, centre.Y);

        using (context.PushTransform(transform))
            context.DrawGeometry(_colors.ActiveIndicatorColor, null, geometry);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = _tokens.HasContainer ? _tokens.ContainerSize : _tokens.ActiveSize;
        return new Size(size, size);
    }

    protected override void OnThemeChanged(ResolvedTheme theme, bool isDark)
    {
        _tokens = theme.GetLoadingIndicatorTokens(Variant);
        _colors = theme.GetLoadingIndicatorColors(Variant, isDark);
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateLoops();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
        {
            OnThemeChanged(ResolvedTheme, IsDarkVariant);
        }
        else if (change.Property == ValueProperty)
        {
            Progress.AnimateTo(Clamp(Value), Spring(MotionRole.DefaultSpatial));
            Motion.Start();
        }
        else if (change.Property == IsIndeterminateProperty)
        {
            UpdateLoops();
        }
    }

    /// <summary>
    /// Which pair of shapes to morph between, and how far through that pair we are.
    /// </summary>
    /// <remarks>
    /// Morphs are cached because building one matches every feature of both polygons — real work
    /// to redo sixty times a second for a fixed list of shapes.
    /// </remarks>
    private (Morph Morph, double Progress) CurrentMorph(RoundedPolygon[] shapes)
    {
        var phase = IsIndeterminate ? MorphPhase.Value : Progress.Value;
        var scaled = Math.Clamp(phase, 0, 0.999999) * shapes.Length;
        var index = (int)scaled;

        var listKey = IsIndeterminate ? 0 : 1;
        if (!MorphCache.TryGetValue((listKey, index), out var morph))
        {
            morph = new Morph(shapes[index], shapes[(index + 1) % shapes.Length]);
            MorphCache[(listKey, index)] = morph;
        }

        return (morph, scaled - index);
    }

    private void UpdateLoops()
    {
        if (IsIndeterminate)
        {
            MorphPhase.Resume();
            Rotation.Resume();
        }
        else
        {
            MorphPhase.Settle();
            Rotation.Settle();
        }

        Motion.Start();
        InvalidateVisual();
    }

    private static double Clamp(double value)
    {
        return double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
    }
}
