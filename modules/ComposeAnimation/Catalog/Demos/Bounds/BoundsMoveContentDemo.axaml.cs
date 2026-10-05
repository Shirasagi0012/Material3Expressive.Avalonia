using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Bounds;

/// <summary>Lookahead.MoveContent：同一个实例跨父级移动，保留在途动画的值与速度。</summary>
[Demo("bounds/move-content", Title = "跨容器移动", Page = "布局动画", PageOrder = 3, Order = 6, Experimental = true)]
public partial class BoundsMoveContentDemo : DemoControlBase
{
    private bool _right;
    private bool _wide;

    public BoundsMoveContentDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddBoundsDiagnostics(() => Card.CurrentBounds, () => Card.TargetBounds);
    }

    private void MoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var from = _right ? Right : Left;
        var to = _right ? Left : Right;
        Lookahead.MoveContent(Card, () =>
        {
            from.Children.Remove(Card);
            to.Children.Add(Card);
        });
        _right = !_right;
    }

    private void ResizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _wide = !_wide;
        Card.TargetSize = _wide ? new Size(250, 120) : new Size(180, 180);
    }

    [DemoAction("MoveContent 跨父级移动同一个实例：保留在途动画，从旧父级位置飞向新目标并落位")]
    public async Task MovePreservesFlight()
    {
        await AdvanceAsync(1);
        await CaptureAsync("initial");
        Assert(!Card.IsRunning, "初始应静止");
        Assert(CurrentEqualsTarget(), "初始呈现应等于目标");
        Assert(Left.Children.Contains(Card), "初始卡片应在左容器");
        var startX = Card.CurrentBounds!.Value.X;

        Click(MoveButton);
        await AdvanceAsync(1); // 提交新目标：卡片从旧父级位置起飞
        Assert(Right.Children.Contains(Card), "移动后逻辑父级应为右容器");
        Assert(Card.IsRunning, "MoveContent 应保留并继续矩形动画");
        Assert(Near(Card.CurrentBounds!.Value.X, startX),
            "移动后的首个布局应从旧位置继续，不应瞬移");

        await AdvanceAsync(8);
        await CaptureAsync("mid");
        Assert(Card.IsRunning, "中途动画应在运行");
        Assert(!CurrentEqualsTarget(), "中途呈现应在飞行中");
        var midX = Card.CurrentBounds!.Value.X;
        var targetX = Card.TargetBounds!.Value.X;
        Assert(midX > startX && midX < targetX,
            $"中途呈现应位于起点与目标之间（起点 {startX:0.#} < 当前 {midX:0.#} < 目标 {targetX:0.#}）");

        await WaitForSettleAsync();
        await CaptureAsync("settled");
        Assert(!Card.IsRunning, "收敛后动画应停止");
        Assert(CurrentEqualsTarget(), "收敛后呈现应等于目标");
        Assert(Right.Children.Contains(Card), "收敛后卡片应落位在右容器");
    }

    // MoveContent 按边保留在途动画，收敛后矩形由边值重建，可能与目标差 1 ulp
    //（实测 Width=180.00000000000006 vs 180）；用 1e-6 容差表达"呈现收敛到目标"。
    private bool CurrentEqualsTarget()
        => Card.CurrentBounds is { } current && Card.TargetBounds is { } target &&
           NearlyEquals(current.X, target.X) && NearlyEquals(current.Y, target.Y) &&
           NearlyEquals(current.Width, target.Width) && NearlyEquals(current.Height, target.Height);

    private static bool NearlyEquals(double a, double b) => Math.Abs(a - b) < 1e-6;

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.5;

    private async Task WaitForSettleAsync()
    {
        for (var i = 0; i < 300 && Card.IsRunning; i++)
            await AdvanceAsync(3);
    }

}
