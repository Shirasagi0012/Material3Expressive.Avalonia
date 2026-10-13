using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Catalog.Framework;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.LookaheadPages;

/// <summary>
/// LookaheadSize 与 LookaheadPosition 在同一个 Lookahead 上下文里：重排与改尺寸都在途中打断，
/// 每个节点从当前位置平滑走向新的目标位置；尺寸节点在两个栏之间直接换父级时在新 Scope 中继续（D17）。
/// </summary>
[Demo("lookahead/size-position", Title = "Lookahead 尺寸与位置", Page = "布局动画", PageOrder = 3, Order = 8)]
public sealed class LookaheadDemo : DemoControlBase
{
    private const double ItemHeight = 30;
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(300);
    private readonly LookaheadStackPanel _left = Column(), _right = Column();
    private readonly LookaheadPosition[] _items;
    private readonly Border[] _itemChildren;
    private readonly LookaheadSize _size;
    private readonly LookaheadRoot _root;
    private readonly Button _reorderButton = new() { Content = "重排" };
    private readonly Button _resizeButton = new() { Content = "改尺寸" };
    private readonly Button _moveButton = new() { Content = "尺寸块换栏" };

    public LookaheadDemo(DemoContext context) : base(context)
    {
        _itemChildren = new Border[3];
        _items = new LookaheadPosition[3];
        for (var i = 0; i < 3; i++)
        {
            _itemChildren[i] = new Border
            {
                Width = 140, Height = ItemHeight, Background = Brush("CatalogActorTintBrush"),
                Child = new TextBlock { Text = $"项目 {(char)('A' + i)}", Margin = new Thickness(8, 4) },
            };
            _items[i] = new LookaheadPosition
            {
                Child = _itemChildren[i], HorizontalAlignment = HorizontalAlignment.Left, UseLayoutRounding = false,
                AnimationSpec = new TweenSpec<Point>(Duration, easing: new LinearEasing()),
            };
        }
        _size = new LookaheadSize
        {
            Child = new LookaheadTextBlock { Text = "LookaheadSize", Background = Brush("CatalogActorTintBrush") },
            HorizontalAlignment = HorizontalAlignment.Left, UseLayoutRounding = false, TargetSize = new Size(140, 40),
            AnimationSpec = new TweenSpec<Size>(Duration, easing: new LinearEasing()),
        };
        _root = new LookaheadRoot
        {
            Width = 420, Height = 260, UseLayoutRounding = false,
            Child = new LookaheadStackPanel { Orientation = Orientation.Horizontal, Spacing = 40, Children = { _left, _right } },
        };
        PlaceInitial();
        _reorderButton.Click += (_, _) => Reorder();
        _resizeButton.Click += (_, _) => ToggleSize();
        _moveButton.Click += (_, _) => MoveSize();

        Content = new StackPanel
        {
            Classes = { "demo-root" }, VerticalAlignment = VerticalAlignment.Top, Spacing = 12,
            Children =
            {
                new Border { Classes = { "demo-stage" }, Height = 280, MinWidth = 460, Child = _root },
                new StackPanel { Classes = { "demo-controls" }, Orientation = Orientation.Horizontal, Spacing = 8, Children = { _reorderButton, _resizeButton, _moveButton } },
            },
        };
        // 两栏各有 Get Scope：尺寸块换栏即换父 Scope。
        _left.AttachedToVisualTree += (_, _) => AnimationScope.Get(_left);
        _right.AttachedToVisualTree += (_, _) => AnimationScope.Get(_right);
        AddDiagnostic("项目 A 纵向位置", () => Y(_itemChildren[0]), unit: "DIP");
        AddDiagnostic("尺寸块高度", () => _size.Bounds.Height, () => _size.TargetSize?.Height, "DIP");
    }

    private static LookaheadStackPanel Column() => new() { Orientation = Orientation.Vertical, Width = 160, VerticalAlignment = VerticalAlignment.Top };

    private IBrush? Brush(string key) => this.TryFindResource(key, out var value) ? value as IBrush : Brushes.LightSteelBlue;

    private void PlaceInitial()
    {
        foreach (var c in _left.Children.ToArray()) _left.Children.Remove(c);
        foreach (var c in _right.Children.ToArray()) _right.Children.Remove(c);
        _left.Children.Add(_items[0]);
        _left.Children.Add(_size);
        _left.Children.Add(_items[1]);
        _left.Children.Add(_items[2]);
    }

    private double Y(Control c) => c.TranslatePoint(default, _root)?.Y ?? double.NaN;

