using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Catalog.Framework;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Engine;

/// <summary>
/// InfiniteTransition 与 Transition 在同一个 Scope 下，速度经 AnimationScope.DurationScale 调节（速度 = 1 / 缩放）。
/// 缩放变化时两者都沿用上游语义：InfiniteTransition 从初值重新开始；Transition 按新缩放从起点重算播放时间。
/// </summary>
[Demo("engine/infinite-transition", Title = "无限过渡与速度", Page = "时间控制", PageOrder = 5, Order = 1)]
public sealed class InfiniteTransitionDemo : DemoControlBase
{
    private static readonly TimeSpan Period = TimeSpan.FromMilliseconds(2000);
    private static readonly TimeSpan OneShot = TimeSpan.FromMilliseconds(1000);
    private readonly Border _stage;
    private readonly Border _spinner = new() { Width = 60, Height = 60, Background = Brushes.MediumPurple, RenderTransform = new RotateTransform() };
    private readonly Border _bar = new() { Height = 12, Width = 0, Background = Brushes.SeaGreen, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly ComboBox _speed = new() { ItemsSource = new[] { "0.5×", "1×", "3×" }, SelectedIndex = 1 };
    private readonly Button _toggle = new() { Content = "切换单次过渡" };
    private InfiniteTransition? _infinite;
    private InfiniteTransition.TransitionAnimationState<double>? _angle;
    private Transition<bool>? _transition;
    private Transition<bool>.TransitionAnimationState<double>? _width;
    private AnimationChildScope? _run;

    public InfiniteTransitionDemo(DemoContext context) : base(context)
    {
        _stage = new Border
        {
            Classes = { "demo-stage" }, Height = 180, MinWidth = 420,
            Child = new StackPanel { Spacing = 24, Margin = new Thickness(24), Children = { _spinner, new Border { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Child = _bar } } },
        };
        _speed.SelectionChanged += (_, _) => ApplySpeed();
        _toggle.Click += (_, _) => _transition?.UpdateTarget(!_transition.TargetState);
        Content = new StackPanel
        {
            Classes = { "demo-root" }, VerticalAlignment = VerticalAlignment.Top, Spacing = 12,
            Children = { _stage, new StackPanel { Classes = { "demo-controls" }, Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "速度", VerticalAlignment = VerticalAlignment.Center }, _speed, _toggle } } },
        };
        _stage.AttachedToVisualTree += (_, _) => Start();
        _stage.DetachedFromVisualTree += (_, _) => Stop();
        AddDiagnostic("旋转角", () => _angle?.Value, unit: "°");
        AddDiagnostic("条宽", () => _width?.Value, () => _transition?.TargetState == true ? 300 : 0, "DIP");
        AddDiagnostic("DurationScale", () => AnimationScope.GetDurationScale(_stage));
    }

    private static double ScaleFor(int index) => index switch { 0 => 2.0, 2 => 1.0 / 3, _ => 1.0 };

    private void ApplySpeed() => AnimationScope.SetDurationScale(_stage, ScaleFor(_speed.SelectedIndex));

    private void Start()
    {
        Stop();
        var scope = AnimationScope.Get(_stage).CreateChild();
        _run = scope;
        _infinite = new InfiniteTransition("Spinner");
        _angle = _infinite.AnimateDouble(0, 360, new InfiniteRepeatableSpec<double>(new TweenSpec<double>(Period, easing: new LinearEasing())));
        _transition = new Transition<bool>(false, "Bar");
        _width = _transition.AnimateDouble(s => s ? 300 : 0, _ => new TweenSpec<double>(OneShot, easing: new LinearEasing()));
        scope.SubscribeValue(_angle, v => ((RotateTransform)_spinner.RenderTransform!).Angle = v);
        scope.SubscribeValue(_width, v => _bar.Width = v);
        var infinite = _infinite;
        var transition = _transition;
        _ = infinite.RunAsync(scope);
        _ = transition.RunAsync(scope);
    }

    private void Stop()
    {
        _run?.Dispose();
        _run = null;
        _infinite?.Dispose();
        _transition?.Dispose();
        _infinite = null;
        _transition = null;
    }

    private async Task ResetAsync(int speedIndex = 1)
    {
        _speed.SelectedIndex = speedIndex;
        ApplySpeed();
        Start();
        await AdvanceAsync(1);
    }

    [DemoAction("速度 0.5×/1×/3×：固定帧数后的角度与速度成正比")]
    public async Task SpeedScalesAngle()
    {
        var angles = new double[3];
        for (var i = 0; i < 3; i++)
        {
            await ResetAsync(i);
            await AdvanceAsync(30);
            angles[i] = _angle!.Value;
            await CaptureAsync($"speed-{i}");
        }
        // 1× 下 30 帧 ≈ 500 ms ≈ 90°；允许一帧误差（3°×速度）。
        Assert(Math.Abs(angles[1] - 90) <= 3.5, $"1× 30 帧应约 90°（实际 {angles[1]:0.##}）");
        Assert(Math.Abs(angles[0] - 45) <= 2, $"0.5× 应约 45°（实际 {angles[0]:0.##}）");
        Assert(Math.Abs(angles[2] - 270) <= 10.5, $"3× 应约 270°（实际 {angles[2]:0.##}）");
    }

    [DemoAction("运行中改速度：InfiniteTransition 从初值重新开始，Transition 按新缩放从起点重算")]
    public async Task SpeedChangeRestarts()
    {
        await ResetAsync(1);
        _transition!.UpdateTarget(true);
        await AdvanceAsync(30);
        var angleBefore = _angle!.Value;
        var widthBefore = _width!.Value;
        Assert(angleBefore > 60 && widthBefore > 100, $"改速前应已推进（{angleBefore:0.##}°，{widthBefore:0.##}）");
        _speed.SelectedIndex = 0; // 0.5×，缩放 2
        await AdvanceAsync(2);
        await CaptureAsync("after-change");
        var angleAfter = _angle.Value;
        var widthAfter = _width.Value;
        Assert(angleAfter < 5, $"InfiniteTransition 应从 0° 重新开始（实际 {angleAfter:0.##}）");
        // Transition：播放时间 = 自起点的经过时间 / 新缩放，约为改速前的一半。
        Assert(Math.Abs(widthAfter - widthBefore / 2) <= 6, $"Transition 应按新缩放重算（改前 {widthBefore:0.##}，改后 {widthAfter:0.##}）");
        await AdvanceAsync(30);
        Assert(_angle.Value > angleAfter, "重新开始后应继续旋转");
    }
}
