using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class PathBezierPage : MotionPageBase
{
    private readonly PointVectorConverter _converter = new();
    private readonly Animatable<Point> _path = new(new Point(20, 40), new PointVectorConverter());
    private readonly Animatable<Point> _bezier = new(new Point(20, 200), new PointVectorConverter());

    public PathBezierPage() => InitializeComponent();
    protected override void OnAttached()
    {
        BindValue(_path, value => ApplyPosition(PathBox, value));
        BindValue(_bezier, value => ApplyPosition(BezierBox, value));
    }
    private void OnPath(object? sender, RoutedEventArgs e)
    {
        _path.SnapTo(new Point(20, 40));
        _path.AnimateTo(new Point(600, 40), new PathSpec<Point>(_converter, TimeSpan.FromSeconds(1.4), [new Point(180, 40), new Point(300, 180), new Point(440, 70), new Point(600, 40)], [Easings.EaseOut, Easings.FastOutSlowIn, Easings.EaseIn, Easings.EaseOut]));
        Status.Text = "segmented path";
        RequestFrame();
    }
    private void OnBezier(object? sender, RoutedEventArgs e)
    {
        _bezier.SnapTo(new Point(20, 200));
        _bezier.AnimateTo(new Point(600, 200), new CubicBezierSpec<Point>(_converter, TimeSpan.FromSeconds(1.4), new Point(180, 20), new Point(430, 280), Easings.FastOutSlowIn));
        Status.Text = "cubic Bézier";
        RequestFrame();
    }
    private static void ApplyPosition(Control control, Point value) { Canvas.SetLeft(control, value.X); Canvas.SetTop(control, value.Y); }
}
