using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Popups;

/// <summary>
/// Flyout 里的 AnimatedVisibility：弹层内容的 Scope 以所有者控件的 Scope 为父（D15），
/// 与窗口共用同一帧时钟，并继承所有者上的 DurationScale；动画途中关闭弹层后重新打开仍正常完成；所有者 Scope 父子关系由 DurationScale 继承可见。
/// 内容 Scope 的父级身份与 AnimationFailed 计数只能经内部 API 观察，由单元测试覆盖
/// （AnimationScopeRebuildTest.FlyoutContent_ParentsToTheOwnerControlsScope 等）；本页只做公开可见的检查。
/// </summary>
[Demo("popup/flyout", Title = "弹层中的显隐", Page = "显隐过渡", PageOrder = 1, Order = 4)]
public sealed class PopupDemo : DemoControlBase
{
    private readonly AnimatedVisibility _visibility = new()
    {
        Visible = false,
        Content = new Border { Width = 180, Height = 90, Background = Brushes.SteelBlue, Child = new TextBlock { Text = "弹层内容", Margin = new Thickness(12), Foreground = Brushes.White } },
    };
    private readonly Button _owner;
    private readonly Flyout _flyout;
    private readonly Button _toggle = new() { Content = "切换显隐" };

    public PopupDemo(DemoContext context) : base(context)
    {
        _flyout = new Flyout { Content = new Border { Padding = new Thickness(8), MinWidth = 200, MinHeight = 110, Child = _visibility }, Placement = PlacementMode.Bottom };
        _owner = new Button { Content = "打开弹层", Flyout = _flyout };
        _toggle.Click += (_, _) => _visibility.Visible = !_visibility.Visible;
        Content = new StackPanel
        {
            Classes = { "demo-root" }, VerticalAlignment = VerticalAlignment.Top, Spacing = 12,
            Children =
            {
                new Border { Classes = { "demo-stage" }, Height = 220, MinWidth = 420, Child = new StackPanel { Margin = new Thickness(16), Children = { _owner } } },
                new StackPanel { Classes = { "demo-controls" }, Orientation = Orientation.Horizontal, Spacing = 8, Children = { _toggle } },
            },
        };
        AddDiagnostic("弹层已打开", () => _flyout.IsOpen ? 1 : 0);
        AddDiagnostic("显隐空闲", () => _visibility.IsIdle ? 1 : 0);
    }

    private async Task ResetAsync(double scale = 1)
    {
        _flyout.Hide();
        AnimationScope.SetDurationScale(_owner, scale);
        _visibility.Visible = false;
        await AdvanceAsync(2);
        for (var i = 0; i < 60 && !_visibility.IsIdle; i++) await AdvanceAsync(3);
    }

    private async Task OpenAsync()
    {
        _flyout.ShowAt(_owner);
        await AdvanceAsync(2);
        Assert(_flyout.IsOpen, "弹层应已打开");
    }

    [DemoAction("弹层打开后展开内容：内容 Scope 以所有者 Scope 为父，逐帧推进；途中关闭后重新打开仍正常完成")]
    public async Task OpenAnimateAndCloseMidFlight()
    {
        await ResetAsync();
        await OpenAsync();
        _visibility.Visible = true;
        await AdvanceAsync(4);
        await CaptureAsync("mid-flight");
        Assert(!_visibility.IsIdle, "展开应仍在进行（帧时钟与窗口共用）");
        _flyout.Hide();
        await AdvanceAsync(4);
        await CaptureAsync("closed");
        Assert(!_flyout.IsOpen, "弹层应已关闭");
        // 再次打开：新的内容 Scope 正常推进到目标。
        await OpenAsync();
        _visibility.Visible = true;
        for (var i = 0; i < 60 && !_visibility.IsIdle; i++) await AdvanceAsync(3);
        Assert(_visibility.IsIdle, "重新打开后应完成展开");
        _flyout.Hide();
        await AdvanceAsync(2);
    }

    [DemoAction("所有者上的 DurationScale=0 继承到弹层内容：显隐一帧内完成")]
    public async Task OwnerDurationScaleReachesPopup()
    {
        await ResetAsync(scale: 0);
        await OpenAsync();
        _visibility.Visible = true;
        await AdvanceAsync(2);
        await CaptureAsync("snapped");
        Assert(_visibility.IsIdle, "缩放为 0 时显隐应立即完成");
        _flyout.Hide();
        AnimationScope.SetDurationScale(_owner, 1);
        await AdvanceAsync(2);
    }
}
