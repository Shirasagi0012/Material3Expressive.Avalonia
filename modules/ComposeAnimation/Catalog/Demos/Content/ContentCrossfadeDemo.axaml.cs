using Avalonia.Controls;
using Avalonia.Interactivity;
using Catalog.Framework;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Content;

/// <summary>移植自 Scratchpad/AnimatedContentDemo 的 Crossfade 段：同时淡入淡出切换。</summary>
[Demo("content/crossfade", Title = "交叉淡化", Page = "内容切换", PageOrder = 2, Order = 2)]
public partial class ContentCrossfadeDemo : DemoControlBase
{
    public ContentCrossfadeDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("300 ms 内容数量", () => Fader.VisibleSlotCount, () => 1);
        AddDiagnostic("500 ms 内容数量", () => SlowFader.VisibleSlotCount, () => 1);
        SlowFader.Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(500));
    }

    private void AlphaButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Fader.Content = Label("Alpha");
        SlowFader.Content = Label("Alpha");
    }

    private void BetaButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Fader.Content = Label("Beta");
        SlowFader.Content = Label("Beta");
    }

    private static TextBlock Label(string text) => new() { FontSize = 24, Text = text };

    [DemoAction("切换到 Beta：中途双内容叠加，默认规格先收敛、500ms 对照后收敛")]
    public async Task SwitchContent()
    {
        await CaptureAsync("initial");
        await WaitForIdleAsync(Fader);
        await WaitForIdleAsync(SlowFader);
        Assert(Fader.IsIdle && SlowFader.IsIdle,
            $"初始应空闲（Fader={Fader.IsIdle}, SlowFader={SlowFader.IsIdle}）");

        Click(BetaButton);
        await AdvanceAsync(6);
        await CaptureAsync("crossfading");
        Assert(Fader.VisibleSlotCount == 2, "默认 Crossfade 中途应有两个槽位叠加");
        Assert(SlowFader.VisibleSlotCount == 2, "500ms Crossfade 中途应有两个槽位叠加");

        await WaitForIdleAsync(Fader);
        Assert(Fader.IsIdle && Fader.VisibleSlotCount == 1, "默认 Crossfade 应先收敛");
        Assert(SlowFader.VisibleSlotCount == 2, "默认规格收敛时 500ms 对照应仍在过渡");

        await WaitForIdleAsync(SlowFader);
        await CaptureAsync("settled");
        Assert(SlowFader.IsIdle && SlowFader.VisibleSlotCount == 1, "500ms Crossfade 收敛后应只剩一个槽位");
        Assert(Fader.TargetContent is TextBlock { Text: "Beta" }, "收敛后目标内容应为 Beta");
    }

    private async Task WaitForIdleAsync(ComposeAnimation.AnimatedContent host)
    {
        for (var i = 0; i < 600 && !host.IsIdle; i++)
            await AdvanceAsync(1);
    }
}
