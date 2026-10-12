using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;

namespace Catalog.Framework;

/// <summary>统一的演示舞台、播放控制与按需刷新的调试面板。</summary>
public sealed partial class DemoFrame : UserControl
{
    private readonly DemoDescriptor _descriptor;
    private readonly DemoContext _context;
    private readonly DispatcherTimer _diagnosticTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private DemoContext? _instanceContext;
    private DemoControlBase? _current;
    private CancellationTokenSource? _lifetime;

    public DemoFrame(DemoDescriptor descriptor, DemoContext context)
    {
        InitializeComponent();
        _descriptor = descriptor;
        _context = context;
        TitleText.Text = descriptor.Title;
        ExperimentalBadge.IsVisible = descriptor.Experimental;
        RestartButton.Click += (_, _) => Restart();
        SpeedPicker.IsVisible = !context.IsAutomation;
        SpeedPicker.SelectionChanged += (_, _) =>
        {
            if (_current is { } demo)
                AnimationScope.SetDurationScale(demo, SpeedPicker.SelectedIndex switch { 0 => 4, 1 => 2, _ => 1 });
        };
        DetailsToggle.IsCheckedChanged += (_, _) => SetDetailsVisible();
        _diagnosticTimer.Tick += (_, _) => _current?.RefreshDiagnostics();
    }

    public DemoControlBase? Current => _current;
    public bool DetailsExpanded
    {
        get => DetailsToggle.IsChecked == true;
        set => DetailsToggle.IsChecked = value;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Restart();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ReleaseDemo();
        base.OnDetachedFromVisualTree(e);
    }

    public void Restart()
    {
        ReleaseDemo();
        SpeedPicker.SelectedIndex = 2;
        _instanceContext = new DemoContext { FrameSource = _context.FrameSource, IsAutomation = _context.IsAutomation };
        var demo = DemoRegistry.Create(_descriptor, _instanceContext);
        _lifetime = new CancellationTokenSource();
        demo.AttachEnvironment(new InteractiveEnvironment(_lifetime.Token));
        _current = demo;
        AnimationScope.SetDurationScale(demo, 1);
        ContentHost.Content = demo;
        DiagnosticsList.ItemsSource = demo.Diagnostics;
        SetDetailsVisible();
    }

    private void ReleaseDemo()
    {
        _diagnosticTimer.Stop();
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        ContentHost.Content = null;
        DiagnosticsList.ItemsSource = null;
        _current = null;

    }

    private void SetDetailsVisible()
    {
        DetailsReveal.Visible = DetailsExpanded;
        DetailsChevron.Data = StreamGeometry.Parse(DetailsExpanded ? "M 0,7 L 5,2 L 10,7 L 11,6 L 5,0 L -1,6 Z" : "M 0,0 L 5,5 L 10,0 L 11,1 L 5,7 L -1,1 Z");
        _diagnosticTimer.Stop();
        if (DetailsExpanded && _current is not null)
        {
            _current.RefreshDiagnostics();
            _diagnosticTimer.Start();
        }
    }

    public async Task RunActionAsync(DemoActionDescriptor action)
    {
        // 动作断言以初始状态、正常速度为前提。
        Restart();
        await Dispatcher.UIThread.InvokeAsync(UpdateLayout, DispatcherPriority.Render);
        var demo = _current;
        if (demo is null) return;
        if (action.Method.Invoke(demo, null) is Task task) await task;
    }
}
