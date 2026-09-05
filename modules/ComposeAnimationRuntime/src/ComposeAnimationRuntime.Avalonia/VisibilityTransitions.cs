using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ComposeAnimationRuntime.Avalonia;

public abstract class VisibilityEffect
{
    internal abstract void Apply(Control control, double progress);
}

public sealed class FadeEffect(double from, double to) : VisibilityEffect
{
    internal override void Apply(Control control, double progress) => control.Opacity = from + (to - from) * progress;
}

public sealed class ScaleEffect(double from, double to) : VisibilityEffect
{
    internal override void Apply(Control control, double progress)
    {
        var value = from + (to - from) * progress;
        TransformComposition.Add(control, new ScaleTransform(value, value));
        control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
    }
}

public sealed class SlideEffect(Vector offset) : VisibilityEffect
{
    internal override void Apply(Control control, double progress) => TransformComposition.Add(control, new TranslateTransform(offset.X * (1 - progress), offset.Y * (1 - progress)));
}

public sealed class RotateEffect(double from, double to) : VisibilityEffect
{
    internal override void Apply(Control control, double progress)
        => TransformComposition.Add(control, new RotateTransform(from + (to - from) * progress));
}

public sealed class SkewEffect(double fromX, double fromY, double toX, double toY) : VisibilityEffect
{
    internal override void Apply(Control control, double progress)
    {
        var x = fromX + (toX - fromX) * progress;
        var y = fromY + (toY - fromY) * progress;
        TransformComposition.Add(control, new SkewTransform(x, y));
    }
}

public enum SizeTransformAxis { Vertical, Horizontal, Both }

public sealed class SizeTransformEffect(bool expand, SizeTransformAxis axis) : VisibilityEffect
{
    internal override void Apply(Control control, double progress)
    {
        var value = expand ? progress : 1 - progress;
        control.ClipToBounds = true;
        var scale = Math.Clamp(value, 0, 1);
        TransformComposition.Add(control, axis switch
        {
            SizeTransformAxis.Horizontal => new ScaleTransform(scale, 1),
            SizeTransformAxis.Both => new ScaleTransform(scale, scale),
            _ => new ScaleTransform(1, scale),
        });
        control.RenderTransformOrigin = new RelativePoint(0.5, 0, RelativeUnit.Relative);
    }
}

public static class SizeTransform
{
    public static VisibilityEffect Expand(SizeTransformAxis axis = SizeTransformAxis.Both) => new SizeTransformEffect(true, axis);
    public static VisibilityEffect Shrink(SizeTransformAxis axis = SizeTransformAxis.Both) => new SizeTransformEffect(false, axis);
}

public sealed class EnterTransition
{
    private readonly IReadOnlyList<VisibilityEffect> _effects;
    private EnterTransition(IReadOnlyList<VisibilityEffect> effects) => _effects = effects;
    public static EnterTransition None { get; } = new([]);
    public static EnterTransition FadeIn(double from = 0) => new([new FadeEffect(from, 1)]);
    public static EnterTransition ScaleIn(double from = 0.92) => new([new ScaleEffect(from, 1)]);
    public static EnterTransition SlideIn(Vector offset) => new([new SlideEffect(offset)]);
    public static EnterTransition RotateIn(double from = -8) => new([new RotateEffect(from, 0)]);
    public static EnterTransition SkewIn(double fromX = 0, double fromY = 0) => new([new SkewEffect(fromX, fromY, 0, 0)]);
    public static EnterTransition ExpandVertically() => new([new SizeTransformEffect(true, SizeTransformAxis.Vertical)]);
    public static EnterTransition ExpandHorizontally() => new([new SizeTransformEffect(true, SizeTransformAxis.Horizontal)]);
    public static EnterTransition ExpandIn() => new([new SizeTransformEffect(true, SizeTransformAxis.Both)]);
    public EnterTransition TogetherWith(EnterTransition other) { ArgumentNullException.ThrowIfNull(other); return new(_effects.Concat(other._effects).ToArray()); }
    internal void Apply(Control control, double progress)
    {
        TransformComposition.Begin(control);
        foreach (var effect in _effects) effect.Apply(control, progress);
    }
}

public sealed class ExitTransition
{
    private readonly IReadOnlyList<VisibilityEffect> _effects;
    private ExitTransition(IReadOnlyList<VisibilityEffect> effects) => _effects = effects;
    public static ExitTransition None { get; } = new([]);
    public static ExitTransition FadeOut(double to = 0) => new([new FadeEffect(1, to)]);
    public static ExitTransition ScaleOut(double to = 0.92) => new([new ScaleEffect(1, to)]);
    public static ExitTransition SlideOut(Vector offset) => new([new SlideEffect(offset)]);
    public static ExitTransition RotateOut(double to = 8) => new([new RotateEffect(0, to)]);
    public static ExitTransition SkewOut(double toX = 0, double toY = 0) => new([new SkewEffect(0, 0, toX, toY)]);
    public static ExitTransition ShrinkVertically() => new([new SizeTransformEffect(false, SizeTransformAxis.Vertical)]);
    public static ExitTransition ShrinkHorizontally() => new([new SizeTransformEffect(false, SizeTransformAxis.Horizontal)]);
    public static ExitTransition ShrinkOut() => new([new SizeTransformEffect(false, SizeTransformAxis.Both)]);
    public ExitTransition TogetherWith(ExitTransition other) { ArgumentNullException.ThrowIfNull(other); return new(_effects.Concat(other._effects).ToArray()); }
    internal void Apply(Control control, double progress)
    {
        TransformComposition.Begin(control);
        foreach (var effect in _effects) effect.Apply(control, progress);
    }
}

public sealed class ContentTransform
{
    public ContentTransform(EnterTransition enter, ExitTransition exit)
    {
        Enter = enter ?? throw new ArgumentNullException(nameof(enter));
        Exit = exit ?? throw new ArgumentNullException(nameof(exit));
    }

    public EnterTransition Enter { get; }
    public ExitTransition Exit { get; }
    public VisibilityEffect? SizeTransform { get; init; }
}
