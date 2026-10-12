using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Bounds;

/// <summary>LookaheadBoundary 包裹普通内容：目标布局保持、呈现动画、输入命中跟随呈现。</summary>
[Demo("bounds/boundary", Title = "内容裁剪", Page = "布局动画", PageOrder = 3, Order = 4, Experimental = true)]
public partial class BoundsBoundaryDemo : DemoControlBase
{
    private bool _expanded;
    private int _liveClicks;

    public BoundsBoundaryDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddBoundsDiagnostics(() => Card.CurrentBounds, () => Card.TargetBounds);
    }

    private void ToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _expanded = !_expanded;
        Card.TargetSize = _expanded ? new Size(420, 220) : new Size(170, 110);
        Card.HorizontalAlignment = _expanded ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        Card.VerticalAlignment = _expanded ? VerticalAlignment.Bottom : VerticalAlignment.Top;
    }

    private void LiveButton_OnClick(object? sender, RoutedEventArgs e)
        => LiveButton.Content = $"点击 {++_liveClicks}";

    [DemoAction("展开时布局槽立即到位、内容保持目标布局；输入命中跟随呈现位置，呈现框外被 Boundary 裁剪")]
    public async Task ExpandWithLiveContent()
    {
        await AdvanceAsync(1);
        await CaptureAsync("initial");
        Assert(!Card.IsRunning, "初始应静止");
        Assert(CurrentEqualsTarget(), "初始呈现应等于目标");
        Assert(await HitLiveButtonAtPresentationAsync(), "静止时命中按钮呈现位置应命中按钮");

        Click(ToggleButton); // 420×220 + Right/Bottom
        await AdvanceAsync(8);
        await CaptureAsync("mid");
        Assert(Card.IsRunning, "目标变更后动画应在运行");
        Assert(!CurrentEqualsTarget(), "动画中途呈现应尚未到达目标");
        Assert(Near(Card.Bounds.Width, 420) && Near(Card.Bounds.Height, 220),
            $"布局槽应立即按目标排布（实际 {Card.Bounds.Width:0.##}×{Card.Bounds.Height:0.##}）");
        // 飞行中不命中测试呈现位置：Avalonia 12.1.2 的合成器 hit-test 对"呈现宿主被排布在本机槽位之外"
        // 的中间态不可靠（实测命中几何与布局几何不一致），改用布局坐标断言呈现已离开槽位。
        var current = Card.CurrentBounds!.Value;
        Assert(Math.Abs(current.X - Card.Bounds.X) > 20 || Math.Abs(current.Y - Card.Bounds.Y) > 20,
            $"飞行中呈现应已离开布局槽（呈现 {Format(current)}，槽 {Format(Card.Bounds)}）");
        Assert(await HitOutsideFrameMissesContentAsync(), "呈现框外的点应被 Boundary 裁剪，不命中内容");

        await WaitForSettleAsync();
        await CaptureAsync("settled");
        Assert(!Card.IsRunning, "收敛后动画应停止");
        Assert(CurrentEqualsTarget(), "收敛后呈现应等于目标");
        Assert(await HitLiveButtonAtPresentationAsync(), "收敛后命中按钮呈现位置应命中按钮");

        Click(LiveButton);
        Assert(_liveClicks == 1, "内容内的按钮应保持可交互");
    }

    // InputHitTest 走窗口合成器的 readback：要等真实渲染帧把当前布局提交、应用成可命中的几何。
    // ProbeFrameSource 不随真实帧推进，动画状态在轮询期间冻结，因此有上限地泵帧直到命中收敛是确定性的。
    private async Task<bool> HitLiveButtonAtPresentationAsync()
    {
        var position = LiveButton.TranslatePoint(default, this);
        Assert(position is not null, "按钮应能定位到 demo 坐标系");
        var point = position!.Value + new Vector(8, 8);
        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (IsLiveButton(this.InputHitTest(point))) return true;
            await PumpRenderFrameAsync();
        }
        return false;
    }

    private async Task<bool> HitOutsideFrameMissesContentAsync()
    {
        var topLeft = Boundary.TranslatePoint(default, this);
        Assert(topLeft is not null, "呈现框应能定位到 demo 坐标系");
        var frame = new Rect(topLeft!.Value, Boundary.Bounds.Size);
        var point = new Point(frame.Right + 6, frame.Top + 20);
        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (this.InputHitTest(point) is { } hit) return !IsLiveButton(hit);
            await PumpRenderFrameAsync();
        }
        return false;
    }

    private async Task PumpRenderFrameAsync()
    {
        if (TopLevel.GetTopLevel(this) is { } top)
        {
            var frame = new TaskCompletionSource();
            top.RequestAnimationFrame(_ => frame.SetResult());
            await frame.Task;
        }
        else await Task.Delay(16); // 未附加 TopLevel 时命中测试本就不可用，仅兜底
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private bool IsLiveButton(IInputElement? hit)
        => ReferenceEquals(hit, LiveButton) ||
           hit is Visual visual && visual.GetVisualAncestors().Contains(LiveButton);

    private bool CurrentEqualsTarget()
        // 收敛后的呈现由边值重建矩形，可能有 1 ulp（~1e-14）浮点残差，用 Near 容差比较。
        => Card.CurrentBounds is { } current && Card.TargetBounds is { } target
           && Near(current.X, target.X) && Near(current.Y, target.Y)
           && Near(current.Width, target.Width) && Near(current.Height, target.Height);

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.01;

    private async Task WaitForSettleAsync()
    {
        for (var i = 0; i < 300 && Card.IsRunning; i++)
            await AdvanceAsync(3);
    }

    private static string Format(Rect? rect)
        => rect is { } r ? $"({r.X:0.#}, {r.Y:0.#}) {r.Width:0.#}×{r.Height:0.#}" : "—";
}
