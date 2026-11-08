using Avalonia;
using Avalonia.Data;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

public abstract partial class Chip
{
    private AnimatedCornerRadius? _shape;
    private CornerShape?[]? _shapeConfiguration;
    private MaterialShape[] _observedShapes = [];
    private MaterialShape? _staticShape, _paintedPath;
    private readonly CutCornerShape _cutOutline = new();
    private IDisposable? _shapeContribution;
    private double _paintedShapeProgress = double.NaN;

    private void ObserveShapes()
    {
        var shapes = this.IsAttachedToVisualTree() && MorphShapes is { } values
            ? new[] { values.Shape, values.SelectedShape, values.PressedShape }.Distinct().ToArray() : [];
        if (_observedShapes.SequenceEqual(shapes)) return;
        foreach (var shape in _observedShapes) shape.PropertyChanged -= ShapeChanged;
        _observedShapes = shapes;
        foreach (var shape in shapes) shape.PropertyChanged += ShapeChanged;
    }

    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => QueueTargets();

    private void StopShape()
    {
        _shapeEffect?.Cancel(); _shapeEffect = null;
        _shape?.Dispose(); _shape = null; _shapeConfiguration = null; _staticShape = null;
        _shapeContribution?.Dispose(); _shapeContribution = null; _paintedPath = null;
        _surface?.SetAnimatedCorners(null);
        foreach (var shape in _observedShapes) shape.PropertyChanged -= ShapeChanged;
        _observedShapes = [];
    }

    private void PaintShape()
    {
        if (_surface is not { } surface) return;
        var path = _shape is { CurrentIsCut: true } ? _cutOutline : _shape is null ? _staticShape : null;
        if (_shapeContribution is null || !ReferenceEquals(path, _paintedPath))
        {
            _shapeContribution?.Dispose();
            _shapeContribution = surface.SetValue(MaterialSurface.ShapeProperty, path, BindingPriority.Animation);
            _paintedPath = path;
        }
        if (_shape is { } shape)
        {
            shape.Size = surface.Bounds.Size;
            surface.SetAnimatedCorners(shape.Current, shapeStateChanged: _paintedShapeProgress != shape.Progress);
            _paintedShapeProgress = shape.Progress;
        }
        else surface.SetAnimatedCorners(null);
    }

    private void RefreshShape(bool initial)
    {
        if (_surface is null || !this.IsAttachedToVisualTree() || _attachScope is not { } motion
            || motion.Lifetime.IsCancellationRequested) return;
        var shapes = MorphShapes;
        if (shapes is null)
        {
            StopShape();
            _surface.CornerRadius = CornerRadius;
            return;
        }
        ObserveShapes();
        MaterialShape[] paths = [shapes.Shape, shapes.SelectedShape, shapes.PressedShape];
        var configuration = paths.Select(CornerShape.FromMaterialShape).ToArray();
        var index = _sourcePressed ? 2 : Selected ? 1 : 0;
        if (configuration.Any(shape => shape is null))
        {
            // shapeByInteraction disables interpolation for the whole configuration if
            // even one state is not CornerBasedShape. Its selected shape is used directly.
            if (_shape is not null || _staticShape is not null && !ReferenceEquals(_staticShape, paths[index]))
                MaterialInteraction.GetOrCreate(this).ResetSourceIndication();
            _shapeEffect?.Cancel(); _shape?.Dispose(); _shape = null; _shapeConfiguration = null;
            _staticShape = paths[index];
            PaintShape();
            return;
        }
        var target = configuration[index]!;
        if (ShapeAnimation is not { } spec) return;
        if (initial || _shapeConfiguration is null || !_shapeConfiguration.SequenceEqual(configuration) || _shape is null)
        {
            if (_shape is not null || _staticShape is not null)
                MaterialInteraction.GetOrCreate(this).ResetSourceIndication();
            _shapeEffect?.Cancel(); _shapeEffect = null;
            _shape?.Dispose(); _shapeConfiguration = configuration; _staticShape = null;
            _shape = new AnimatedCornerRadius(target, spec, motion, PaintShape) { Size = _surface.Bounds.Size };
            PaintShape();
        }
        if (_shape.TargetShape == target) return;
        _shapeEffect?.Cancel();
        var previous = _shapeTask;
        var animated = _shape;
        _shapeTask = motion.Launch(async ct =>
        {
            if (previous is not null) { try { await previous; } catch (OperationCanceledException) { } }
            using var effect = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _shapeEffect = effect;
            try
            {
                animated.Size = _surface.Bounds.Size;
                await animated.AnimateToAsync(target, spec, effect.Token);
            }
            finally { if (ReferenceEquals(_shapeEffect, effect)) _shapeEffect = null; }
        }, "Chip shape");
    }
}
