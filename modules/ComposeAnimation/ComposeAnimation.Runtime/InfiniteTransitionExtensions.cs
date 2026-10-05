namespace ComposeAnimation.Runtime;

public static class InfiniteTransitionExtensions
{
    public static InfiniteTransition.TransitionAnimationState<double> AnimateDouble(
        this InfiniteTransition transition,
        double initialValue,
        double targetValue,
        InfiniteRepeatableSpec<double> animationSpec,
        string label = "DoubleAnimation")
        => transition.AnimateValue(initialValue, targetValue, new DoubleConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<float> AnimateFloat(
        this InfiniteTransition transition,
        float initialValue,
        float targetValue,
        InfiniteRepeatableSpec<float> animationSpec,
        string label = "FloatAnimation")
        => transition.AnimateValue(initialValue, targetValue, new FloatConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<int> AnimateInt(
        this InfiniteTransition transition,
        int initialValue,
        int targetValue,
        InfiniteRepeatableSpec<int> animationSpec,
        string label = "IntAnimation")
        => transition.AnimateValue(initialValue, targetValue, new Int32Converter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.Point> AnimatePoint(
        this InfiniteTransition transition,
        Avalonia.Point initialValue,
        Avalonia.Point targetValue,
        InfiniteRepeatableSpec<Avalonia.Point> animationSpec,
        string label = "PointAnimation")
        => transition.AnimateValue(initialValue, targetValue, new PointConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.Vector> AnimateVector(
        this InfiniteTransition transition,
        Avalonia.Vector initialValue,
        Avalonia.Vector targetValue,
        InfiniteRepeatableSpec<Avalonia.Vector> animationSpec,
        string label = "VectorAnimation")
        => transition.AnimateValue(initialValue, targetValue, new VectorConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.Size> AnimateSize(
        this InfiniteTransition transition,
        Avalonia.Size initialValue,
        Avalonia.Size targetValue,
        InfiniteRepeatableSpec<Avalonia.Size> animationSpec,
        string label = "SizeAnimation")
        => transition.AnimateValue(initialValue, targetValue, new SizeConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.Rect> AnimateRect(
        this InfiniteTransition transition,
        Avalonia.Rect initialValue,
        Avalonia.Rect targetValue,
        InfiniteRepeatableSpec<Avalonia.Rect> animationSpec,
        string label = "RectAnimation")
        => transition.AnimateValue(initialValue, targetValue, new RectConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.Thickness> AnimateThickness(
        this InfiniteTransition transition,
        Avalonia.Thickness initialValue,
        Avalonia.Thickness targetValue,
        InfiniteRepeatableSpec<Avalonia.Thickness> animationSpec,
        string label = "ThicknessAnimation")
        => transition.AnimateValue(initialValue, targetValue, new ThicknessConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.CornerRadius> AnimateCornerRadius(
        this InfiniteTransition transition,
        Avalonia.CornerRadius initialValue,
        Avalonia.CornerRadius targetValue,
        InfiniteRepeatableSpec<Avalonia.CornerRadius> animationSpec,
        string label = "CornerRadiusAnimation")
        => transition.AnimateValue(initialValue, targetValue, new CornerRadiusConverter(), animationSpec, label);

    public static InfiniteTransition.TransitionAnimationState<Avalonia.Media.Color> AnimateColor(
        this InfiniteTransition transition,
        Avalonia.Media.Color initialValue,
        Avalonia.Media.Color targetValue,
        InfiniteRepeatableSpec<Avalonia.Media.Color> animationSpec,
        string label = "ColorAnimation")
        => transition.AnimateValue(initialValue, targetValue, new ColorConverter(), animationSpec, label);
}
