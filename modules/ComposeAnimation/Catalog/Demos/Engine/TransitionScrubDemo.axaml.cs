using ComposeAnimation;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Catalog.Framework;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Engine;

/// <summary>
/// Transition 手动进度：<see cref="SeekableTransitionState{T}"/> + <see cref="Transition{TState}"/>
/// 驱动一张卡片的高度 / 圆角 / 不透明度；Slider 拖动即手动 scrub，也可按时钟自动播放。
/// 新设计 demo，无迁移来源。
/// </summary>
[Demo("engine/transition-scrub", Title = "手动进度", Page = "时间控制", PageOrder = 5, Order = 0)]
public partial class TransitionScrubDemo : DemoControlBase
{
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(1000);

    private SeekableTransitionState<bool>? _state;
    private Transition<bool>? _transition;
    private bool _suppressSlider;
    private bool _playing;

    public TransitionScrubDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("进度", () => Openness * 100, () => 100, "%");
        AddDiagnostic("高度", () => Card.Height, () => 200, "DIP");
        AddDiagnostic("圆角", () => Card.CornerRadius.TopLeft, () => 24, "DIP");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureInitialized();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Dispose 会取消在途的 seek/播放操作（其任务以 OperationCanceledException 结束，已捕获）。
        _transition?.Dispose();
        _transition = null;
        _state = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void EnsureInitialized()
    {
        if (_state is not null) return;

        // 引擎组件必须等基类 Clock 可用（控件附加到可视树后）再创建。
        var state = new SeekableTransitionState<bool>(false);
        var transition = Motion.CreateTransition(state, label: "Scrub");

        // 注意：seekable 过渡自己驱动帧循环，不要对 transition 调 RunAsync；
        // 一个 SeekableTransitionState 实例也只能配置给这一个 Transition。
        var height = transition.AnimateDouble(s => s ? 200.0 : 80.0,
            _ => new TweenSpec<double>(Duration, easing: new LinearEasing()), label: "Height");
        var radius = transition.AnimateCornerRadius(s => s ? new CornerRadius(24) : new CornerRadius(4),
            _ => new TweenSpec<CornerRadius>(Duration, easing: new LinearEasing()), label: "Radius");
        var opacity = transition.AnimateDouble(s => s ? 1.0 : 0.4,
            _ => new TweenSpec<double>(Duration, easing: new LinearEasing()), label: "Opacity");

        Motion.BindAnimatedValue(Card, HeightProperty, height);
        Motion.BindAnimatedValue(Card, Border.CornerRadiusProperty, radius);
        Motion.BindAnimatedValue(Card, OpacityProperty, opacity);

        state.PropertyChanged += (_, _) => UpdateSlider();
        transition.PropertyChanged += (_, _) => UpdateSlider();

        _state = state;
        _transition = transition;
        UpdateSlider();
    }

    /// <summary>界面上呈现的「展开度」：seek/播放中按当前段方向折算，空闲时按当前状态取 0/1。</summary>
    private double Openness => _state is null || _transition is null
        ? 0
        : _transition.IsSeeking
            ? _state.TargetState ? _state.Fraction : 1 - _state.Fraction
            : _state.CurrentState ? 1 : 0;

    private void UpdateSlider()
    {
        if (_state is null || _transition is null) return;

        _suppressSlider = true;
        ScrubSlider.Value = Openness;
        _suppressSlider = false;
    }

    private void ScrubSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_suppressSlider || _state is null) return;
        _ = SeekCoreAsync(e.NewValue);
    }

    private void PlayButton_OnClick(object? sender, RoutedEventArgs e)
    {
        EnsureInitialized();
        _ = PlayCoreAsync();
    }

    private async Task SeekCoreAsync(double openness)
    {
        try
        {
            // 按拖动方向选择目标状态：上拖向 true 展开，下拖向 false 收起。
            if (openness >= Openness)
                await _state!.SeekToAsync(true, openness);
            else
                await _state!.SeekToAsync(false, 1 - openness);
        }
        catch (OperationCanceledException)
        {
            // 新的 seek/播放操作会打断正在等待帧的旧操作，是常态语义。
        }
    }

    private async Task PlayCoreAsync()
    {
        // Transition.IsRunning 在 seekable 播放期间会被逐帧 SeekAnimations 重置，
        // 不能作为「播放中」信号，因此自行跟踪。
        _playing = true;
        UpdateSlider();
        try
        {
            await _state!.AnimateToAsync(true);
        }
        catch (OperationCanceledException)
        {
            // 被拖动或重置打断。
        }
        finally
        {
            _playing = false;
            UpdateSlider();
        }
    }

    [DemoAction("手动 Seek 到一半后播放至终点：验证中间值、播放过程与收敛终态")]
    public async Task ScrubAndPlay()
    {
        EnsureInitialized();
        var state = _state!;
        var transition = _transition!;

        await CaptureAsync("initial");
        Assert(Math.Abs(Card.Height - 80) < 0.5 && Openness == 0, "初始应为收起态");

        // 模拟拖动：直接调用 state 的 SeekToAsync（与滑杆处理同一入口）。
        await state.SeekToAsync(true, 0.5);
        await CaptureAsync("half");
        Assert(Math.Abs(state.Fraction - 0.5) < 0.001,
            $"Seek 后 Fraction 应为 0.5（当前 {state.Fraction:F3}）");
        Assert(transition.IsSeeking, "Seek 后过渡应处于手动模式");
        Assert(Math.Abs(Card.Height - 140) < 1, $"半程高度应为 140（当前 {Card.Height:F2}）");
        Assert(Math.Abs(Card.CornerRadius.TopLeft - 14) < 1, $"半程圆角应为 14（当前 {Card.CornerRadius.TopLeft:F2}）");
        Assert(Math.Abs(Card.Opacity - 0.7) < 0.02, $"半程不透明度应为 0.7（当前 {Card.Opacity:F3}）");

        Click(PlayButton);
        await AdvanceAsync(10);
        await CaptureAsync("playing");
        Assert(_playing && transition.IsSeeking, "播放 10 帧后应仍在手动 Seek 时间线上推进");
        Assert(state.Fraction > 0.5 && state.Fraction < 1,
            $"播放中 Fraction 应在 0.5~1 之间（当前 {state.Fraction:F3}）");
        Assert(Card.Height > 140 && Card.Height < 200,
            $"播放中高度应在半程与终点之间（当前 {Card.Height:F2}）");

        // 有上限轮询收敛：0.5 → 1.0 需播完整个 1000ms 时间线（约 60 帧）；
        // 播放完成时过渡退出 Seek 模式（IsSeeking → false），以此判定收敛。
        for (var i = 0; i < 200 && transition.IsSeeking; i++)
            await AdvanceAsync(5);
        await CaptureAsync("settled");

        Assert(!_playing && !transition.IsSeeking, "播放结束后应退出手动模式并停止");
        Assert(state.CurrentState && state.TargetState, "播放结束后 Current/Target 应都为 true");
        Assert(Math.Abs(Card.Height - 200) < 0.5, $"终点高度应为 200（当前 {Card.Height:F2}）");
        Assert(Math.Abs(Card.CornerRadius.TopLeft - 24) < 0.5, $"终点圆角应为 24（当前 {Card.CornerRadius.TopLeft:F2}）");
        Assert(Math.Abs(Card.Opacity - 1) < 0.01, $"终点不透明度应为 1（当前 {Card.Opacity:F3}）");
    }
}
