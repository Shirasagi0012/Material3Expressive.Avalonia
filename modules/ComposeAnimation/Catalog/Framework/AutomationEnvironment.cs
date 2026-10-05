using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Catalog.Framework;

/// <summary>
/// 自动化环境：ProbeClock 逐帧推进 + 布局/渲染 flush；截图落盘到
/// <c>&lt;out&gt;/&lt;demo-id&gt;/&lt;action&gt;/&lt;name&gt;.png</c>；断言记入列表供 manifest 输出。
/// 每个 (demo, action) 运行使用一个全新实例。
/// </summary>
internal sealed class AutomationEnvironment : IDemoEnvironment
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(1000.0 / 60);

    private readonly ProbeClock _clock;
    private readonly List<string> _captures = [];
    private readonly List<AssertionRecord> _assertions = [];

    public AutomationEnvironment(ProbeClock clock, string outputRoot, string demoId, string actionName)
    {
        _clock = clock;
        DemoId = demoId;
        ActionName = actionName;
        OutputDirectory = Path.Combine(outputRoot, demoId, actionName);
    }

    public string DemoId { get; }

    public string ActionName { get; }

    /// <summary>本 action 的输出目录（截图与 manifest 落盘处）。</summary>
    public string OutputDirectory { get; }

    /// <summary>已生成截图的文件名（相对于 <see cref="OutputDirectory"/>）。</summary>
    public IReadOnlyList<string> Captures => _captures;

    public IReadOnlyList<AssertionRecord> Assertions => _assertions;

    public async Task AdvanceAsync(DemoControlBase demo, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            _clock.Step(FrameInterval);
            // 库的动画循环以 Loaded 优先级 post 帧 continuation（Dispatcher 按 job 优先级切换
            // SynchronizationContext），会被本帧后续的 Render/Normal 任务无限插队；Step 之后
            // 先排空队列让所有已到期的 continuation 执行，再推进布局与渲染。
            Dispatcher.UIThread.RunJobs();
            demo.UpdateLayout();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        }
    }

    public Task CaptureAsync(DemoControlBase demo, string name)
    {
        Directory.CreateDirectory(OutputDirectory);
        var fileName = name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? name : name + ".png";
        var path = Path.Combine(OutputDirectory, fileName);
        var size = demo.Bounds.Size;
        var width = Math.Max(1, (int)Math.Ceiling(size.Width));
        var height = Math.Max(1, (int)Math.Ceiling(size.Height));
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(demo);
        bitmap.Save(path, new PngBitmapEncoderOptions());
        _captures.Add(fileName);
        return Task.CompletedTask;
    }

    public void RecordAssertion(bool success, string message) => _assertions.Add(new AssertionRecord(success, message));
}
