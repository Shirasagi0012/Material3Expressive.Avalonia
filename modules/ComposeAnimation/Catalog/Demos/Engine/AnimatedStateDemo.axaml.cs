using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Catalog.Framework;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Catalog.Demos.Engine;

[Demo("state/target", Title = "目标驱动状态", Page = "属性动画", PageOrder = 0, Order = 2)]
public sealed partial class AnimatedStateDemo : DemoControlBase
{
    private readonly AnimatedState<double> _width;
    private Border _second;
    private IDisposable _secondConnection;
    private bool _expanded;

    public AnimatedStateDemo(DemoContext context) : base(context)
    {
        InitializeComponent();
        _second = SecondActor;
        
        _width = AnimatedState.Create(this, 80d,
            defaultAnimationSpec: new TweenSpec<double>(TimeSpan.FromMilliseconds(280), easing: new LinearEasing()),
            label: "Shared actor width");
        _width.Bind(FirstActor, WidthProperty);
        
        _secondConnection = _width.Bind(_second, WidthProperty);
        
        AddDiagnostic("宽度", () => _width.Value, () => _width.TargetValue, "DIP");
        AddDiagnostic("正在运行", () => _width.IsRunning);
    }

    private void ToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _expanded = !_expanded;
        _width.UpdateTarget(_expanded ? 280 : 80);
    }

    private void ReplaceButton_OnClick(object? sender, RoutedEventArgs e) => Replace();

    private void ReconnectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Actors.Children.Contains(_second))
            Actors.Children.Remove(_second);
        else 
            Actors.Children.Add(_second);
    }

    private void Replace()
    {
        _secondConnection.Dispose();
        Actors.Children.Remove(_second);
        
        _second = (Border)((IDataTemplate)Resources["SecondActorTemplate"]!).Build(null)!;
        Actors.Children.Add(_second);
        _secondConnection = _width.Bind(_second, WidthProperty);
    }

    [DemoAction("中途改变目标，两条属性连接共享运动")]
    public async Task Retarget()
    {
        _width.UpdateTarget(280);
        await AdvanceAsync(7);
        Assert(_width.Value > 80 && _width.Value < 280, "共享宽度处于动画中途");
        Assert(FirstActor.Width == _second.Width, "两个输出消费相同采样");
        await CaptureAsync("in-flight");
        _width.UpdateTarget(140);
        await AdvanceAsync(20);
        Assert(_width.Value == 140 && !_width.IsRunning, "新目标完成");
        Assert(FirstActor.Width == 140 && _second.Width == 140, "两条贡献达到新目标");
        await CaptureAsync("retargeted");
    }

    [DemoAction("替换输出部件，保留动画状态")]
    public async Task ReplaceTarget()
    {
        _width.UpdateTarget(280);
        await AdvanceAsync(7);
        var old = _second;
        var current = _width.Value;
        Replace();
        Assert(old.Width == 80, "旧部件恢复底层宽度");
        Assert(_second.Width == current, "新部件立即贡献当前值");
        await CaptureAsync("replacement");
        await AdvanceAsync(20);
        Assert(_second.Width == 280 && FirstActor.Width == 280, "原动画继续完成");
        await CaptureAsync("completed");
    }

    [DemoAction("目标暂时脱离，源动画继续，挂回取得当前值")]
    public async Task ReconnectTarget()
    {
        _width.UpdateTarget(280);
        await AdvanceAsync(7);
        var before = _width.Value;
        Actors.Children.Remove(_second);
        Assert(_second.Width == 80, "目标脱离撤销贡献");
        await AdvanceAsync(4);
        Assert(_width.Value > before, "源运动没有因单个目标脱离而停止");
        Actors.Children.Add(_second);
        Assert(_second.Width == _width.Value, "挂回取得最新值");
        await CaptureAsync("reconnected");
        await AdvanceAsync(20);
        Assert(FirstActor.Width == 280 && _second.Width == 280, "两个目标共同完成");
    }
}
