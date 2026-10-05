using Avalonia;
using Avalonia.Interactivity;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog.Demos.ContentSize;

/// <summary>
/// 迁移自 Scratchpad/AnimatedContentSizeDemo 第 1/3 节：文本在短/长/更宽之间切换，
/// AnimatedContentSize（SizeSpringSpec）把内容卡片报告给父级的尺寸平滑过渡到新的自然尺寸。
/// </summary>
[Demo("content-size/text", Title = "文本与尺寸", Page = "布局动画", PageOrder = 3, Order = 0)]
public partial class TextGrowthDemo : DemoControlBase
{
    private const double NarrowWidth = 280;
    private const double WideWidth = 430;

    private const string ShortText = "短文本：一行内容。";
    private const string LongText = "长文本：这是一段明显更长的内容，用来演示 AnimatedContentSize 在文本换行时把容器高度平滑撑到目标尺寸，而不是瞬间跳变。";
    private const string WideText = "更宽文本：内容卡片变得更宽，宽度与高度一起参与尺寸动画。";

    private int _completedCount;
    private Size _lastInitial;
    private Size _lastTarget;

    public TextGrowthDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("尺寸", () => Card.Bounds.Size, () => CardBorder.DesiredSize, "DIP");
    }

    private void ShortTextButton_OnClick(object? sender, RoutedEventArgs e) => SetText(NarrowWidth, ShortText);
    private void LongTextButton_OnClick(object? sender, RoutedEventArgs e) => SetText(NarrowWidth, LongText);
    private void WideTextButton_OnClick(object? sender, RoutedEventArgs e) => SetText(WideWidth, WideText);

    private void SetText(double width, string text)
    {
        // 不替换 Content，只改现有子控件：测量尺寸变化同样触发动画。
        CardText.Width = width;
        CardText.Text = text;
    }

    private void Card_OnAnimationCompleted(object? sender, AnimatedContentSizeChangedEventArgs e)
    {
        _completedCount++;
        _lastInitial = e.InitialSize;
        _lastTarget = e.TargetSize;
    }

    [DemoAction("短文本 → 长文本：中途尺寸介于两者之间，收敛后等于内容自然尺寸且触发 AnimationCompleted")]
    public async Task Grow()
    {
        await CaptureAsync("initial");
        var initialSize = Card.Bounds.Size;

        Click(LongTextButton);
        await AdvanceAsync(1); // 布局测出新目标并启动动画
        var targetSize = CardBorder.DesiredSize; // 子级始终按自然（目标）尺寸测量
        Assert(targetSize.Height > initialSize.Height + 10,
            $"长文本目标高度应明显大于初始高度（{initialSize.Height:F1} → {targetSize.Height:F1}）");

        await AdvanceAsync(2);
        await CaptureAsync("mid");
        var midHeight = Card.Bounds.Height;
        Assert(midHeight > initialSize.Height + 0.5 && midHeight < targetSize.Height - 0.5,
            $"中途高度应介于 {initialSize.Height:F1} 与 {targetSize.Height:F1} 之间（当前 {midHeight:F1}）");

        var expectedCount = _completedCount + 1;
        var completed = await WaitForCompletedAsync(expectedCount);
        await CaptureAsync("settled");
        Assert(completed, "AnimationCompleted 应在轮询上限内触发");

        var settled = Card.Bounds.Size;
        Assert(Math.Abs(settled.Width - targetSize.Width) < 0.5 && Math.Abs(settled.Height - targetSize.Height) < 0.5,
            $"最终报告尺寸应等于内容自然尺寸 {targetSize.Width:F1}×{targetSize.Height:F1}（当前 {settled.Width:F1}×{settled.Height:F1}）");
        Assert(Math.Abs(_lastInitial.Height - initialSize.Height) < 0.5 && Math.Abs(_lastTarget.Height - targetSize.Height) < 0.5,
            $"完成事件应携带本次运行的起点与目标（{_lastInitial.Height:F1} → {_lastTarget.Height:F1}）");
    }

    // 有上限的轮询等待动画自然完成，比写死帧数更稳
    private async Task<bool> WaitForCompletedAsync(int expectedCount)
    {
        for (var i = 0; i < 600 && _completedCount < expectedCount; i++)
            await AdvanceAsync(1);
        return _completedCount >= expectedCount;
    }
}
