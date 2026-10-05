using Avalonia.Interactivity;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Content;

/// <summary>移植自 Scratchpad/AnimatedContentDemo 的 Count 段：方向感知的计数器滑动过渡。</summary>
[Demo("content/counter", Title = "方向感知计数器", Page = "内容切换", PageOrder = 2, Order = 1)]
public partial class ContentCounterDemo : DemoControlBase
{
    private int _count;

    public ContentCounterDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("内容数量", () => Counter.VisibleSlotCount, () => 1);
        Counter.Content = 0;
        Counter.TransitionSpec = scope =>
        {
            var up = scope.TargetState is int to && scope.InitialState is int from && to > from;
            return (new SlideInVertically { InitialOffsetRatio = up ? 1 : -1 }
                    + new FadeIn())
                .TogetherWith(new SlideOutVertically { TargetOffsetRatio = up ? -1 : 1 } + new FadeOut())
                .Using(new SizeTransform { Clip = true });
        };
    }

    private void IncrementButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Counter.Content = ++_count;
    }

    private void DecrementButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Counter.Content = --_count;
    }

    [DemoAction("递增：新数字从下方滑入、旧数字向上滑出")]
    public async Task CountUp()
    {
        await CaptureAsync("initial");
        await WaitForIdleAsync();
        Assert(Counter.IsIdle && Equals(Counter.TargetContent, 0),
            $"初始应显示 0 且空闲（IsIdle={Counter.IsIdle}, target={Counter.TargetContent}）");

        Click(IncrementButton);
        await AdvanceAsync(6);
        await CaptureAsync("sliding-up");
        Assert(Counter.VisibleSlotCount == 2, "滑动中途应有两个槽位");
        Assert(!Counter.IsIdle, "过渡进行中不应空闲");

        await WaitForIdleAsync();
        await CaptureAsync("settled");
        Assert(Counter.IsIdle, "收敛后应空闲");
        Assert(Counter.VisibleSlotCount == 1, "收敛后应只剩一个槽位");
        Assert(Equals(Counter.TargetContent, 1), "收敛后目标内容应为 1");
    }

    [DemoAction("递减：新数字从上方滑入、旧数字向下滑出")]
    public async Task CountDown()
    {
        await CaptureAsync("initial");
        await WaitForIdleAsync();
        Assert(Counter.IsIdle && Equals(Counter.TargetContent, 0),
            $"初始应显示 0 且空闲（IsIdle={Counter.IsIdle}, target={Counter.TargetContent}）");

        Click(DecrementButton);
        await AdvanceAsync(6);
        await CaptureAsync("sliding-down");
        Assert(Counter.VisibleSlotCount == 2, "滑动中途应有两个槽位");
        Assert(!Counter.IsIdle, "过渡进行中不应空闲");

        await WaitForIdleAsync();
        await CaptureAsync("settled");
        Assert(Counter.IsIdle, "收敛后应空闲");
        Assert(Counter.VisibleSlotCount == 1, "收敛后应只剩一个槽位");
        Assert(Equals(Counter.TargetContent, -1), "收敛后目标内容应为 -1");
    }

    private async Task WaitForIdleAsync()
    {
        for (var i = 0; i < 600 && !Counter.IsIdle; i++)
            await AdvanceAsync(1);
    }
}