    // 节点的布局目标：栏顶加上它之前各节点的目标高度（纵向栈，无间距）。
    private double TargetY(Control node)
    {
        var column = (Panel)node.Parent!;
        var y = column.TranslatePoint(default, _root)!.Value.Y;
        foreach (var c in column.Children)
        {
            if (ReferenceEquals(c, node)) return y;
            y += ReferenceEquals(c, _size) ? _size.TargetSize!.Value.Height : ItemHeight;
        }
        return double.NaN;
    }

    private void Reorder()
    {
        var first = _left.Children.OfType<LookaheadPosition>().First();
        _left.Children.Remove(first);
        _left.Children.Add(first);
    }

    private void ToggleSize() => _size.TargetSize = _size.TargetSize?.Height < 80 ? new Size(140, 110) : new Size(140, 40);

    private void MoveSize()
    {
        var (from, to) = ReferenceEquals(_size.Parent, _left) ? (_left, _right) : (_right, _left);
        from.Children.Remove(_size);
        to.Children.Add(_size);
    }

    private bool Settled() =>
        _items.All(i => Math.Abs(Y(i.Child!) - TargetY(i)) < 0.5) && Math.Abs(_size.Bounds.Height - _size.TargetSize!.Value.Height) < 0.5;

    private async Task ResetAsync()
    {
        PlaceInitial();
        _size.TargetSize = new Size(140, 40);
        for (var i = 0; i < 100 && !Settled(); i++) await AdvanceAsync(3);
        Assert(Settled(), "复位后应静止在布局目标上");
    }

    [DemoAction("重排后途中改尺寸：每个项目最终落在布局目标上，任一帧位移不超过一帧的规格进度")]
    public async Task ReorderAndResizeMidFlight()
    {
        await ResetAsync();
        await AdvanceAsync(2);
        await CaptureAsync("initial");
        var nodes = _items.Select(i => (Control)i.Child!).ToArray();
        var last = nodes.Select(Y).ToArray();
        // 线性 300 ms 补间：每帧至多走完总距离的 1/18；打断后新运行从当前值出发，距离不超过舞台高度。
        const double maxTravel = 260;
        var maxStep = maxTravel * (1000.0 / 60) / Duration.TotalMilliseconds + 0.5;
        var worst = 0.0;
        Reorder();
        for (var f = 0; f < 60; f++)
        {
            if (f == 6) ToggleSize();
            await AdvanceAsync(1);
            for (var n = 0; n < nodes.Length; n++)
            {
                var y = Y(nodes[n]);
                worst = Math.Max(worst, Math.Abs(y - last[n]));
                last[n] = y;
            }
            if (f == 8) await CaptureAsync("mid-flight");
        }
        for (var i = 0; i < 100 && !Settled(); i++) await AdvanceAsync(3);
        await CaptureAsync("settled");
        Assert(worst <= maxStep, $"单帧位移 {worst:0.##} 超过一帧规格进度 {maxStep:0.##}");
        Assert(worst > 0.1, "项目应当移动");
        foreach (var item in _items)
            Assert(Math.Abs(Y(item.Child!) - TargetY(item)) < 0.5, $"项目应落在布局目标（{Y(item.Child!):0.##} ≠ {TargetY(item):0.##}）");
    }

    [DemoAction("尺寸块变高途中换栏：在新 Scope 中继续，排空帧不跳变，收敛到目标")]
    public async Task SizeReparentResumes()
    {
        await ResetAsync();
        await AdvanceAsync(2);
        ToggleSize();
        await AdvanceAsync(6);
        var a = _size.Bounds.Height;
        await AdvanceAsync(1);
        var b = _size.Bounds.Height;
        var step = b - a;
        Assert(step > 0.1, $"换栏前应在变高（每帧 {step:0.##}）");
        MoveSize();
        await AdvanceAsync(1);
        var c = _size.Bounds.Height;
        await CaptureAsync("moved");
        Assert(c - b >= -1e-6 && c - b <= step * 2 + 0.5, $"排空帧不应跳变（{b:0.##} → {c:0.##}，此前每帧 {step:0.##}）");
        await AdvanceAsync(1);
        Assert(_size.Bounds.Height > c || Math.Abs(_size.Bounds.Height - 110) < 0.5, "换栏后应继续变高");
        for (var i = 0; i < 100 && Math.Abs(_size.Bounds.Height - 110) >= 0.5; i++) await AdvanceAsync(3);
        await CaptureAsync("settled");
        Assert(Math.Abs(_size.Bounds.Height - 110) < 0.5, $"应收敛到 110（实际 {_size.Bounds.Height:0.##}）");
        Assert(ReferenceEquals(_size.Parent, _right), "尺寸块应在右栏");
    }
}
