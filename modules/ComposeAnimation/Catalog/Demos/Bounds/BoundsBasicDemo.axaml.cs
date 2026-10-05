using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Bounds;

/// <summary>AnimatedBounds 基础：TargetSize 与对齐切换，布局槽与呈现位置的实时对比。</summary>
[Demo("bounds/basic", Title = "位置与尺寸", Page = "布局动画", PageOrder = 3, Order = 2, Experimental = true)]
public partial class BoundsBasicDemo : DemoControlBase
{
    private bool _expanded;

    public BoundsBasicDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddBoundsDiagnostics(() => Card.CurrentBounds, () => Card.TargetBounds);
    }

    private void ToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _expanded = !_expanded;
        Card.TargetSize = _expanded ? new Size(200, 120) : new Size(120, 80);
        Card.HorizontalAlignment = _expanded ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        Card.VerticalAlignment = _expanded ? VerticalAlignment.Bottom : VerticalAlignment.Top;
    }

    [DemoAction("切换 TargetSize 与对齐：布局槽立即到位，呈现矩形逐帧逼近并收敛到目标")]
    public async Task ResizeAndRealign()
    {
        await AdvanceAsync(1); // 时钟兜底注入后的首个布局：发布几何
        await CaptureAsync("initial");
        Assert(!Card.IsRunning, "初始应静止（首次布局直接在目标处初始化）");
        Assert(CurrentEqualsTarget(), "初始呈现应等于目标");

        Click(ToggleButton); // 200×120 + Right/Bottom
        await AdvanceAsync(10);
        await CaptureAsync("mid");
        Assert(Card.IsRunning, "目标变更后动画应在运行");
        Assert(!CurrentEqualsTarget(), "动画中途呈现应尚未到达目标");
        Assert(Near(Card.Bounds.Width, 200) && Near(Card.Bounds.Height, 120),
            $"布局槽应立即按目标排布（实际 {Card.Bounds.Width:0.##}×{Card.Bounds.Height:0.##}）");

        await WaitForSettleAsync();
        await CaptureAsync("settled");
        Assert(!Card.IsRunning, "收敛后动画应停止");
        Assert(CurrentEqualsTarget(),
            $"收敛后呈现应等于目标（呈现 {Format(Card.CurrentBounds)}，目标 {Format(Card.TargetBounds)}，{Delta()}）");
        var target = Card.TargetBounds!.Value;
        Assert(Near(target.Width, 200) && Near(target.Height, 120),
            $"目标尺寸应为 200×120（实际 {target.Width:0.##}×{target.Height:0.##}）");
    }

    private bool CurrentEqualsTarget()
        // 收敛后的呈现由边值重建矩形，可能有 1 ulp（~1e-14）浮点残差，用 Near 容差比较。
        => Card.CurrentBounds is { } current && Card.TargetBounds is { } target
           && Near(current.X, target.X) && Near(current.Y, target.Y)
           && Near(current.Width, target.Width) && Near(current.Height, target.Height);

    private string Delta()
        => Card.CurrentBounds is { } c && Card.TargetBounds is { } t
            ? $"Δx={c.X - t.X:R} Δy={c.Y - t.Y:R} Δw={c.Width - t.Width:R} Δh={c.Height - t.Height:R}"
            : "无矩形";

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.01;

    private async Task WaitForSettleAsync()
    {
        for (var i = 0; i < 300 && Card.IsRunning; i++)
            await AdvanceAsync(3);
    }

    private static string Format(Rect? rect)
        => rect is { } r ? $"({r.X:0.#}, {r.Y:0.#}) {r.Width:0.#}×{r.Height:0.#}" : "—";
}
