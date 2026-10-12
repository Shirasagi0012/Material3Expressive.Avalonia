using ComposeAnimation;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Catalog.Framework;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Engine;

/// <summary>
/// 动画规格画廊：同一目标位移经 7 种 <see cref="IAnimationSpec{T}"/> 驱动的
/// <see cref="Animatable{T}"/> 并排对比。新设计 demo，无迁移来源。
/// </summary>
[Demo("engine/spec-gallery", Title = "曲线与弹簧", Page = "基础动画", PageOrder = 0, Order = 1)]
public partial class SpecGalleryDemo : DemoControlBase
{
    private static readonly Point LinearTarget = new(320, 0);
    private static readonly Point ArcTarget = new(320, 60);
    private static readonly TimeSpan TweenDuration = TimeSpan.FromMilliseconds(1200);

    private readonly List<SpecRow> _rows = [];
    private IFrameSource? _clock;

    public SpecGalleryDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureInitialized();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // 取消各行动画的挂起帧等待；实例被自动化宿主替换或交互模式重启时走这里。
        _clock = null;
        _rows.Clear();
        base.OnDetachedFromVisualTree(e);
    }

    private void EnsureInitialized()
    {
        if (_clock is not null) return;

        // 引擎组件必须等基类 Clock 可用（控件附加到可视树后）再创建。
        // Scope 工厂和声明式属性动画共享同一个根帧请求。
        var clock = Motion;

        var keyframes = new KeyframesSpecConfig<Point> { Duration = TweenDuration };
        keyframes.At(0, new Point(0, 0)).Using(new CubicEaseIn());
        keyframes.At(500, new Point(240, 0)).Using(new CubicEaseOut());
        keyframes.At(900, new Point(280, 0));

        _rows.Add(SpecRow.Create("无回弹", BallSpringNoBounce, LinearTarget,
            new SpringSpec<Point>(Spring.DampingRatioNoBouncy, Spring.StiffnessLow, VisibilityThresholds.Point),
            clock));
        _rows.Add(SpecRow.Create("轻微回弹", BallSpringLow, LinearTarget,
            new SpringSpec<Point>(Spring.DampingRatioLowBouncy, Spring.StiffnessMediumLow, VisibilityThresholds.Point),
            clock));
        _rows.Add(SpecRow.Create("明显回弹", BallSpringHigh, LinearTarget,
            new SpringSpec<Point>(Spring.DampingRatioHighBouncy, Spring.StiffnessLow, VisibilityThresholds.Point),
            clock));
        _rows.Add(SpecRow.Create("线性", BallTweenLinear, LinearTarget,
            new TweenSpec<Point>(TweenDuration, easing: new LinearEasing()), clock));
        _rows.Add(SpecRow.Create("缓入缓出", BallTweenFosi, LinearTarget,
            new TweenSpec<Point>(TweenDuration, easing: ComposeEasings.FastOutSlowIn), clock));
        _rows.Add(SpecRow.Create("关键帧", BallKeyframes, LinearTarget,
            new KeyframesSpec<Point>(keyframes), clock));
        _rows.Add(SpecRow.Create("弧线", BallArc, ArcTarget,
            new ArcAnimationSpec<Point>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(900), easing: new LinearEasing()),
            clock));

        _clock = clock.Clock;
        Diagnostics.Clear();
        foreach (var row in _rows)
            AddDiagnostic(row.Name, () => row.Animatable.Value, () => row.Animatable.TargetValue, "DIP");
    }

    private void PlayAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        EnsureInitialized();
        foreach (var row in _rows) _ = Motion.Launch(_ => row.PlayAsync(Motion));
    }

    [DemoAction("统一播放：15 帧后各球位置互不相同且全部在途中；轮询收敛后全部到达终点")]
    public async Task PlayAndCompare()
    {
        EnsureInitialized();
        await CaptureAsync("initial");
        Assert(_rows.All(row => row.Animatable.Value == default && !row.Animatable.IsRunning),
            "初始时各行应静止在起点");

        Click(PlayAllButton);
        await AdvanceAsync(15);
        await CaptureAsync("mid");

        var mid = _rows.Select(row => row.Animatable.Value).ToArray();
        for (var i = 0; i < _rows.Count; i++)
            Assert(_rows[i].Animatable.IsRunning,
                $"15 帧时「{_rows[i].Name}」应仍在动画途中（当前 X={mid[i].X:F1}）");
        for (var i = 0; i < _rows.Count; i++)
        for (var j = i + 1; j < _rows.Count; j++)
            Assert(Math.Abs(mid[i].X - mid[j].X) > 3,
                $"15 帧时「{_rows[i].Name}」与「{_rows[j].Name}」的横向位置应明显不同（{mid[i].X:F1} vs {mid[j].X:F1}）");

        // 有上限轮询收敛：弹簧时长由物理决定（高回弹约 2.3s），写死帧数会漂移。
        for (var i = 0; i < 120 && _rows.Any(row => row.Animatable.IsRunning); i++)
            await AdvanceAsync(5);
        await CaptureAsync("settled");

        foreach (var row in _rows)
        {
            Assert(!row.Animatable.IsRunning, $"「{row.Name}」应已收敛");
            Assert(Math.Abs(row.Animatable.Value.X - row.Target.X) < 1 &&
                   Math.Abs(row.Animatable.Value.Y - row.Target.Y) < 1,
                $"「{row.Name}」应到达终点 ({row.Target.X:F0},{row.Target.Y:F0})（当前 {row.Animatable.Value}）");
        }
    }

    /// <summary>一行规格对比：一个 <see cref="Animatable{Point}"/> 驱动一颗小球的 <see cref="TranslateTransform"/>。</summary>
    private sealed class SpecRow
    {
        private readonly IAnimationSpec<Point> _spec;

        private SpecRow(string name, Animatable<Point> animatable, Point target, IAnimationSpec<Point> spec)
        {
            Name = name;
            Animatable = animatable;
            Target = target;
            _spec = spec;
        }

        public string Name { get; }

        public Animatable<Point> Animatable { get; }

        public Point Target { get; }

        public static SpecRow Create(string name, Border ball, Point target, IAnimationSpec<Point> spec,
            AnimationScope clock)
        {
            var animatable = ComposeAnimation.Runtime.Animatable.Create(new Point(), label: name);

            // 位移展示用渲染层 TranslateTransform，不触发布局。
            var transform = new TranslateTransform();
            ball.RenderTransform = transform;

            var row = new SpecRow(name, animatable, target, spec);
            clock.SubscribeValue(animatable, point =>
            {
                transform.X = point.X;
                transform.Y = point.Y;
            });
            return row;
        }

        public async Task PlayAsync(FrameScope scope)
        {
            try
            {
                await Animatable.AnimateToAsync(scope, Target, _spec);
            }
            catch (OperationCanceledException)
            {
                throw; // Scope 观察正常取消；保持返回任务的取消状态。
            }
        }

    }
}
