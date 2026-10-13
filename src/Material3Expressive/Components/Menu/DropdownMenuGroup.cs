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
using AnimatedCornerRadius = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

public class DropdownMenuGroup : ItemsControl
{
    public static readonly StyledProperty<MenuGroupShapes?> ShapesProperty = AvaloniaProperty.Register<DropdownMenuGroup, MenuGroupShapes?>(nameof(Shapes));
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
    private AnimationScope? _scope;
    private AnimatedCornerRadius? _shape;
    private bool _hasBeenHovered, _pending;
    private (CornerRadius, CornerRadius, MaterialSpring)? _configuration;
    private CornerRadius Target => _hasBeenHovered && !IsPointerOver ? InactiveCornerRadius : CornerRadius;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    { Stop(); base.OnApplyTemplate(e); _surface = e.NameScope.Find<MaterialSurface>("PART_GroupSurface"); Initialize(); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); Initialize(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { Stop(); _hasBeenHovered = false; base.OnDetachedFromVisualTree(e); }
    protected override void OnPointerEntered(PointerEventArgs e)
    { base.OnPointerEntered(e); Queue(); }
    protected override void OnPointerExited(PointerEventArgs e)
    { base.OnPointerExited(e); Queue(); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CornerRadiusProperty || change.Property == InactiveCornerRadiusProperty || change.Property == ShapeAnimationProperty)
        { Initialize(); Queue(); }
        if (change.Property == BoundsProperty) Paint();
    }
    private void Initialize()
    {
        if (_surface is null || !this.IsAttachedToVisualTree() || ShapeAnimation is not { } spec || _scope is not null) return;
        _scope = AnimationScope.Get(this);
        var surface = _surface;
        void BoundsChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        { if (e.Property == BoundsProperty) Paint(); }
        surface.PropertyChanged += BoundsChanged;
        _scope.Lifetime.Register(() => surface.PropertyChanged -= BoundsChanged);
        ResetShape(spec);
    }
    private void ResetShape(MaterialSpring spec)
    {
        _shape?.Dispose();
        _configuration = (CornerRadius, InactiveCornerRadius, spec);
        _shape = new AnimatedCornerRadius(Target, spec, _scope!, Paint);
        Paint();
    }
    private void Queue()
    {
        if (_pending || !this.IsAttachedToVisualTree() || _scope is null) return;
        var motion = _scope = AnimationScope.Get(this);
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct); _pending = false;
            if (_scope is null) return;
            // Like the composable, remember hover only when a composition observes it.
            _hasBeenHovered |= IsPointerOver;
            if (ShapeAnimation is not { } spec) return;
            if (_configuration != (CornerRadius, InactiveCornerRadius, spec)) ResetShape(spec);
            if (_shape is { } shape && shape.Target != Target)
                await shape.AnimateToAsync(Target, spec, ct);
        }, "Menu group shape");
    }
    private void Paint()
    {
        if (_surface is null || _shape is null) return;
        _shape.Size = _surface.Bounds.Size;
        _surface.SetAnimatedCorners(_shape.Current);
    }
    private void Stop()
    { _shape?.Dispose(); _shape = null; _surface?.SetAnimatedCorners(null); _scope = null; _pending = false; _configuration = null; }
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
