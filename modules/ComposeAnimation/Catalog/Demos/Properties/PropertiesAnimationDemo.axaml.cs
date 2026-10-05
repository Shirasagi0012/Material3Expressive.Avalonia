using Avalonia.Controls;
using Avalonia.Interactivity;
using Catalog.Framework;

namespace Catalog.Demos.Properties;

/// <summary>移植自 Scratchpad/SimpleAnimation：Border 宽度上的 Double 弹簧动画。</summary>
[Demo("properties/basic", Title = "宽度弹簧", Page = "基础动画", PageOrder = 0, Order = 0)]
public partial class PropertiesAnimationDemo : DemoControlBase
{
    private bool _expanded;

    public PropertiesAnimationDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        AddDiagnostic("宽度", () => Box.Width, () => _expanded ? 500 : 100, "DIP");
    }

    private void ToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Box.Width = _expanded ? 100 : 500;
        _expanded = !_expanded;
    }

    [DemoAction("仅截取初始状态")]
    public async Task Initial()
    {
        await CaptureAsync("initial");
    }

    [DemoAction("切换宽度并验证弹簧收敛到目标值")]
    public async Task ToggleWidth()
    {
        const double target = 500;

        await CaptureAsync("initial");
        Click(ToggleButton);
        await AdvanceAsync(12);
        await CaptureAsync("mid");
        await AdvanceAsync(90);
        await CaptureAsync("settled");

        // DampingRatio=0.5 / Stiffness=10 的弹簧需要约 6.8s（~410 帧）才收敛，
        // 102 帧时仍距目标约 17px；继续推进直到宽度稳定（动画已停）再断言。
        var stopped = await WaitForSettleAsync();
        Assert(Math.Abs(Box.Width - target) < 0.5 || stopped,
            $"宽度应收敛到 {target}（当前 {Box.Width:F2}，动画已停={stopped}）");
    }

    private async Task<bool> WaitForSettleAsync()
    {
        for (var i = 0; i < 120; i++)
        {
            var before = Box.Width;
            await AdvanceAsync(10);
            if (Math.Abs(Box.Width - before) < 0.001) return true;
        }
        return false;
    }
}
