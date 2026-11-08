/* Menu.kt: DropdownMenuGroup / shapeByInteraction.
 * Copyright 2025-2026 The Android Open Source Project; Apache-2.0. */
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using AnimatedCornerRadius = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

public class DropdownMenuGroup : ItemsControl
{
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = AvaloniaProperty.Register<DropdownMenuGroup, MaterialInteractionSource?>(nameof(InteractionSource));
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    public static readonly StyledProperty<MenuGroupShapes?> ShapesProperty = AvaloniaProperty.Register<DropdownMenuGroup, MenuGroupShapes?>(nameof(Shapes));
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = AvaloniaProperty.Register<DropdownMenuGroup, MaterialShape?>(nameof(Shape));
    public static readonly StyledProperty<MaterialShape?> InactiveShapeProperty = AvaloniaProperty.Register<DropdownMenuGroup, MaterialShape?>(nameof(InactiveShape));
    /// <summary>Initial and hovered outline; null uses CornerRadius / Shapes.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    /// <summary>Outline after leaving a previously hovered group; null uses InactiveCornerRadius / Shapes.</summary>
    public MaterialShape? InactiveShape { get => GetValue(InactiveShapeProperty); set => SetValue(InactiveShapeProperty, value); }
    public static readonly StyledProperty<double> TonalElevationProperty = AvaloniaProperty.Register<DropdownMenuGroup, double>(nameof(TonalElevation));
    public static readonly StyledProperty<double> ShadowElevationProperty = AvaloniaProperty.Register<DropdownMenuGroup, double>(nameof(ShadowElevation));
    public static readonly StyledProperty<CornerRadius> InactiveCornerRadiusProperty = AvaloniaProperty.Register<DropdownMenuGroup, CornerRadius>(nameof(InactiveCornerRadius));
    internal static readonly StyledProperty<MaterialSpring?> ShapeAnimationProperty = AvaloniaProperty.Register<DropdownMenuGroup, MaterialSpring?>(nameof(ShapeAnimation));
    public MenuGroupShapes? Shapes { get => GetValue(ShapesProperty); set => SetValue(ShapesProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    public CornerRadius InactiveCornerRadius { get => GetValue(InactiveCornerRadiusProperty); set => SetValue(InactiveCornerRadiusProperty, value); }
    internal MaterialSpring? ShapeAnimation { get => GetValue(ShapeAnimationProperty); set => SetValue(ShapeAnimationProperty, value); }
    private MaterialSurface? _surface;
    private AnimatedCornerRadius? _shape;
    private AnimationChildScope? _attachScope;
    private bool _hasBeenHovered, _pending;
    private readonly MaterialInteractionSource _localSource = new();
    private MaterialInteractionSource? _source;
    private MaterialInteractionEvent.HoverEnter? _nativeHover;
    private readonly List<MaterialInteractionEvent.HoverEnter> _hoverInteractions = [];
    private bool _hovered;
    private sealed record ShapeEntry(MaterialShape? Path, CornerShape? Corners)
    {
        public bool Equals(ShapeEntry? other) => other is not null && (Corners is not null
            ? Corners == other.Corners : other.Corners is null && ReferenceEquals(Path, other.Path));
        public override int GetHashCode() => Corners?.GetHashCode() ?? Path?.GetHashCode() ?? 0;
    }
    private (ShapeEntry Normal, ShapeEntry Inactive, MaterialSpring Spec)? _configuration;
    private MaterialShape[] _observedShapes = [];
    private readonly CutCornerShape _cutOutline = new();
    private bool _initialized;
    private bool Inactive => _hasBeenHovered && !_hovered;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_surface is not null) _surface.PropertyChanged -= SurfaceChanged;
        Stop(); base.OnApplyTemplate(e); _surface = e.NameScope.Find<MaterialSurface>("PART_GroupSurface");
        if (_surface is not null) _surface.PropertyChanged += SurfaceChanged;
        Initialize();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); _attachScope = AnimationScope.Get(this).CreateChild("Menu group"); ConnectSource(); Initialize(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EndNativeHover();
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = null; _hoverInteractions.Clear(); _hovered = false;
        Stop(); _hasBeenHovered = false; _attachScope?.Dispose(); _attachScope = null; base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (_source is not null && _nativeHover is null)
        { _nativeHover = new(); _source.Emit(_nativeHover); }
    }
    protected override void OnPointerExited(PointerEventArgs e)
    { base.OnPointerExited(e); EndNativeHover(); }
    private void EndNativeHover()
    {
        if (_nativeHover is not { } hover) return;
        _nativeHover = null; _source?.Emit(new MaterialInteractionEvent.HoverExit(hover));
    }
    private void ConnectSource()
    {
        if (!this.IsAttachedToVisualTree()) return;
        var source = InteractionSource ?? _localSource;
        if (ReferenceEquals(source, _source)) return;
        EndNativeHover();
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = source; _hoverInteractions.Clear();
        // collectIsHoveredAsState remembers the Boolean outside LaunchedEffect(source),
        // but creates its list inside it. Replacement retains the Boolean until any event.
        _source.Interaction += ReceiveInteraction;
    }
    private void ReceiveInteraction(MaterialInteractionEvent interaction)
    {
        if (interaction is MaterialInteractionEvent.HoverEnter enter) _hoverInteractions.Add(enter);
        else if (interaction is MaterialInteractionEvent.HoverExit exit) _hoverInteractions.Remove(exit.Start);
        var hovered = _hoverInteractions.Count != 0;
        if (_hovered == hovered) return;
        _hovered = hovered; Queue();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty) ConnectSource();
        if (change.Property == CornerRadiusProperty || change.Property == InactiveCornerRadiusProperty || change.Property == ShapeAnimationProperty
            || change.Property == ShapeProperty || change.Property == InactiveShapeProperty)
        { ObserveShapes(); Initialize(); Queue(); }
        if (change.Property == BoundsProperty) Paint();
    }
    private void Initialize()
    {
        if (_surface is null || !this.IsAttachedToVisualTree() || ShapeAnimation is not { } spec || _initialized
            || _attachScope is not { } scope || scope.Lifetime.IsCancellationRequested) return;
        ObserveShapes();
        _initialized = true;
        ResetShape(Configuration(spec));
    }
    private (ShapeEntry Normal, ShapeEntry Inactive, MaterialSpring Spec) Configuration(MaterialSpring spec)
    {
        ShapeEntry Resolve(MaterialShape? path, CornerRadius corners) => new(path,
            path is CutCornerShape cut ? CornerShape.Cut.From(cut) : path is null ? new CornerShape.Defined(corners) : null);
        return (Resolve(Shape, CornerRadius), Resolve(InactiveShape, InactiveCornerRadius), spec);
    }
    private void ResetShape((ShapeEntry Normal, ShapeEntry Inactive, MaterialSpring Spec) configuration)
    {
        _shape?.Dispose();
        _configuration = configuration;
        var target = Inactive ? configuration.Inactive : configuration.Normal;
        _shape = configuration.Normal.Corners is not null && configuration.Inactive.Corners is not null
            ? new AnimatedCornerRadius(target.Corners!, configuration.Spec, _attachScope!, Paint) : null;
        Paint();
    }
    private void Queue()
    {
        if (_pending || !this.IsAttachedToVisualTree() || !_initialized || _attachScope is not { } motion
            || motion.Lifetime.IsCancellationRequested) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            if (!await motion.WaitForNextFrameAsync(ct)) return; _pending = false;
            if (!_initialized) return;
            // Like the composable, remember hover only when a composition observes it.
            _hasBeenHovered |= _hovered;
            if (ShapeAnimation is not { } spec) return;
            var configuration = Configuration(spec);
            if (_configuration != configuration) ResetShape(configuration);
            var target = Inactive ? configuration.Inactive : configuration.Normal;
            if (_shape is { } shape && shape.TargetShape != target.Corners)
                await shape.AnimateToAsync(target.Corners!, spec, ct);
            Paint();
        }, "Menu group shape");
    }
    private void Paint()
    {
        if (_surface is null || _configuration is not { } configuration) return;
        if (_shape is null)
        {
            var target = Inactive ? configuration.Inactive : configuration.Normal;
            _surface.Shape = target.Path;
            _surface.SetAnimatedCorners(target.Path is null ? target.Corners?.Resolve(_surface.Bounds.Size) : null);
            return;
        }
        _shape.Size = _surface.Bounds.Size;
        _surface.Shape = _shape.CurrentIsCut ? _cutOutline : null;
        _surface.SetAnimatedCorners(_shape.Current);
    }
    private void SurfaceChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { if (e.Property == BoundsProperty) Paint(); }
    private void ObserveShapes()
    {
        MaterialShape?[] candidates = this.IsAttachedToVisualTree() ? [Shape, InactiveShape] : [];
        var shapes = candidates.OfType<MaterialShape>().Distinct().ToArray();
        if (_observedShapes.SequenceEqual(shapes)) return;
        foreach (var shape in _observedShapes) shape.PropertyChanged -= ShapeChanged;
        _observedShapes = shapes;
        foreach (var shape in shapes) shape.PropertyChanged += ShapeChanged;
    }
    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Queue();
    private void Stop()
    {
        foreach (var shape in _observedShapes) shape.PropertyChanged -= ShapeChanged;
        _observedShapes = [];
        _shape?.Dispose(); _shape = null; _surface?.SetAnimatedCorners(null);
        if (_surface is not null) _surface.Shape = null;
        _pending = false; _configuration = null; _initialized = false;
    }
}

internal sealed class MenuShapeConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count is not (2 or 3)) return AvaloniaProperty.UnsetValue;
        return values[0] switch
        {
            MenuItemShapes s => (parameter as string == "Selected" ? s.SelectedShape : s.Shape) ?? values[1],
            MenuGroupShapes s => (parameter as string == "Inactive" ? s.InactiveShape : s.Shape)
                ?? (parameter as string != "Inactive" && s.UsesMiddleShape && values.Count == 3 ? values[2] : values[1]),
            _ => values[1]
        };
    }
}
