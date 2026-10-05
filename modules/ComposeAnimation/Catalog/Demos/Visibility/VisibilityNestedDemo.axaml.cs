using Catalog.Framework;

namespace Catalog.Demos.Visibility;

/// <summary>移植自 Scratchpad/AnimatedVisibilityDemo 的嵌套例子：AnimatedVisibility 容器内的 AnimateEnterExit 子级有独立的滑入/滑出时序，并延长宿主退出存活。</summary>
[Demo("visibility/nested", Title = "子级过渡", Page = "显隐过渡", PageOrder = 1, Order = 2)]
public partial class VisibilityNestedDemo : DemoControlBase
{
    public VisibilityNestedDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("内容存在", () => Visibility.IsContentPresent, () => Visibility.TargetVisible);
    }

    [DemoAction("隐藏容器：子级滑出(500ms)比父级淡出(350ms)更久，验证子级延长宿主存活")]
    public async Task HideWithChildTiming()
    {
        await CaptureAsync("visible");

        VisibilityToggle.IsChecked = false;
        await AdvanceAsync(15); // ~250ms：父级淡出/收起与子级滑出同时进行，时序差异可见
        await CaptureAsync("mid-exit");
        Assert(Visibility.IsContentPresent, "退出动画播放期间内容应保持存活");
        Assert(!Visibility.IsIdle, "退出动画播放期间宿主不应处于 Idle");

        await AdvanceAsync(11); // 累计 ~433ms：父级 350ms 淡出已结束，子级 500ms 滑出仍在进行
        await CaptureAsync("child-outlives-parent");
        Assert(Visibility.IsContentPresent, "父级淡出结束后子级仍在滑出，内容应仍然存活");

        await WaitForIdleAsync();
        await CaptureAsync("hidden");
        Assert(!Visibility.IsContentPresent, "全部动画结束后内容应被移除");
        Assert(Visibility.IsIdle, "全部动画结束后宿主应处于 Idle");
    }

    private async Task WaitForIdleAsync()
    {
        for (var i = 0; i < 600 && !Visibility.IsIdle; i++)
            await AdvanceAsync(1);
    }

}
