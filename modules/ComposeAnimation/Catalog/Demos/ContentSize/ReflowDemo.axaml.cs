using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Catalog.Framework;

namespace Catalog.Demos.ContentSize;

/// <summary>
/// 迁移自 Scratchpad/AnimatedContentSizeDemo 第 2 节：AnimatedContentSize 容器内增删子项，
/// 报告给父级的动画中间尺寸让下方的兄弟元素被平滑推开 / 合拢。
/// </summary>
[Demo("content-size/reflow", Title = "列表重排", Page = "布局动画", PageOrder = 3, Order = 1)]
public partial class ReflowDemo : DemoControlBase
{
    private const int MaxItems = 6;

    public ReflowDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("项目数量", () => ItemsPanel.Children.Count);
        AddDiagnostic("高度", () => List.Bounds.Height, () => ItemsPanel.DesiredSize.Height, "DIP");
        ItemsPanel.Children.Add(CreateItem(1));
    }

    private void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemsPanel.Children.Count >= MaxItems) return;
        ItemsPanel.Children.Add(CreateItem(ItemsPanel.Children.Count + 1));
        UpdateCount();
    }

    private void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemsPanel.Children.Count == 0) return;
        ItemsPanel.Children.RemoveAt(ItemsPanel.Children.Count - 1);
        UpdateCount();
    }

    private void UpdateCount()
    {
        AddButton.IsEnabled = ItemsPanel.Children.Count < MaxItems;
        RemoveButton.IsEnabled = ItemsPanel.Children.Count > 0;
    }

    private static Border CreateItem(int index) => new()
    {
        Width = 280,
        Height = 36,
        CornerRadius = new CornerRadius(8),
        Classes = { "demo-actor" },
        Padding = new Thickness(12, 0),
        Child = new TextBlock
        {
            Text = $"子项 {index}",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    [DemoAction("加两项再删回：兄弟元素随容器尺寸动画平滑移动，收敛后到位 / 回到原位")]
    public async Task AddAndRemove()
    {
        await CaptureAsync("initial");
        var initialContentHeight = ItemsPanel.DesiredSize.Height;
        var initialMarkerY = SiblingMarker.Bounds.Y;

        Click(AddButton);
        Click(AddButton);
        await AdvanceAsync(1); // 布局测出新目标并启动动画
        Assert(ItemsPanel.Children.Count == 3, "应已添加两项");
        var grownContentHeight = ItemsPanel.DesiredSize.Height;
        var grownDelta = grownContentHeight - initialContentHeight;
        Assert(grownDelta > 10,
            $"加两项后内容目标高度应明显增长（{initialContentHeight:F1} → {grownContentHeight:F1}）");

        await AdvanceAsync(2);
        await CaptureAsync("mid");
        var midY = SiblingMarker.Bounds.Y;
        Assert(midY > initialMarkerY + 0.5 && midY < initialMarkerY + grownDelta - 0.5,
            $"兄弟元素应在位移中（{initialMarkerY:F1} < {midY:F1} < {initialMarkerY + grownDelta:F1}）");

        Assert(await WaitForSettleAsync(), "添加后容器高度应在轮询上限内收敛");
        await CaptureAsync("grown");
        Assert(Math.Abs(List.Bounds.Height - grownContentHeight) < 0.5,
            $"收敛后容器高度应等于内容自然高度 {grownContentHeight:F1}（当前 {List.Bounds.Height:F1}）");
        Assert(Math.Abs(SiblingMarker.Bounds.Y - (initialMarkerY + grownDelta)) < 0.5,
            $"兄弟元素应被推开 {grownDelta:F1}（当前位移 {SiblingMarker.Bounds.Y - initialMarkerY:F1}）");

        Click(RemoveButton);
        Click(RemoveButton);
        Assert(await WaitForSettleAsync(), "删回后容器高度应在轮询上限内收敛");
        await CaptureAsync("restored");
        Assert(ItemsPanel.Children.Count == 1, "应删回只剩初始子项");
        Assert(Math.Abs(List.Bounds.Height - initialContentHeight) < 0.5,
            $"删回收敛后容器高度应回到初始值 {initialContentHeight:F1}（当前 {List.Bounds.Height:F1}）");
        Assert(Math.Abs(SiblingMarker.Bounds.Y - initialMarkerY) < 0.5,
            $"兄弟元素应回到初始位置 {initialMarkerY:F1}（当前 {SiblingMarker.Bounds.Y:F1}）");
    }

    // 有上限的轮询等待收敛：报告高度跨帧不再变化即视为稳定
    private async Task<bool> WaitForSettleAsync()
    {
        for (var i = 0; i < 120; i++)
        {
            var before = List.Bounds.Height;
            await AdvanceAsync(5);
            if (Math.Abs(List.Bounds.Height - before) < 0.001) return true;
        }
        return false;
    }
}
