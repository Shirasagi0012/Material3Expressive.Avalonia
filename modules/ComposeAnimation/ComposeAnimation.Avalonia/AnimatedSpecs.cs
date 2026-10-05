using Avalonia;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public sealed class DoubleTweenSpec : TweenSpec<double>;
public sealed class DoubleSpringSpec() : SpringSpec<double>(visibilityThreshold: VisibilityThresholds.Double);

public sealed class FloatTweenSpec : TweenSpec<float>;
public sealed class FloatSpringSpec() : SpringSpec<float>(visibilityThreshold: VisibilityThresholds.Float);

public sealed class Int32TweenSpec : TweenSpec<int>;
public sealed class Int32SpringSpec() : SpringSpec<int>(visibilityThreshold: VisibilityThresholds.Int32);

public sealed class PointTweenSpec : TweenSpec<Point>;
public sealed class PointSpringSpec() : SpringSpec<Point>(visibilityThreshold: VisibilityThresholds.Point);

public sealed class SizeTweenSpec : TweenSpec<Size>;
public sealed class SizeSpringSpec() : SpringSpec<Size>(visibilityThreshold: VisibilityThresholds.Size);

public sealed class VectorTweenSpec : TweenSpec<Vector>;
public sealed class VectorSpringSpec() : SpringSpec<Vector>(visibilityThreshold: VisibilityThresholds.Vector);

public sealed class RectTweenSpec : TweenSpec<Rect>;
public sealed class RectSpringSpec() : SpringSpec<Rect>(visibilityThreshold: VisibilityThresholds.Rect);

public sealed class ThicknessTweenSpec : TweenSpec<Thickness>;
public sealed class ThicknessSpringSpec() : SpringSpec<Thickness>(visibilityThreshold: VisibilityThresholds.Thickness);

public sealed class CornerRadiusTweenSpec : TweenSpec<CornerRadius>;
public sealed class CornerRadiusSpringSpec() : SpringSpec<CornerRadius>(visibilityThreshold: VisibilityThresholds.CornerRadius);

public sealed class ColorTweenSpec : TweenSpec<Color>;
public sealed class ColorSpringSpec() : SpringSpec<Color>;
