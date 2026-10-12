using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;

namespace Catalog.Framework;

/// <summary>
/// 自动化宿主窗口。Opened 后依次对每个 demo 的每个匹配动作：全新实例 + ProbeFrameSource +
/// <see cref="AutomationEnvironment"/>，注入时钟（含首次布局后遍历可视树兜底），反射执行动作并记录
/// 截图/断言/耗时/状态。全部完成后写 manifest、打印摘要、经退出回调带码关闭。
/// </summary>
public sealed class DemoRunnerWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IReadOnlyList<DemoDescriptor> _demos;
    private readonly string? _actionFilter;
    private readonly string _outputDirectory;
    private readonly Action<int> _exit;
    private readonly ProbeFrameSource _probe = new();
    private bool _started;

    public DemoRunnerWindow(
        IReadOnlyList<DemoDescriptor> demos,
        string? actionFilter,
        string outputDirectory,
        Action<int> exit)
    {
        AnimationScope.Initialize(this, _probe);
        _demos = demos;
        _actionFilter = actionFilter;
        _outputDirectory = outputDirectory;
        _exit = exit;
        Title = "ComposeAnimation Catalog — 自动化运行";
        Width = 960;
        Height = 720;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_started) return;
        _started = true;
        var exitCode = await RunAllAsync();
        Environment.ExitCode = exitCode;
        _exit(exitCode);
    }

    private async Task<int> RunAllAsync()
    {
        Directory.CreateDirectory(_outputDirectory);
        var anyFailure = false;

        foreach (var demo in _demos)
        {
            var actions = new List<ActionManifest>();
            foreach (var action in demo.Actions.Where(Matches))
            {
                var result = await RunActionAsync(demo, action);
                actions.Add(result);
                WriteActionManifest(result);
                Console.WriteLine(
                    $"[{(result.Status == "passed" ? "PASS" : "FAIL")}] {demo.Id} :: {action.Name} " +
                    $"({result.DurationMs} ms, {result.Captures.Count} 截图, {result.Assertions.Count} 断言)");
                if (result.Error is not null) Console.WriteLine(result.Error);
                anyFailure |= result.Status != "passed";
            }

            if (actions.Count == 0)
            {
                anyFailure = true;
                Console.WriteLine($"[FAIL] {demo.Id} :: 没有匹配 --action \"{_actionFilter}\" 的动作。");
            }

            WriteDemoManifest(demo, actions);
        }

        Console.WriteLine(anyFailure ? "存在失败的动作。" : "全部动作通过。");
        return anyFailure ? 1 : 0;
    }

    private bool Matches(DemoActionDescriptor action)
        => _actionFilter is null || action.Name == _actionFilter;

    private async Task<ActionManifest> RunActionAsync(DemoDescriptor demo, DemoActionDescriptor action)
    {
        var probe = _probe;
        var environment = new AutomationEnvironment(probe, _outputDirectory, demo.Id, action.Name);
        var context = new DemoContext { FrameSource = probe, IsAutomation = true };
        var instance = DemoRegistry.Create(demo, context);
        instance.AttachEnvironment(environment);

        // 窗口只由外层 Show 一次，这里仅替换内容。截图背景跟随卡片令牌（自动化模式已锁定浅色）。
        var host = new Border
        {
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = instance
        };
        host.Bind(BackgroundProperty, new DynamicResourceExtension("CatalogCardBrush"));
        Content = host;

        UpdateLayout();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        var stopwatch = Stopwatch.StartNew();
        string? error = null;
        try
        {
            if (action.Method.Invoke(instance, null) is Task task)
                await task;
        }
        catch (Exception exception)
        {
            error = exception is TargetInvocationException { InnerException: not null } invocation
                ? invocation.InnerException!.ToString()
                : exception.ToString();
        }
        stopwatch.Stop();

        var failed = error is not null || environment.Assertions.Any(assertion => !assertion.Success);
        return new ActionManifest(
            demo.Id,
            action.Name,
            failed ? "failed" : "passed",
            error,
            stopwatch.ElapsedMilliseconds,
            environment.Captures.ToArray(),
            environment.Assertions.ToArray());
    }

    private void WriteActionManifest(ActionManifest result)
    {
        var directory = Path.Combine(_outputDirectory, result.Demo, result.Action);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "manifest.json"),
            JsonSerializer.Serialize(result, JsonOptions));
    }

    private void WriteDemoManifest(DemoDescriptor demo, IReadOnlyList<ActionManifest> actions)
    {
        var directory = Path.Combine(_outputDirectory, demo.Id);
        Directory.CreateDirectory(directory);
        var manifest = new DemoManifest(demo.Id, demo.Title, demo.Page, actions);
        File.WriteAllText(
            Path.Combine(directory, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions));
    }

    private sealed record ActionManifest(
        string Demo,
        string Action,
        string Status,
        string? Error,
        long DurationMs,
        IReadOnlyList<string> Captures,
        IReadOnlyList<AssertionRecord> Assertions);

    private sealed record DemoManifest(
        string Demo,
        string Title,
        string Page,
        IReadOnlyList<ActionManifest> Actions);
}
