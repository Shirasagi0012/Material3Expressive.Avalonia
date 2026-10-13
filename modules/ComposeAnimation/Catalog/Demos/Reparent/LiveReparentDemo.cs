using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Reparent;

/// <summary>
/// 运行中的控件在两个容器间直接移动（普通 Remove/Add，不重建页面、不走 MoveContent）：
/// 每个容器拥有自己的 Get Scope，移动使宿主 Scope 重建，在途动画在新 Scope 中恢复（D17）。
/// </summary>
[Demo("reparent/live", Title = "运行中换父级", Page = "布局动画", PageOrder = 3, Order = 7)]
public sealed class LiveReparentDemo : DemoControlBase
{
    private readonly Border _visibilityLeft = Zone("demo-zone-a"), _visibilityRight = Zone("demo-zone-b");
    private readonly Border _sizeLeft = Zone("demo-zone-a"), _sizeRight = Zone("demo-zone-b");
    private readonly LookaheadBox _boundsLeft = BoundsZone("demo-zone-a", HorizontalAlignment.Left);
    private readonly LookaheadBox _boundsRight = BoundsZone("demo-zone-b", HorizontalAlignment.Right);
    private readonly AnimatedVisibility _visibility;
    private readonly AnimatedContentSize _size;
    private readonly Border _sizeContent;
    private readonly AnimatedBounds _bounds;
    private readonly Button _visibilityButton = new() { Content = "显隐切换并换容器" };
    private readonly Button _sizeButton = new() { Content = "改尺寸并换容器" };
    private readonly Button _boundsButton = new() { Content = "改矩形并换容器" };

