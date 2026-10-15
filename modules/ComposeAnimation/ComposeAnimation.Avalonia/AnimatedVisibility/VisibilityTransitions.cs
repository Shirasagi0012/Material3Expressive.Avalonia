using System.Collections;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Metadata;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>Reusable enter declaration. Playback belongs to a visibility scope, not this object.</summary>
public abstract class EnterTransition
{
    private protected EnterTransition()
    {
    }

    public static EnterTransition None { get; } = new EmptyEnter();
    internal abstract void Apply(EnterExitEffects.Builder effects);

    public static EnterTransition operator +(EnterTransition left, EnterTransition right)
        => new EnterTransitionGroup { left, right };

    private sealed class EmptyEnter : EnterTransition
    {
        internal override void Apply(EnterExitEffects.Builder effects)
        {
        }
    }
}

/// <summary>Reusable exit declaration. Effects of different kinds run in parallel.</summary>
public abstract class ExitTransition
{
    private protected ExitTransition()
    {
    }

    public static ExitTransition None { get; } = new EmptyExit();

    /// <summary>
    /// Defers disposal of exiting AnimatedContent until the parent content transform finishes.
    /// AnimatedVisibility ignores this flag.
    /// </summary>
    public static ExitTransition KeepUntilTransitionsFinished { get; } = new HoldExit();

    internal abstract void Apply(EnterExitEffects.Builder effects);

    public static ExitTransition operator +(ExitTransition left, ExitTransition right)
        => new ExitTransitionGroup { left, right };

    private sealed class EmptyExit : ExitTransition
    {
        internal override void Apply(EnterExitEffects.Builder effects)
        {
        }
    }

    private sealed class HoldExit : ExitTransition
    {
        internal override void Apply(EnterExitEffects.Builder e) => e.Hold = true;
    }
}

public sealed class EnterTransitionGroup : EnterTransition, IEnumerable<EnterTransition>
{
    [Content] public IList<EnterTransition> Children { get; } = new List<EnterTransition>();

    public void Add(EnterTransition transition) =>
        Children.Add(transition ?? throw new ArgumentNullException(nameof(transition)));

    public IEnumerator<EnterTransition> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal override void Apply(EnterExitEffects.Builder effects) => EnterExitEffects.ApplyGroup(this, Children, effects);
}

public sealed class ExitTransitionGroup : ExitTransition, IEnumerable<ExitTransition>
{
    [Content] public IList<ExitTransition> Children { get; } = new List<ExitTransition>();
    public void Add(ExitTransition transition) => Children.Add(transition ?? throw new ArgumentNullException(nameof(transition)));
    public IEnumerator<ExitTransition> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal override void Apply(EnterExitEffects.Builder effects) => EnterExitEffects.ApplyGroup(this, Children, effects);
}

public sealed class FadeIn : EnterTransition
{
    public double InitialAlpha { get; set; }
    public IFiniteAnimationSpec<double> Spec { get; set; } = EnterExitEffects.Spring<double>();

    internal override void Apply(EnterExitEffects.Builder e) =>
        e.Fade = new EnterExitEffects.FadeEffect(EnterExitEffects.Alpha(InitialAlpha), Spec);
}

public sealed class FadeOut : ExitTransition
{
    public double TargetAlpha { get; set; }
    public IFiniteAnimationSpec<double> Spec { get; set; } = EnterExitEffects.Spring<double>();

    internal override void Apply(EnterExitEffects.Builder e) =>
        e.Fade = new EnterExitEffects.FadeEffect(EnterExitEffects.Alpha(TargetAlpha), Spec);
}

public sealed class ScaleIn : EnterTransition
{
    public double InitialScale { get; set; }
    public RelativePoint TransformOrigin { get; set; } = RelativePoint.Center;
    public IFiniteAnimationSpec<double> Spec { get; set; } = EnterExitEffects.Spring<double>();

    internal override void Apply(EnterExitEffects.Builder e) =>
        e.Scale = new EnterExitEffects.ScaleEffect(EnterExitEffects.Finite(InitialScale), Spec, TransformOrigin);
}

public sealed class ScaleOut : ExitTransition
{
    public double TargetScale { get; set; }
    public RelativePoint TransformOrigin { get; set; } = RelativePoint.Center;
    public IFiniteAnimationSpec<double> Spec { get; set; } = EnterExitEffects.Spring<double>();

    internal override void Apply(EnterExitEffects.Builder e) =>
        e.Scale = new EnterExitEffects.ScaleEffect(EnterExitEffects.Finite(TargetScale), Spec, TransformOrigin);
}

