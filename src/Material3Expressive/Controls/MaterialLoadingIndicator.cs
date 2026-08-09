using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Controls.Motion;
using MaterialShapes;

namespace Material3Expressive.Controls;

/// <summary>Whether a loading indicator owns a container behind its morphing shape.</summary>
public enum LoadingIndicatorVariant { Standard, Contained }

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
public class MaterialLoadingIndicator : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.Theme.Current.IsDark ?? false;
    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) =>
        ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    private void OnMotionTick() => InvalidateVisual();

    /// <inheritdoc cref="Variant" />
    public static readonly StyledProperty<LoadingIndicatorVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialLoadingIndicator, LoadingIndicatorVariant>(nameof(Variant));

    /// <inheritdoc cref="Value" />
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MaterialLoadingIndicator, double>(nameof(Value));

    /// <inheritdoc cref="IsIndeterminate" />
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MaterialLoadingIndicator, bool>(nameof(IsIndeterminate), true);

    /// <summary>AndroidX's independent, linear global rotation period.</summary>
    private static readonly TimeSpan GlobalRotationPeriod = TimeSpan.FromMilliseconds(4666);

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

    private static readonly double IndeterminateShapeScale = CalculateScaleFactor(IndeterminateShapes);
    private static readonly double DeterminateShapeScale = CalculateScaleFactor(DeterminateShapes);

    private static readonly Dictionary<(int List, int Index), Morph> MorphCache = new();

    private LoadingIndicatorMorphAnimation? _morph;
    private LoopingAnimation? _globalRotation;
    private SpringAnimation? _progress;

    private LoadingIndicatorMetrics _tokens = LoadingIndicatorDefaults.Metrics(LoadingIndicatorVariant.Standard);
    private LoadingIndicatorVisualColors _colors = new(
        LoadingIndicatorTokens.ActiveIndicatorColor.ResolveBrush(null, false),
        ColorTokenValue.Literal(Avalonia.Media.Colors.Transparent).ResolveBrush(null, false));

    public MaterialLoadingIndicator()
    {
        _material = MaterialElementContext.Ensure(this);
    }

    static MaterialLoadingIndicator()
    {
        AffectsRender<MaterialLoadingIndicator>(VariantProperty, IsIndeterminateProperty, ValueProperty);
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

    // Deliberate friend-assembly seam: this is the component's committed token snapshot, not a
    // compatibility API. It lets the runtime acceptance test prove the local Variant channel
    // without depending on a private field or a rasterized drawing tree.
    internal bool HasResolvedContainer => _tokens.HasContainer;

    private LoadingIndicatorMorphAnimation MorphPhase =>
        _morph ??= Motion.Add(new LoadingIndicatorMorphAnimation(IndeterminateShapes.Length));

    private LoopingAnimation GlobalRotation =>
        _globalRotation ??= Motion.Add(new LoopingAnimation(GlobalRotationPeriod));

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

        var (morph, progress, rotationDegrees) = IsIndeterminate
            ? CurrentIndeterminateMorph()
            : CurrentDeterminateMorph();

        // LoadingIndicator.kt first reserves the public 48dp container, then scales the normalized
        // path by calculateScaleFactor(polygons) * ActiveIndicatorScale. The first factor keeps all
        // rotating shapes inside their slot; the second is the token ratio (38dp / 48dp).
        var polygonScale = IsIndeterminate ? IndeterminateShapeScale : DeterminateShapeScale;
        var activeIndicatorScale = _tokens.ContainerSize <= 0
            ? 0
            : _tokens.ActiveSize / _tokens.ContainerSize;
        var active = box * polygonScale * activeIndicatorScale;
        var cubics = morph.AsCubics(progress);
        var geometry = morph.ToGeometry(progress);
        // Compose's Path.getBounds() is Skia's cached path-point bounds (anchors and controls),
        // not the mathematically tight Bézier bounds exposed by Avalonia Geometry.Bounds.
        var geometryCentre = CalculatePathPointBounds(cubics).Center;
        var transform =
            Matrix.CreateScale(active, active)
            * Matrix.CreateTranslation(-geometryCentre.X * active, -geometryCentre.Y * active)
            * Matrix.CreateRotation(rotationDegrees * Math.PI / 180)
            * Matrix.CreateTranslation(centre.X, centre.Y);

        using (context.PushTransform(transform))
            context.DrawGeometry(_colors.ActiveIndicatorColor, null, geometry);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Both upstream overloads own the same public ContainerWidth × ContainerHeight slot. The
        // standard variant simply paints that slot with Color.Unspecified/transparent.
        var size = _tokens.ContainerSize;
        return new Size(size, size);
    }

    private void ApplyTokens()
    {
        var theme = ResolvedTheme;
        _tokens = LoadingIndicatorDefaults.Metrics(Variant);
        _colors = LoadingIndicatorDefaults.Colors(theme, Variant, IsDarkVariant);
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
            // Variant is a component-local token input. Theme deduplication deliberately does not
            // model it, so reapply against the context's already committed snapshot here.
            ApplyTokens();
        }
        else if (change.Property == ValueProperty && !IsIndeterminate)
        {
            Progress.AnimateTo(Clamp(Value), Spring(MotionRole.DefaultSpatial));
            Motion.RequestFrame();
        }
        else if (change.Property == IsIndeterminateProperty)
        {
            UpdateLoops();
        }
    }

    /// <summary>
    /// The indeterminate sequence advances to a fresh morph every 650ms. Its spring makes each
    /// shape transition accelerate and settle, while the global rotation remains deliberately
    /// linear as in AndroidX LoadingIndicator.kt.
    /// </summary>
    /// <remarks>
    /// Morphs are cached because building one matches every feature of both polygons — real work
    /// to redo sixty times a second for a fixed list of shapes.
    /// </remarks>
    private (Morph Morph, double Progress, double RotationDegrees) CurrentIndeterminateMorph()
    {
        var phase = MorphPhase;
        if (!MorphCache.TryGetValue((0, phase.MorphIndex), out var morph))
        {
            morph = new Morph(
                IndeterminateShapes[phase.MorphIndex],
                IndeterminateShapes[(phase.MorphIndex + 1) % IndeterminateShapes.Length]);
            MorphCache[(0, phase.MorphIndex)] = morph;
        }

        return (morph, phase.Value, phase.MorphRotationDegrees + phase.Value * 90 + GlobalRotation.Value * 360);
    }

    /// <summary>
    /// Determinate Compose uses only consecutive morphs (not a circular sequence) and rotates
    /// counter-clockwise by half a turn across the supplied progress.
    /// </summary>
    private (Morph Morph, double Progress, double RotationDegrees) CurrentDeterminateMorph()
    {
        var phase = Clamp(Progress.Value);
        var morphCount = DeterminateShapes.Length - 1;
        var scaled = phase * morphCount;
        var index = Math.Min((int)scaled, morphCount - 1);
        var adjustedProgress = phase == 1 ? 1 : scaled - index;

        if (!MorphCache.TryGetValue((1, index), out var morph))
        {
            morph = new Morph(DeterminateShapes[index], DeterminateShapes[index + 1]);
            MorphCache[(1, index)] = morph;
        }

        return (morph, adjustedProgress, -phase * 180);
    }

    private void UpdateLoops()
    {
        if (IsIndeterminate)
        {
            MorphPhase.Resume();
            GlobalRotation.Resume();
        }
        else
        {
            MorphPhase.Settle();
            GlobalRotation.Settle();
        }

        Motion.RequestFrame();
        InvalidateVisual();
    }

    private static double Clamp(double value)
    {
        return double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
    }

    /// <summary>
    /// Port of LoadingIndicator.kt calculateScaleFactor(): compare each polygon's occupied bounds
    /// with the axis-aligned maximum bounds needed while it rotates, and retain the tightest scale.
    /// </summary>
    private static double CalculateScaleFactor(IReadOnlyList<RoundedPolygon> polygons)
    {
        var scaleFactor = 1d;
        foreach (var polygon in polygons)
        {
            var bounds = polygon.CalculateBounds();
            var maxBounds = polygon.CalculateMaxBounds();
            if (maxBounds.Width <= 0 || maxBounds.Height <= 0)
                continue;

            var scaleX = bounds.Width / maxBounds.Width;
            var scaleY = bounds.Height / maxBounds.Height;
            scaleFactor = Math.Min(scaleFactor, Math.Max(scaleX, scaleY));
        }

        return scaleFactor;
    }

    private static Rect CalculatePathPointBounds(IReadOnlyList<CubicBezier> cubics)
    {
        if (cubics.Count == 0)
            return default;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        foreach (var cubic in cubics)
        {
            Include(cubic.Anchor0);
            Include(cubic.Control0);
            Include(cubic.Control1);
            Include(cubic.Anchor1);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);

        void Include(Point point)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }
    }

}
