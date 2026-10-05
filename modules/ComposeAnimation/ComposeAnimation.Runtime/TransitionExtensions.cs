namespace ComposeAnimation.Runtime;

public static class TransitionExtensions
{
    public static Transition<TState>.TransitionAnimationState<double> AnimateDouble<TState>(
        this Transition<TState> transition, Func<TState, double> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<double>>? transitionSpec = null,
        string label = "DoubleAnimation")
        => transition.AnimateValue(new DoubleConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<float> AnimateFloat<TState>(
        this Transition<TState> transition, Func<TState, float> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<float>>? transitionSpec = null,
        string label = "FloatAnimation")
        => transition.AnimateValue(new FloatConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<int> AnimateInt<TState>(
        this Transition<TState> transition, Func<TState, int> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<int>>? transitionSpec = null,
        string label = "IntAnimation")
        => transition.AnimateValue(new Int32Converter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.Point> AnimatePoint<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.Point> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.Point>>? transitionSpec = null,
        string label = "PointAnimation")
        => transition.AnimateValue(new PointConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.Vector> AnimateVector<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.Vector> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.Vector>>? transitionSpec = null,
        string label = "VectorAnimation")
        => transition.AnimateValue(new VectorConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.Size> AnimateSize<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.Size> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.Size>>? transitionSpec = null,
        string label = "SizeAnimation")
        => transition.AnimateValue(new SizeConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.Rect> AnimateRect<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.Rect> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.Rect>>? transitionSpec = null,
        string label = "RectAnimation")
        => transition.AnimateValue(new RectConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.Thickness> AnimateThickness<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.Thickness> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.Thickness>>? transitionSpec = null,
        string label = "ThicknessAnimation")
        => transition.AnimateValue(new ThicknessConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.CornerRadius> AnimateCornerRadius<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.CornerRadius> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.CornerRadius>>? transitionSpec = null,
        string label = "CornerRadiusAnimation")
        => transition.AnimateValue(new CornerRadiusConverter(), targetValueByState, transitionSpec, label);

    public static Transition<TState>.TransitionAnimationState<Avalonia.Media.Color> AnimateColor<TState>(
        this Transition<TState> transition, Func<TState, Avalonia.Media.Color> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<Avalonia.Media.Color>>? transitionSpec = null,
        string label = "ColorAnimation")
        => transition.AnimateValue(new ColorConverter(), targetValueByState, transitionSpec, label);

}
