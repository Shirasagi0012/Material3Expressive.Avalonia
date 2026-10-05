using Avalonia;
using Avalonia.Controls;
using Catalog.Framework;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Visibility;

/// <summary>移植自 ComposeAnimation.Test/AnimatedVisibilityTest.cs 的 CustomAnimation 模式：ScopeCreated 里 AnimateCornerRadius + Scope.Bind，退出时圆角 4 → 24。</summary>
[Demo("visibility/scope-custom", Title = "自定义属性", Page = "显隐过渡", PageOrder = 1, Order = 3)]
public partial class VisibilityScopeCustomDemo : DemoControlBase
{
    public VisibilityScopeCustomDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("圆角", () => Card.CornerRadius.TopLeft, () => Visibility.TargetVisible ? 4 : 24, "DIP");
        AddDiagnostic("内容存在", () => Visibility.IsContentPresent, () => Visibility.TargetVisible);
        Visibility.ScopeCreated += (_, e) =>
        {
            var radius = e.Scope.Transition.AnimateCornerRadius(
                state => new CornerRadius(state == EnterExitState.PostExit ? 24 : 4),
                _ => new TweenSpec<CornerRadius>(TimeSpan.FromMilliseconds(3000)));
            e.Scope.Bind(Card, Border.CornerRadiusProperty, radius);
        };
    }

    [DemoAction("隐藏并验证圆角从 4 动画到 24、与淡出同步完成，结束后绑定释放")]
    public async Task HideAnimatesCornerRadius()
    {
        await CaptureAsync("visible");
        Assert(Math.Abs(Card.CornerRadius.TopLeft - 4) < 0.01,
            $"初始圆角应为 4（当前 {Card.CornerRadius.TopLeft:F2}）");

        VisibilityToggle.IsChecked = false;
        // 圆角补间为 3000ms：推进到时间中点（~1.5s）附近，容忍数帧的启动调度延迟
        await AdvanceAsync(90);
        await CaptureAsync("rounding");
        Assert(Visibility.IsContentPresent, "退出动画播放期间内容应保持存活");
        Assert(Card.CornerRadius.TopLeft > 4.5 && Card.CornerRadius.TopLeft < 23.5,
            $"退出中途圆角应介于 4 与 24 之间（当前 {Card.CornerRadius.TopLeft:F2}）");

        await WaitForIdleAsync();
        await CaptureAsync("hidden");
        Assert(!Visibility.IsContentPresent, "退出结束后内容应被移除");
        Assert(Math.Abs(Card.CornerRadius.TopLeft - 4) < 0.01,
            $"呈现结束后作用域绑定释放，圆角应恢复本地值 4（当前 {Card.CornerRadius.TopLeft:F2}）");
    }

    private async Task WaitForIdleAsync()
    {
        for (var i = 0; i < 600 && !Visibility.IsIdle; i++)
            await AdvanceAsync(1);
    }

}
