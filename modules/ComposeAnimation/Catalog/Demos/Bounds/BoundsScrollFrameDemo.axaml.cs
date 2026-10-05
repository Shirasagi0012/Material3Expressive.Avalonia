using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Bounds;

/// <summary>滚动参照系：AnimateMotionFrameOfReference 开/关的并排对比。</summary>
[Demo("bounds/scroll-frame", Title = "滚动跟随", Page = "布局动画", PageOrder = 3, Order = 5, Experimental = true)]
public partial class BoundsScrollFrameDemo : DemoControlBase
{
    private bool _expanded;

    public BoundsScrollFrameDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddBoundsDiagnostics(() => FollowCard.CurrentBounds, () => FollowCard.TargetBounds, "立即跟随 · ");
        AddBoundsDiagnostics(() => ChaseCard.CurrentBounds, () => ChaseCard.TargetBounds, "动画跟随 · ");
    }

    private void ScrollButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FollowViewport.Offset += new Vector(0, 60);
        ChaseViewport.Offset += new Vector(0, 60);
    }

    private void ResetButton_OnClick(object? sender, RoutedEventArgs e)
        => FollowViewport.Offset = ChaseViewport.Offset = default;

    private void TargetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _expanded = !_expanded;
        FollowCard.TargetSize = ChaseCard.TargetSize = _expanded ? new Size(260, 110) : new Size(150, 180);
        FollowCard.HorizontalAlignment = ChaseCard.HorizontalAlignment =
            _expanded ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    [DemoAction("滚动：默认卡片立即跟随且不动画，并入卡片重定向动画并追上；布局自身的变化两种模式都动画")]
    public async Task ScrollRedirectsAnimation()
    {
        await AdvanceAsync(1);
        await CaptureAsync("initial");
        Assert(!FollowCard.IsRunning && !ChaseCard.IsRunning, "初始两张卡片都应静止");
        Assert(CurrentEqualsTarget(FollowCard) && CurrentEqualsTarget(ChaseCard), "初始呈现应等于目标");
        var followY0 = FollowCard.CurrentBounds!.Value.Y;
        var chaseY0 = ChaseCard.CurrentBounds!.Value.Y;

        Click(ScrollButton); // 两侧 Offset += (0, 60)
        await AdvanceAsync(3);
        await CaptureAsync("scrolled");
        Assert(!FollowCard.IsRunning, "默认模式：滚动立即跟随，不启动动画");
        Assert(Near(FollowCard.CurrentBounds!.Value.Y, followY0 - 60),
            $"默认卡片应立即随滚动上移 60（实际 ΔY={FollowCard.CurrentBounds.Value.Y - followY0:0.##}）");
        Assert(CurrentEqualsTarget(FollowCard), "默认卡片呈现应始终等于目标");
        Assert(ChaseCard.IsRunning, "并入模式：滚动应重定向动画");
        Assert(!CurrentEqualsTarget(ChaseCard), "并入卡片中途应尚未追上目标");

        await WaitForSettleAsync(ChaseCard);
        await CaptureAsync("chased");
        Assert(CurrentEqualsTarget(ChaseCard), "并入卡片收敛后呈现应等于目标");
        Assert(Near(ChaseCard.CurrentBounds!.Value.Y, chaseY0 - 60),
            $"并入卡片收敛后也应上移 60（实际 ΔY={ChaseCard.CurrentBounds.Value.Y - chaseY0:0.##}）");

        Click(TargetButton); // 两卡同时：150×180 → 260×110，Left → Right
        await AdvanceAsync(10);
        await CaptureAsync("mid");
        Assert(FollowCard.IsRunning && ChaseCard.IsRunning, "布局自身的变化两种模式都应动画");
        Assert(!CurrentEqualsTarget(FollowCard) && !CurrentEqualsTarget(ChaseCard),
            "目标变更中途呈现应尚未到达目标");

        await WaitForSettleAsync(FollowCard);
        await WaitForSettleAsync(ChaseCard);
        await CaptureAsync("settled");
        Assert(!FollowCard.IsRunning && !ChaseCard.IsRunning, "收敛后两张卡片都应停止");
        Assert(CurrentEqualsTarget(FollowCard) && CurrentEqualsTarget(ChaseCard), "收敛后呈现应等于目标");
    }

    private static bool CurrentEqualsTarget(AnimatedBounds card)
        // 收敛后的呈现由边值重建矩形，可能有 1 ulp（~1e-14）浮点残差，用 Near 容差比较。
        => card.CurrentBounds is { } current && card.TargetBounds is { } target
           && Near(current.X, target.X) && Near(current.Y, target.Y)
           && Near(current.Width, target.Width) && Near(current.Height, target.Height);

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.5;

    private async Task WaitForSettleAsync(AnimatedBounds card)
    {
        for (var i = 0; i < 300 && card.IsRunning; i++)
            await AdvanceAsync(3);
    }

}
