using Catalog.Framework;

namespace Catalog.Demos.Visibility;

/// <summary>移植自 Scratchpad/AnimatedVisibilityDemo 的主体：ToggleSwitch 驱动 Visible，进入 FadeIn+ExpandVertically、退出 FadeOut(350ms)+ShrinkVertically。</summary>
[Demo("visibility/basic", Title = "展开与收起", Page = "显隐过渡", PageOrder = 1, Order = 0)]
public partial class VisibilityBasicDemo : DemoControlBase
{
    public VisibilityBasicDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("内容存在", () => Visibility.IsContentPresent, () => Visibility.TargetVisible);
    }

    [DemoAction("隐藏再显示，验证退出/进入两个方向的存活语义")]
    public async Task HideAndShow()
    {
        await CaptureAsync("visible");
        Assert(Visibility.IsContentPresent, "初始应可见且内容存活");

        VisibilityToggle.IsChecked = false; // Visible = false，开始退出动画
        await AdvanceAsync(6);
        await CaptureAsync("exiting");
        Assert(Visibility.IsContentPresent, "退出动画播放期间内容应仍然存活");
        Assert(!Visibility.IsIdle, "退出动画播放期间宿主不应处于 Idle");

        await WaitForIdleAsync();
        await CaptureAsync("hidden");
        Assert(!Visibility.IsContentPresent, "退出结束后内容应被移除");

        VisibilityToggle.IsChecked = true; // Visible = true，开始进入动画
        await AdvanceAsync(6);
        await CaptureAsync("entering");
        Assert(Visibility.IsContentPresent, "进入动画播放期间内容应已存活");

        await WaitForIdleAsync();
        await CaptureAsync("shown");
        Assert(Visibility.IsContentPresent && Visibility.IsIdle, "进入结束后内容应存活且收敛到 Idle");
    }

    private async Task WaitForIdleAsync()
    {
        for (var i = 0; i < 600 && !Visibility.IsIdle; i++)
            await AdvanceAsync(1);
    }

}
