using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>Typed factories for retained animation state, with shared converters.</summary>
public static class AnimatedState
{
    private static readonly DoubleConverter SharedDoubleConverter = new();
    private static readonly FloatConverter SharedFloatConverter = new();
    private static readonly Int32Converter SharedInt32Converter = new();
    private static readonly PointConverter SharedPointConverter = new();
    private static readonly VectorConverter SharedVectorConverter = new();
    private static readonly SizeConverter SharedSizeConverter = new();
    private static readonly RectConverter SharedRectConverter = new();
    private static readonly ThicknessConverter SharedThicknessConverter = new();
    private static readonly CornerRadiusConverter SharedCornerRadiusConverter = new();
    private static readonly ColorConverter SharedColorConverter = new();

    public static AnimatedState<double> Create(Visual owner, double initialValue,
        IAnimationSpec<double>? defaultAnimationSpec = null,
        Optional<double> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedDoubleConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<double> Create(Visual owner, double initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<double>? defaultAnimationSpec = null,
        Optional<double> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedDoubleConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<double> Create(AnimationScope scope, double initialValue,
        IAnimationSpec<double>? defaultAnimationSpec = null,
        Optional<double> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedDoubleConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<double> Create(AnimationScope scope, double initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<double>? defaultAnimationSpec = null,
        Optional<double> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedDoubleConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<float> Create(Visual owner, float initialValue,
        IAnimationSpec<float>? defaultAnimationSpec = null,
        Optional<float> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedFloatConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<float> Create(Visual owner, float initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<float>? defaultAnimationSpec = null,
        Optional<float> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedFloatConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<float> Create(AnimationScope scope, float initialValue,
        IAnimationSpec<float>? defaultAnimationSpec = null,
        Optional<float> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedFloatConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<float> Create(AnimationScope scope, float initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<float>? defaultAnimationSpec = null,
        Optional<float> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedFloatConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<int> Create(Visual owner, int initialValue,
        IAnimationSpec<int>? defaultAnimationSpec = null,
        Optional<int> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedInt32Converter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<int> Create(Visual owner, int initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<int>? defaultAnimationSpec = null,
        Optional<int> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedInt32Converter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<int> Create(AnimationScope scope, int initialValue,
        IAnimationSpec<int>? defaultAnimationSpec = null,
        Optional<int> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedInt32Converter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<int> Create(AnimationScope scope, int initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<int>? defaultAnimationSpec = null,
        Optional<int> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedInt32Converter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Point> Create(Visual owner, Point initialValue,
        IAnimationSpec<Point>? defaultAnimationSpec = null,
        Optional<Point> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedPointConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Point> Create(Visual owner, Point initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Point>? defaultAnimationSpec = null,
        Optional<Point> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedPointConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Point> Create(AnimationScope scope, Point initialValue,
        IAnimationSpec<Point>? defaultAnimationSpec = null,
        Optional<Point> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedPointConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Point> Create(AnimationScope scope, Point initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Point>? defaultAnimationSpec = null,
        Optional<Point> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedPointConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Vector> Create(Visual owner, Vector initialValue,
        IAnimationSpec<Vector>? defaultAnimationSpec = null,
        Optional<Vector> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedVectorConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Vector> Create(Visual owner, Vector initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Vector>? defaultAnimationSpec = null,
        Optional<Vector> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedVectorConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Vector> Create(AnimationScope scope, Vector initialValue,
        IAnimationSpec<Vector>? defaultAnimationSpec = null,
        Optional<Vector> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedVectorConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Vector> Create(AnimationScope scope, Vector initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Vector>? defaultAnimationSpec = null,
        Optional<Vector> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedVectorConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Size> Create(Visual owner, Size initialValue,
        IAnimationSpec<Size>? defaultAnimationSpec = null,
        Optional<Size> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedSizeConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Size> Create(Visual owner, Size initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Size>? defaultAnimationSpec = null,
        Optional<Size> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedSizeConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Size> Create(AnimationScope scope, Size initialValue,
        IAnimationSpec<Size>? defaultAnimationSpec = null,
        Optional<Size> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedSizeConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Size> Create(AnimationScope scope, Size initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Size>? defaultAnimationSpec = null,
        Optional<Size> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedSizeConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Rect> Create(Visual owner, Rect initialValue,
        IAnimationSpec<Rect>? defaultAnimationSpec = null,
        Optional<Rect> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedRectConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Rect> Create(Visual owner, Rect initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Rect>? defaultAnimationSpec = null,
        Optional<Rect> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedRectConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Rect> Create(AnimationScope scope, Rect initialValue,
        IAnimationSpec<Rect>? defaultAnimationSpec = null,
        Optional<Rect> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedRectConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Rect> Create(AnimationScope scope, Rect initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Rect>? defaultAnimationSpec = null,
        Optional<Rect> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedRectConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Thickness> Create(Visual owner, Thickness initialValue,
        IAnimationSpec<Thickness>? defaultAnimationSpec = null,
        Optional<Thickness> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedThicknessConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Thickness> Create(Visual owner, Thickness initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Thickness>? defaultAnimationSpec = null,
        Optional<Thickness> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedThicknessConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Thickness> Create(AnimationScope scope, Thickness initialValue,
        IAnimationSpec<Thickness>? defaultAnimationSpec = null,
        Optional<Thickness> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedThicknessConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Thickness> Create(AnimationScope scope, Thickness initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Thickness>? defaultAnimationSpec = null,
        Optional<Thickness> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedThicknessConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<CornerRadius> Create(Visual owner, CornerRadius initialValue,
        IAnimationSpec<CornerRadius>? defaultAnimationSpec = null,
        Optional<CornerRadius> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedCornerRadiusConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<CornerRadius> Create(Visual owner, CornerRadius initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<CornerRadius>? defaultAnimationSpec = null,
        Optional<CornerRadius> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedCornerRadiusConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<CornerRadius> Create(AnimationScope scope, CornerRadius initialValue,
        IAnimationSpec<CornerRadius>? defaultAnimationSpec = null,
        Optional<CornerRadius> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedCornerRadiusConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<CornerRadius> Create(AnimationScope scope, CornerRadius initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<CornerRadius>? defaultAnimationSpec = null,
        Optional<CornerRadius> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedCornerRadiusConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Color> Create(Visual owner, Color initialValue,
        IAnimationSpec<Color>? defaultAnimationSpec = null,
        Optional<Color> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedColorConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Color> Create(Visual owner, Color initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Color>? defaultAnimationSpec = null,
        Optional<Color> visibilityThreshold = default, string? label = null)
        => new(owner, initialValue, SharedColorConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Color> Create(AnimationScope scope, Color initialValue,
        IAnimationSpec<Color>? defaultAnimationSpec = null,
        Optional<Color> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedColorConverter, defaultAnimationSpec, visibilityThreshold, label);

    public static AnimatedState<Color> Create(AnimationScope scope, Color initialValue,
        AnimationCommitPhase commitPhase, AnimationInterruptionMode interruptionMode = AnimationInterruptionMode.Continue,
        IAnimationSpec<Color>? defaultAnimationSpec = null,
        Optional<Color> visibilityThreshold = default, string? label = null)
        => new(scope, initialValue, SharedColorConverter, commitPhase, interruptionMode, defaultAnimationSpec, visibilityThreshold, label);
}

