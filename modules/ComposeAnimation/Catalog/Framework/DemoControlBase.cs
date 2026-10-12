using Avalonia.Controls;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Interactivity;
using ComposeAnimation.Runtime;
using ComposeAnimation;

namespace Catalog.Framework;

/// <summary>
/// demo 控件基类。动作方法内通过 <see cref="AdvanceAsync"/> / <see cref="CaptureAsync"/> /
/// <see cref="Assert"/> / <see cref="Click"/> 与宿主环境交互；自动化与交互模式复用同一份动作代码。
/// </summary>
public abstract class DemoControlBase : UserControl
{
    private IDemoEnvironment? _environment;

    protected DemoControlBase(DemoContext context)
        => Context = context ?? throw new ArgumentNullException(nameof(context));

    public DemoContext Context { get; }

    public ObservableCollection<DemoDiagnostic> Diagnostics { get; } = [];

    protected void AddDiagnostic(string name, Func<object?> current, Func<object?>? target = null, string unit = "")
        => Diagnostics.Add(new DemoDiagnostic(name, current, target, unit));

    protected void AddBoundsDiagnostics(Func<Rect?> current, Func<Rect?> target, string prefix = "")
    {
        AddDiagnostic(prefix + "位置", () => current()?.Position, () => target()?.Position, "DIP");
        AddDiagnostic(prefix + "尺寸", () => current()?.Size, () => target()?.Size, "DIP");
    }

    internal void RefreshDiagnostics()
    {
        foreach (var diagnostic in Diagnostics) diagnostic.Refresh();
    }

    protected AnimationScope Motion => AnimationScope.Get(this);
    internal FrameScope EnvironmentScope => Motion;

    /// <summary>
    /// 推进动画帧。自动化模式：每帧 ProbeFrameSource.Step(1/60s) + UpdateLayout + Render 优先级 flush；
    /// 交互模式：逐帧等待环境帧（未附加 TopLevel 时退化为 Task.Delay(16) 近似）。
    /// 环境未注入时按 <see cref="DemoContext.IsAutomation"/> 创建兜底环境。
    /// </summary>
    protected Task AdvanceAsync(int frames) => Environment.AdvanceAsync(this, frames);

    /// <summary>
    /// 截图。自动化模式：渲染本控件到 <c>&lt;out&gt;/&lt;demo-id&gt;/&lt;action&gt;/&lt;name&gt;.png</c>；
    /// 交互模式：no-op（Debug 输出）。
    /// </summary>
    protected Task CaptureAsync(string name) => Environment.CaptureAsync(this, name);

    /// <summary>断言：<paramref name="condition"/> 为 false 时记录并抛出 <see cref="DemoAssertionException"/>。</summary>
    protected void Assert(bool condition, string message)
    {
        Environment.RecordAssertion(condition, message);
        if (!condition) throw new DemoAssertionException(message);
    }

    /// <summary>以编程方式触发按钮的 Click 事件。</summary>
    protected void Click(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        // Avalonia 12 中 Button.PerformClick 不是公开 API，改为直接引发 Click 路由事件。
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    /// <summary>由宿主（runner / frame）注入环境；未注入时 helper 使用按上下文创建的兜底环境。</summary>
    internal void AttachEnvironment(IDemoEnvironment? environment) => _environment = environment;

    private IDemoEnvironment Environment => _environment ??= CreateFallbackEnvironment();

    private IDemoEnvironment CreateFallbackEnvironment()
        => Context.IsAutomation && Context.FrameSource is ProbeFrameSource probe
            ? new AutomationEnvironment(probe, Path.Combine(Path.GetTempPath(), "Catalog"), GetType().Name, "default")
            : new InteractiveEnvironment();
}