    public LiveReparentDemo(DemoContext context) : base(context)
    {
        _visibility = new AnimatedVisibility
        {
            Visible = true,
            Content = new Border { Width = 160, Height = 80, Background = Brush("CatalogActorTintBrush"), Child = new TextBlock { Text = "AnimatedVisibility", Margin = new Thickness(8) } },
        };
        _sizeContent = new Border { Width = 120, Height = 60, Background = Brush("CatalogActorTintBrush"), Child = new TextBlock { Text = "AnimatedContentSize", Margin = new Thickness(8) } };
        _size = new AnimatedContentSize { Content = _sizeContent, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        _bounds = new AnimatedBounds
        {
            TargetSize = new Size(120, 80), Padding = new Thickness(8), Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Child = new LookaheadTextBlock { Text = "AnimatedBounds", Background = Brush("CatalogActorTintBrush") },
        };
        _visibilityLeft.Child = _visibility;
        _sizeLeft.Child = _size;
        _boundsLeft.Children.Add(_bounds);
        _visibilityButton.Click += (_, _) => { _visibility.Visible = !_visibility.Visible; Move(_visibility, _visibilityLeft, _visibilityRight); };
        _sizeButton.Click += (_, _) => { ToggleSize(); Move(_size, _sizeLeft, _sizeRight); };
        _boundsButton.Click += (_, _) => { ToggleBounds(); MoveBounds(); };

        Content = new StackPanel
        {
            Classes = { "demo-root" }, VerticalAlignment = VerticalAlignment.Top, Spacing = 12,
            Children =
            {
                Row(_visibilityLeft, _visibilityRight),
                Row(_sizeLeft, _sizeRight),
                new Border
                {
                    Classes = { "demo-stage" }, Padding = new Thickness(0), Height = 160, MinWidth = 640,
                    Child = new LookaheadBox { Children = { new LookaheadBox { Children = { _boundsLeft, _boundsRight } } } },
                },
                new StackPanel { Classes = { "demo-controls" }, Orientation = Orientation.Horizontal, Spacing = 8, Children = { _visibilityButton, _sizeButton, _boundsButton } },
            },
        };
        // 每个容器各有 Get Scope：换容器即换父 Scope，宿主 Scope 在下一次排空时重建。
        foreach (var zone in new Control[] { _visibilityLeft, _visibilityRight, _sizeLeft, _sizeRight, _boundsLeft, _boundsRight })
            zone.AttachedToVisualTree += (_, _) => AnimationScope.Get(zone);
        AddDiagnostic("AnimatedVisibility 不透明度", () => VisibilityAlphaPercent(), unit: "%");
        AddDiagnostic("AnimatedContentSize 宽度", () => _size.Bounds.Width, unit: "DIP");
        AddBoundsDiagnostics(() => _bounds.CurrentBounds, () => _bounds.TargetBounds, "AnimatedBounds ");
    }

    private static Border Zone(string zoneClass) => new()
    {
        Classes = { zoneClass }, Width = 300, Height = 120, Padding = new Thickness(10),
    };

    private static LookaheadBox BoundsZone(string zoneClass, HorizontalAlignment alignment) => new()
    {
        Classes = { zoneClass }, Width = 300, Height = 150, HorizontalAlignment = alignment, VerticalAlignment = VerticalAlignment.Top,
    };

    private static StackPanel Row(Control left, Control right) => new()
    {
        Orientation = Orientation.Horizontal, Spacing = 40, Children = { left, right },
    };

    private IBrush? Brush(string key) => this.TryFindResource(key, out var value) ? value as IBrush : Brushes.LightSteelBlue;

    private static void Move(Control child, Border left, Border right)
    {
        var (from, to) = ReferenceEquals(child.Parent, left) ? (left, right) : (right, left);
        from.Child = null;
        to.Child = child;
    }

    private void MoveBounds()
    {
        var (from, to) = ReferenceEquals(_bounds.Parent, _boundsLeft) ? (_boundsLeft, _boundsRight) : (_boundsRight, _boundsLeft);
        from.Children.Remove(_bounds);
        to.Children.Add(_bounds);
    }

    private void ToggleSize()
        => _sizeContent.Width = _sizeContent.Width < 200 ? 260 : 120;

    private void ToggleBounds()
        => _bounds.TargetSize = _bounds.TargetSize?.Width < 200 ? new Size(260, 120) : new Size(120, 80);

    private async Task ResetSectionAsync(string name)
    {
        Func<bool> idle;
        switch (name)
        {
            case "visibility":
                Place(_visibility, _visibilityLeft, _visibilityRight);
                _visibility.Visible = true;
                idle = () => _visibility.IsIdle && _visibility.IsContentPresent;
                break;
            case "content-size":
                Place(_size, _sizeLeft, _sizeRight);
                _sizeContent.Width = 120;
                _size.ResetAnimation();
                idle = () => Math.Abs(_size.Bounds.Width - 120) < 0.5;
                break;
            default:
                if (!ReferenceEquals(_bounds.Parent, _boundsLeft))
                {
                    _boundsRight.Children.Remove(_bounds);
                    _boundsLeft.Children.Add(_bounds);
                }
                _bounds.TargetSize = new Size(120, 80);
                _bounds.ResetAnimation();
                idle = () => !_bounds.IsRunning;
                break;
        }
        for (var i = 0; i < 200 && !idle(); i++) await AdvanceAsync(3);
        Assert(idle(), $"{name}: 复位后应静止");
    }

    private static void Place(Control child, Border left, Border right)
    {
        if (ReferenceEquals(child.Parent, left)) return;
        right.Child = null;
        left.Child = child;
    }

    [DemoAction("AnimatedVisibility 收起途中换容器：在新 Scope 中继续，排空帧不跳变")]
    public Task VisibilityResumesWithoutJump()
        => RunAsync("visibility", () => _visibility.Visible = false, () => Move(_visibility, _visibilityLeft, _visibilityRight),
            VisibilityAlphaPercent, () => !_visibility.IsIdle, () => !_visibility.IsContentPresent);

    // 内容到宿主之间各层 Opacity 之积（淡出由内部表面层承担），以百分比表示。
    private double VisibilityAlphaPercent()
    {
        var alpha = 1.0;
        for (Visual? v = _visibility.Content as Visual; v is not null && !ReferenceEquals(v, _visibility); v = v.GetVisualParent())
            alpha *= v.Opacity;
        return alpha * 100;
    }

    [DemoAction("AnimatedContentSize 变宽途中换容器：在新 Scope 中继续，排空帧不跳变")]
    public Task ContentSizeResumesWithoutJump()
        => RunAsync("content-size", ToggleSize, () => Move(_size, _sizeLeft, _sizeRight),
            () => _size.Bounds.Width, () => Math.Abs(_size.Bounds.Width - 260) > 0.5, () => Math.Abs(_size.Bounds.Width - 260) < 0.5);

    [DemoAction("AnimatedBounds 改尺寸途中换容器（不走 MoveContent）：在新 Scope 中继续，排空帧不跳变")]
    public Task BoundsResumesWithoutJump()
        => RunAsync("bounds", ToggleBounds, MoveBounds,
            () => _bounds.CurrentBounds?.Width ?? double.NaN, () => _bounds.IsRunning, () => !_bounds.IsRunning);

    private async Task RunAsync(string name, Action start, Action move, Func<double> sample, Func<bool> running, Func<bool> settled)
    {
        // 先把本区复位到初始状态（左侧容器、初始尺寸、静止），同一页面上重复运行结果一致。
        await ResetSectionAsync(name);
        await AdvanceAsync(2);
        await CaptureAsync(name + "-initial");
        start();
        await AdvanceAsync(6);
        Assert(running(), $"{name}: 换容器前动画应在运行");
        var a = sample();
        await AdvanceAsync(1);
        var b = sample();
        var step = Math.Abs(b - a);
        Assert(step > 0.1, $"{name}: 换容器前应在变化（每帧 {step:0.##}）");
        move();
        await AdvanceAsync(1); // 排空帧：宿主 Scope 重建，运行在新 Scope 恢复
        var c = sample();
        await CaptureAsync(name + "-moved");
        Assert(Math.Abs(c - b) <= step * 2 + 0.5, $"{name}: 排空帧不应跳变（{b:0.##} → {c:0.##}，此前每帧 {step:0.##}）");
        Assert(Math.Sign(c - b) == Math.Sign(b - a) || Math.Abs(c - b) < 1e-6, $"{name}: 排空帧不应回跳（{a:0.##}, {b:0.##} → {c:0.##}）");
        await AdvanceAsync(1);
        var d = sample();
        Assert(Math.Abs(d - c) > 1e-6 || settled(), $"{name}: 换容器后应继续运行，不应冻结");
        for (var i = 0; i < 200 && !settled(); i++) await AdvanceAsync(3);
        await CaptureAsync(name + "-settled");
        Assert(settled(), $"{name}: 应收敛到目标");
    }
}
