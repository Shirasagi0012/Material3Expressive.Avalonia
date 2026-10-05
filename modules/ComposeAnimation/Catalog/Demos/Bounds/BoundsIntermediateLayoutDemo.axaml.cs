using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Bounds;

/// <summary>IntermediateLayout 组合顺序语义：FixedSize / Padding / Offset 的 Then 由外向内组合。</summary>
[Demo("bounds/intermediate-layout", Title = "布局组合", Page = "布局动画", PageOrder = 3, Order = 3, Experimental = true)]
public partial class BoundsIntermediateLayoutDemo : DemoControlBase
{
    private bool _paddingFirst;
    private bool _offset;

    public BoundsIntermediateLayoutDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddBoundsDiagnostics(() => Card.CurrentBounds, () => Card.TargetBounds);
        SetLayout();
    }

    private void OrderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _paddingFirst = !_paddingFirst;
        SetLayout();
    }

    private void OffsetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _offset = !_offset;
        SetLayout();
    }

    private void SetLayout()
    {
        var size = BoundsLayout.FixedSize(new Size(200, 160));
        var padding = BoundsLayout.Padding(new Thickness(20));
        Card.IntermediateLayout = (_paddingFirst ? padding.Then(size) : size.Then(padding))
            .Then(BoundsLayout.Offset(_offset ? new Vector(50, 20) : default));
        OrderButton.Content = _paddingFirst ? "内边距 → 尺寸" : "尺寸 → 内边距";
    }

    [DemoAction("切换组合顺序会改变外层目标尺寸并触发动画；内层 Offset 只驱动内框动画、不改变外框目标")]
    public async Task ReorderLayers()
    {
        await AdvanceAsync(1);
        await CaptureAsync("initial");
        Assert(!Card.IsRunning, "初始应静止");
        Assert(CurrentEqualsTarget(), "初始呈现应等于目标");
        Assert(TargetSizeIs(200, 160), "初始组合（尺寸→Padding）外层目标应为 200×160");

        Click(OrderButton); // Padding→尺寸：外层 240×200
        await AdvanceAsync(8);
        await CaptureAsync("mid");
        Assert(Card.IsRunning, "组合变化后动画应在运行");
        Assert(!CurrentEqualsTarget(), "动画中途呈现应尚未到达目标");

        await WaitForSettleAsync();
        await CaptureAsync("reordered");
        Assert(!Card.IsRunning, "收敛后动画应停止");
        Assert(CurrentEqualsTarget(), "收敛后呈现应等于目标");
        Assert(TargetSizeIs(240, 200), "Padding→尺寸组合外层目标应为 240×200");

        Click(OffsetButton); // 内层偏移 (50,20)：外框目标不变，只驱动内框动画
        await AdvanceAsync(8);
        await CaptureAsync("offset-mid");
        Assert(Card.IsRunning, "内层偏移应驱动内框动画");
        Assert(TargetSizeIs(240, 200), "Offset 不扩大占位，外层目标应保持 240×200");

        await WaitForSettleAsync();
        await CaptureAsync("offset-settled");
        Assert(!Card.IsRunning, "偏移动画收敛后应停止");
        Assert(CurrentEqualsTarget(), "收敛后呈现应等于目标");
        Assert(TargetSizeIs(240, 200), "偏移收敛后外层目标仍应为 240×200");
    }

    private bool CurrentEqualsTarget()
        // 收敛后的呈现由边值重建矩形，可能有 1 ulp（~1e-14）浮点残差，用 Near 容差比较。
        => Card.CurrentBounds is { } current && Card.TargetBounds is { } target
           && Near(current.X, target.X) && Near(current.Y, target.Y)
           && Near(current.Width, target.Width) && Near(current.Height, target.Height);

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.01;

    private bool TargetSizeIs(double width, double height)
        => Card.TargetBounds is { } target &&
           Math.Abs(target.Width - width) < 0.01 && Math.Abs(target.Height - height) < 0.01;

    private async Task WaitForSettleAsync()
    {
        for (var i = 0; i < 300 && Card.IsRunning; i++)
            await AdvanceAsync(3);
    }

}