public class SlideIn : EnterTransition
{
    public Vector InitialOffset { get; set; }
    public Vector InitialOffsetRatio { get; set; }
    public Func<Size, Vector>? InitialOffsetSelector { get; set; }
    public IFiniteAnimationSpec<Vector> Spec { get; set; } = EnterExitEffects.Spring<Vector>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        Vector offset = InitialOffset;
        Vector ratio = InitialOffsetRatio;
        e.Slide = new EnterExitEffects.SlideEffect(
            InitialOffsetSelector ?? (s => offset + new Vector(s.Width * ratio.X, s.Height * ratio.Y)), Spec);
    }
}

public class SlideOut : ExitTransition
{
    public Vector TargetOffset { get; set; }
    public Vector TargetOffsetRatio { get; set; }
    public Func<Size, Vector>? TargetOffsetSelector { get; set; }
    public IFiniteAnimationSpec<Vector> Spec { get; set; } = EnterExitEffects.Spring<Vector>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        Vector offset = TargetOffset;
        Vector ratio = TargetOffsetRatio;
        e.Slide = new EnterExitEffects.SlideEffect(
            TargetOffsetSelector ?? (s => offset + new Vector(s.Width * ratio.X, s.Height * ratio.Y)), Spec);
    }
}

public sealed class SlideInHorizontally : EnterTransition
{
    public double InitialOffset { get; set; }
    public double InitialOffsetRatio { get; set; } = -0.5;
    public Func<double, double>? InitialOffsetSelector { get; set; }
    public IFiniteAnimationSpec<Vector> Spec { get; set; } = EnterExitEffects.Spring<Vector>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double offset = InitialOffset;
        double ratio = InitialOffsetRatio;
        Func<double, double>? selector = InitialOffsetSelector;
        e.Slide = new EnterExitEffects.SlideEffect(s => new Vector(selector?.Invoke(s.Width) ?? offset + ratio * s.Width, 0),
            Spec);
    }
}

public sealed class SlideInVertically : EnterTransition
{
    public double InitialOffset { get; set; }
    public double InitialOffsetRatio { get; set; } = -0.5;
    public Func<double, double>? InitialOffsetSelector { get; set; }
    public IFiniteAnimationSpec<Vector> Spec { get; set; } = EnterExitEffects.Spring<Vector>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double offset = InitialOffset;
        double ratio = InitialOffsetRatio;
        Func<double, double>? selector = InitialOffsetSelector;
        e.Slide = new EnterExitEffects.SlideEffect(s => new Vector(0, selector?.Invoke(s.Height) ?? offset + ratio * s.Height),
            Spec);
    }
}

public sealed class SlideOutHorizontally : ExitTransition
{
    public double TargetOffset { get; set; }
    public double TargetOffsetRatio { get; set; } = -0.5;
    public Func<double, double>? TargetOffsetSelector { get; set; }
    public IFiniteAnimationSpec<Vector> Spec { get; set; } = EnterExitEffects.Spring<Vector>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double offset = TargetOffset;
        double ratio = TargetOffsetRatio;
        Func<double, double>? selector = TargetOffsetSelector;
        e.Slide = new EnterExitEffects.SlideEffect(s => new Vector(selector?.Invoke(s.Width) ?? offset + ratio * s.Width, 0),
            Spec);
    }
}

public sealed class SlideOutVertically : ExitTransition
{
    public double TargetOffset { get; set; }
    public double TargetOffsetRatio { get; set; } = -0.5;
    public Func<double, double>? TargetOffsetSelector { get; set; }
    public IFiniteAnimationSpec<Vector> Spec { get; set; } = EnterExitEffects.Spring<Vector>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double offset = TargetOffset;
        double ratio = TargetOffsetRatio;
        Func<double, double>? selector = TargetOffsetSelector;
        e.Slide = new EnterExitEffects.SlideEffect(s => new Vector(0, selector?.Invoke(s.Height) ?? offset + ratio * s.Height),
            Spec);
    }
}

/// <summary>Logical horizontal alignment; Start and End respect FlowDirection.</summary>
public enum VisibilityAlignment
{
    TopStart,
    Top,
    TopEnd,
    Start,
    Center,
    End,
    BottomStart,
    Bottom,
    BottomEnd
}

public enum VisibilityHorizontalAlignment
{
    Start,
    Center,
    End
}

