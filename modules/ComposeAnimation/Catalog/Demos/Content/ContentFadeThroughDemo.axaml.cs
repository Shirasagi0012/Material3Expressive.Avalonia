using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Content;

/// <summary>移植自 Scratchpad/AnimatedContentDemo 的 Pages 段：默认 fade-through 内容切换。</summary>
[Demo("content/fade-through", Title = "淡出后淡入", Page = "内容切换", PageOrder = 2, Order = 0)]
public partial class ContentFadeThroughDemo : DemoControlBase
{
    public ContentFadeThroughDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("内容数量", () => Pages.VisibleSlotCount, () => 1);
        AddDiagnostic("尺寸", () => Pages.Bounds.Size, unit: "DIP");
    }

    private void FooButton_OnClick(object? sender, RoutedEventArgs e)
        => Pages.Content = Card("Foo", 200, 120);

    private void BarButton_OnClick(object? sender, RoutedEventArgs e)
        => Pages.Content = Card("Bar", 80, 80);

    private void BazButton_OnClick(object? sender, RoutedEventArgs e)
        => Pages.Content = Card("Baz", 140, 40);

    private static Border Card(string name, double w, double h) => new()
    {
        Width = w,
        Height = h,
        Classes = { "demo-actor" },
        CornerRadius = new Avalonia.CornerRadius(8),
        Tag = name,
        Child = new TextBlock
        {
            Text = name,
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        },
    };

    [DemoAction("切换到 Bar 卡片：中途两槽重叠，收敛后仅剩新内容")]
    public async Task SwitchContent()
    {
        await CaptureAsync("initial");
        await WaitForIdleAsync();
        Assert(Pages.IsIdle && Pages.VisibleSlotCount == 1,
            $"初始应空闲且只有一个槽位（IsIdle={Pages.IsIdle}, slots={Pages.VisibleSlotCount}）");

        Click(BarButton);
        await AdvanceAsync(2);
        await CaptureAsync("overlapping");
        Assert(Pages.VisibleSlotCount == 2, "退出动画播放期间应有两个槽位重叠");
        Assert(!Pages.IsIdle, "过渡进行中不应空闲");

        await WaitForIdleAsync();
        await CaptureAsync("settled");
        Assert(Pages.IsIdle, "收敛后应空闲");
        Assert(Pages.VisibleSlotCount == 1, "收敛后应只剩一个槽位");
        Assert(Pages.TargetContent is Border { Tag: "Bar" }, "收敛后目标内容应为 Bar 卡片");
    }

    private async Task WaitForIdleAsync()
    {
        for (var i = 0; i < 600 && !Pages.IsIdle; i++)
            await AdvanceAsync(1);
    }
}
