using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.Visibility;

/// <summary>内置进入/退出过渡目录：每格一个效果名 + 小卡片 + 「播放」按钮，点击按钮切换卡片显隐。</summary>
[Demo("visibility/transitions", Title = "过渡效果", Page = "显隐过渡", PageOrder = 1, Order = 1)]
public partial class VisibilityTransitionsDemo : DemoControlBase
{
    private sealed record EffectCell(string CaptureName, AnimatedVisibility Host, Button PlayButton);

    private readonly List<EffectCell> _cells = new();

    public VisibilityTransitionsDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        BuildCells();
    }

    private void BuildCells()
    {
        AddCell("淡入淡出", "fade", new FadeIn(), new FadeOut());
        AddCell("缩放", "scale", new ScaleIn(), new ScaleOut());
        // 库保留 Compose 的 -0.5 默认值；画廊显式滑动完整宽/高，展示完整移入移出。
        AddCell("横向滑动", "slide-horizontal", new SlideInHorizontally { InitialOffsetRatio = -1 }, new SlideOutHorizontally { TargetOffsetRatio = -1 });
        AddCell("纵向滑动", "slide-vertical", new SlideInVertically { InitialOffsetRatio = -1 }, new SlideOutVertically { TargetOffsetRatio = -1 });
        AddCell("展开收起", "expand", new ExpandIn(), new ShrinkOut());
        AddCell("横向展开", "expand-horizontal", new ExpandHorizontally(), new ShrinkHorizontally());
        AddCell("纵向展开", "expand-vertical", new ExpandVertically(), new ShrinkVertically());
    }

    private void AddCell(string displayName, string captureName, EnterTransition enter, ExitTransition exit)
    {
        var card = new Border
        {
            Width = 120,
            Height = 70,
            Child = new TextBlock
            {
                Text = "内容",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        card.Classes.Add("demo-actor");
        var host = new AnimatedVisibility
        {
            Enter = enter,
            Exit = exit,
            Label = displayName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = card,
        };
        var playButton = new Button
        {
            Content = "播放",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        playButton.Click += (_, _) => host.Visible = !host.Visible;
        _cells.Add(new EffectCell(captureName, host, playButton));
        AddDiagnostic(displayName, () => host.IsContentPresent, () => host.TargetVisible);

        EffectsGrid.Children.Add(new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(8),
            Children =
            {
                new TextBlock
                {
                    Text = displayName,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
                // 外层固定舞台，内层以自然动画尺寸居中。Stretch 会让排列高度始终为 96，吞掉纵向展开。
                new Border { Height = 96, Child = new Border
                {
                    Width = 120, Height = 70, ClipToBounds = true,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Child = host
                } },
                playButton,
            },
        });
    }

    [DemoAction("依次点播放收起全部卡片（每个效果一张中途截图），最后等待全部收敛")]
    public async Task CycleAll()
    {
        await CaptureAsync("initial");
        var fullSizes = _cells.ToDictionary(cell => cell.CaptureName, cell => cell.Host.Bounds.Size);

        foreach (var cell in _cells)
        {
            var fullSize = fullSizes[cell.CaptureName];
            Click(cell.PlayButton);
            await AdvanceAsync(6);
            await CaptureAsync(cell.CaptureName);
            Assert(cell.Host.IsContentPresent, $"{cell.CaptureName}: 退出动画播放期间内容应仍然存活");
            if (cell.CaptureName is "expand" or "expand-horizontal" or "expand-vertical")
            {
                AssertIntermediateSize(cell, fullSize);
            }
        }

        for (var i = 0; i < 600 && _cells.Any(c => !c.Host.IsIdle); i++)
            await AdvanceAsync(1);

        Assert(_cells.All(c => c.Host.IsIdle), "全部效果应收敛到 Idle");
        Assert(_cells.All(c => !c.Host.IsContentPresent), "全部内容应在退出结束后被移除");
        await CaptureAsync("all-settled");

        foreach (var cell in _cells) Click(cell.PlayButton);
        await AdvanceAsync(6);
        await CaptureAsync("entering");
        foreach (var cell in _cells.Where(cell => cell.CaptureName.StartsWith("expand", StringComparison.Ordinal)))
            AssertIntermediateSize(cell, fullSizes[cell.CaptureName]);
        for (var i = 0; i < 600 && _cells.Any(cell => !cell.Host.IsIdle); i++)
            await AdvanceAsync(1);
        Assert(_cells.All(cell => cell.Host.IsIdle && cell.Host.IsContentPresent), "重新展开后全部内容应可见且收敛");
        foreach (var cell in _cells)
            Assert(cell.Host.Bounds.Size == fullSizes[cell.CaptureName], $"{cell.CaptureName}: 展开后恢复完整尺寸");
        await CaptureAsync("all-visible");
    }

    private void AssertIntermediateSize(EffectCell cell, Size fullSize)
    {
        var size = cell.Host.Bounds.Size;
        var changesWidth = cell.CaptureName != "expand-vertical";
        var changesHeight = cell.CaptureName != "expand-horizontal";
        Assert(changesWidth ? size.Width > 0 && size.Width < fullSize.Width - 1 : Math.Abs(size.Width - fullSize.Width) < 1,
            $"{cell.CaptureName}: 横向尺寸应{(changesWidth ? "在动画途中" : "保持不变")}（完整 {fullSize.Width:0.#}，当前 {size.Width:0.#}）");
        Assert(changesHeight ? size.Height > 0 && size.Height < fullSize.Height - 1 : Math.Abs(size.Height - fullSize.Height) < 1,
            $"{cell.CaptureName}: 纵向尺寸应{(changesHeight ? "在动画途中" : "保持不变")}（完整 {fullSize.Height:0.#}，当前 {size.Height:0.#}）");
    }
}