public sealed class ExpandIn : EnterTransition
{
    public Size InitialSize { get; set; }
    public Vector InitialSizeRatio { get; set; }
    public Func<Size, Size>? InitialSizeSelector { get; set; }
    public VisibilityAlignment ExpandFrom { get; set; } = VisibilityAlignment.BottomEnd;
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size> Spec { get; set; } = EnterExitEffects.Spring<Size>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        Size size = InitialSize;
        Vector ratio = InitialSizeRatio;
        e.ChangeSize = new EnterExitEffects.SizeEffect(
            InitialSizeSelector ?? (s => new Size(size.Width + s.Width * ratio.X, size.Height + s.Height * ratio.Y)), Spec,
            ExpandFrom, Clip);
    }
}

public sealed class ShrinkOut : ExitTransition
{
    public Size TargetSize { get; set; }
    public Vector TargetSizeRatio { get; set; }
    public Func<Size, Size>? TargetSizeSelector { get; set; }
    public VisibilityAlignment ShrinkTowards { get; set; } = VisibilityAlignment.BottomEnd;
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size> Spec { get; set; } = EnterExitEffects.Spring<Size>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        Size size = TargetSize;
        Vector ratio = TargetSizeRatio;
        e.ChangeSize = new EnterExitEffects.SizeEffect(
            TargetSizeSelector ?? (s => new Size(size.Width + s.Width * ratio.X, size.Height + s.Height * ratio.Y)), Spec,
            ShrinkTowards, Clip);
    }
}

public sealed class ExpandHorizontally : EnterTransition
{
    public double InitialWidth { get; set; }
    public double InitialWidthRatio { get; set; }
    public Func<double, double>? InitialWidthSelector { get; set; }
    public VisibilityHorizontalAlignment ExpandFrom { get; set; } = VisibilityHorizontalAlignment.End;
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size> Spec { get; set; } = EnterExitEffects.Spring<Size>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double value = InitialWidth;
        double ratio = InitialWidthRatio;
        Func<double, double>? selector = InitialWidthSelector;
        e.ChangeSize = new EnterExitEffects.SizeEffect(
            s => new Size(selector?.Invoke(s.Width) ?? value + ratio * s.Width, s.Height), Spec,
            EnterExitEffects.Align(ExpandFrom), Clip);
    }
}

public sealed class ShrinkHorizontally : ExitTransition
{
    public double TargetWidth { get; set; }
    public double TargetWidthRatio { get; set; }
    public Func<double, double>? TargetWidthSelector { get; set; }
    public VisibilityHorizontalAlignment ShrinkTowards { get; set; } = VisibilityHorizontalAlignment.End;
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size> Spec { get; set; } = EnterExitEffects.Spring<Size>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double value = TargetWidth;
        double ratio = TargetWidthRatio;
        Func<double, double>? selector = TargetWidthSelector;
        e.ChangeSize = new EnterExitEffects.SizeEffect(
            s => new Size(selector?.Invoke(s.Width) ?? value + ratio * s.Width, s.Height), Spec,
            EnterExitEffects.Align(ShrinkTowards), Clip);
    }
}

public sealed class ExpandVertically : EnterTransition
{
    public double InitialHeight { get; set; }
    public double InitialHeightRatio { get; set; }
    public Func<double, double>? InitialHeightSelector { get; set; }
    public VerticalAlignment ExpandFrom { get; set; } = VerticalAlignment.Bottom;
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size> Spec { get; set; } = EnterExitEffects.Spring<Size>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double value = InitialHeight;
        double ratio = InitialHeightRatio;
        Func<double, double>? selector = InitialHeightSelector;
        e.ChangeSize = new EnterExitEffects.SizeEffect(
            s => new Size(s.Width, selector?.Invoke(s.Height) ?? value + ratio * s.Height), Spec,
            EnterExitEffects.Align(ExpandFrom), Clip);
    }
}

public sealed class ShrinkVertically : ExitTransition
{
    public double TargetHeight { get; set; }
    public double TargetHeightRatio { get; set; }
    public Func<double, double>? TargetHeightSelector { get; set; }
    public VerticalAlignment ShrinkTowards { get; set; } = VerticalAlignment.Bottom;
    public bool Clip { get; set; } = true;
    public IFiniteAnimationSpec<Size> Spec { get; set; } = EnterExitEffects.Spring<Size>();

    internal override void Apply(EnterExitEffects.Builder e)
    {
        double value = TargetHeight;
        double ratio = TargetHeightRatio;
        Func<double, double>? selector = TargetHeightSelector;
        e.ChangeSize = new EnterExitEffects.SizeEffect(
            s => new Size(s.Width, selector?.Invoke(s.Height) ?? value + ratio * s.Height), Spec,
            EnterExitEffects.Align(ShrinkTowards), Clip);
    }
}
