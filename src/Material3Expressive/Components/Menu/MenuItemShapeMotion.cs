// Menu.kt shapeByInteraction(MenuItemShapes, selected, animationSpec), fixed AndroidX baseline.
// Copyright The Android Open Source Project; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using ComposeAnimation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;
using AnimatedCornerRadius = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;

namespace Material3Expressive.Components;

internal sealed class MenuItemShapeMotion : IDisposable
{
    private readonly Control _owner;
    private readonly MenuItemChrome? _nativeChrome;
    private MaterialShape? NormalPath => _owner is DropdownMenuItem item ? item.Shape : MenuAppearance.GetShape((MenuItem)_owner);
    private MaterialShape? SelectedPath => _owner is DropdownMenuItem item ? item.SelectedShape : MenuAppearance.GetSelectedShape((MenuItem)_owner);
    private readonly MaterialSurface _surface;
    private readonly AnimationChildScope _scope;
    private readonly CutCornerShape _cutOutline = new();
    private sealed record Entry(MaterialShape? Path, CornerShape? Corners)
    {
        public bool Equals(Entry? other) => other is not null && (Corners is not null
            ? Corners == other.Corners : other.Corners is null && ReferenceEquals(Path, other.Path));
        public override int GetHashCode() => Corners?.GetHashCode() ?? Path?.GetHashCode() ?? 0;
    }
    private (Entry Normal, Entry Selected, MaterialSpring Spec)? _configuration;
    private Entry? _staticShape;
    private AnimatedCornerRadius? _shape;
    private MaterialShape[] _observed = [];
    private CancellationTokenSource? _effect;
    private Task? _run;
    private bool _pending;

    internal MenuItemShapeMotion(Control owner, MaterialSurface surface, MenuItemChrome? nativeChrome = null)
    {
        _owner = owner; _surface = surface; _nativeChrome = nativeChrome;
        _scope = AnimationScope.Get(owner).CreateChild("Menu item shape");
        surface.PropertyChanged += SurfaceChanged;
        Observe(); Update();
    }
    internal void Refresh()
    {
        if (_scope.Lifetime.IsCancellationRequested) return;
        Observe();
        if (_pending) return;
        _pending = true;
        _ = _scope.Launch(async ct =>
        {
            if (!await _scope.WaitForNextFrameAsync(ct)) return;
            _pending = false; Update();
        }, "Menu shape composition");
    }
    private void Update()
    {
        var item = _owner as DropdownMenuItem;
        if ((item?.ShapeAnimation ?? _nativeChrome?.ShapeAnimation) is not { } spec) return;
        var expressive = item?.IsExpressive ?? true;
        var isSelected = item?.Selected ?? ((MenuItem)_owner).IsChecked;
        Entry Resolve(MaterialShape? path, CornerRadius corners) => new(path,
            path is null ? new CornerShape.Defined(corners) : CornerShape.FromMaterialShape(path));
        var normal = Resolve(NormalPath, item?.CornerRadius ?? ((MenuItem)_owner).CornerRadius);
        var selected = Resolve(SelectedPath, item?.SelectedCornerRadius ?? _nativeChrome!.SelectedCornerRadius);
        var configuration = (normal, selected, spec);
        var target = expressive && isSelected ? selected : normal;
        if (!expressive || normal.Corners is null || selected.Corners is null)
        {
            if (_shape is not null || _staticShape is not null && _staticShape != target) ResetIndication();
            _effect?.Cancel(); _shape?.Dispose(); _shape = null;
            _configuration = configuration; _staticShape = target; Paint(); return;
        }
        if (_shape is null || _configuration != configuration)
        {
            if (_configuration is not null) ResetIndication();
            _effect?.Cancel(); _shape?.Dispose();
            _configuration = configuration; _staticShape = null;
            _shape = new(target.Corners!, spec, _scope, Paint);
        }
        else if (_shape.TargetShape != target.Corners)
        {
            _effect?.Cancel(); var previous = _run; var shape = _shape;
            _run = _scope.Launch(async ct =>
            {
                if (previous is not null) { try { await previous; } catch (OperationCanceledException) { } }
                using var effect = CancellationTokenSource.CreateLinkedTokenSource(ct); _effect = effect;
                try { await shape.AnimateToAsync(target.Corners!, spec, effect.Token); }
                finally { if (ReferenceEquals(_effect, effect)) _effect = null; }
            }, "Menu selected shape");
        }
        Paint();
    }
    private void ResetIndication() => MaterialInteraction.GetOrCreate(_owner).ResetSourceIndication(preserveSurfaceMotion: true);
    private void Paint()
    {
        if (_shape is null)
        {
            if (_staticShape is not { } target) return;
            _surface.Shape = target.Path;
            _surface.SetAnimatedCorners(target.Path is null ? target.Corners?.Resolve(_surface.Bounds.Size) : null);
            return;
        }
        _shape.Size = _surface.Bounds.Size;
        _surface.Shape = _shape.CurrentIsCut ? _cutOutline : null;
        _surface.SetAnimatedCorners(_shape.Current, shapeStateChanged: true);
    }
    private void SurfaceChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { if (e.Property == Visual.BoundsProperty) Paint(); }
    private void Observe()
    {
        MaterialShape?[] paths = [NormalPath, SelectedPath];
        var shapes = paths.OfType<MaterialShape>().Distinct().ToArray();
        if (_observed.SequenceEqual(shapes)) return;
        foreach (var shape in _observed) shape.PropertyChanged -= ShapeChanged;
        _observed = shapes;
        foreach (var shape in shapes) shape.PropertyChanged += ShapeChanged;
    }
    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Refresh();
    public void Dispose()
    {
        foreach (var shape in _observed) shape.PropertyChanged -= ShapeChanged;
        _surface.PropertyChanged -= SurfaceChanged;
        _effect?.Cancel(); _shape?.Dispose(); _scope.Dispose();
        _surface.SetAnimatedCorners(null); _surface.Shape = null;
    }
}
