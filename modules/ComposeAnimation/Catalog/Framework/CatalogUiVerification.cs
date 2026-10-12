using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;

namespace Catalog.Framework;

/// <summary>在真实目录窗口中检查主题、导航、展开栏和交互时钟，复用同一个应用。</summary>
internal static class CatalogUiVerification
{
    public static async Task<int> RunAsync(CatalogWindow window, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var checks = new List<string>();
        try
        {
            await VerifyDetailsMotionAsync(window, outputDirectory);
            checks.Add("details animation, reversal and pressed geometry: passed");
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                window.RequestedThemeVariant = theme;
                foreach (var descriptor in DemoRegistry.Pages.SelectMany(page => page.Demos))
                {
                    window.ShowDemo(descriptor);
                    await LayoutAsync(window);
                    var frame = window.CurrentFrame!;
                    var instance = frame.Current!;
                    Require(!frame.DetailsExpanded, "新页面默认折叠");
                    Require(instance.Diagnostics.Count > 0, "每个演示均提供调试数据");
                    var originalBounds = instance.Bounds;
                    var prefix = Path.Combine(outputDirectory, $"{theme}-{descriptor.Id.Replace('/', '-')}");
                    Capture(window, prefix + "-collapsed.png");
                    // 经 ToggleButton 的状态变更检查展开栏处理路径。
                    var toggle = frame.FindControl<ToggleButton>("DetailsToggle")!;
                    toggle.IsChecked = true;
                    await WaitForDetailsAsync(window, frame);
                    Require(frame.DetailsExpanded, "展开栏已展开");
                    Require(ReferenceEquals(instance, frame.Current), "展开不重建演示");
                    Require(instance.Bounds == originalBounds, "展开不改变演示区域尺寸");
                    Require(instance.Diagnostics.All(value => value.Current != "—"), "调试数据可采样");
                    Capture(window, prefix + "-expanded.png");
                    toggle.IsChecked = false;
                    await WaitForDetailsAsync(window, frame);
                    Require(instance.Bounds == originalBounds, "收起不改变演示区域尺寸");
                    frame.Restart();
                    await LayoutAsync(window);
                    Require(!ReferenceEquals(instance, frame.Current), "重置创建新演示");
                    Require(TopLevel.GetTopLevel(instance) is null, "旧演示已脱离可视树");
                    checks.Add($"{theme} / {descriptor.Id}: passed");
                }
            }

            window.ShowDemo(DemoRegistry.Find("properties/basic")!);
            await LayoutAsync(window);
            var normal = await MeasureWidthAsync(window, 2);
            var slow = await MeasureWidthAsync(window, 0);
            Require(normal > slow + 20, $"慢速播放生效（1×={normal:0.#}, 0.25×={slow:0.#}）");
            window.CurrentFrame!.Restart();
            await LayoutAsync(window);
            Require(window.CurrentFrame.FindControl<ComboBox>("SpeedPicker")!.SelectedIndex == 2, "重置恢复 1×");
            checks.Add("playback speed and reset: passed");

            var action = DemoRegistry.Find("properties/basic")!.Actions.Single(item => item.Name == "ToggleWidth");
            var pendingAction = window.CurrentFrame.RunActionAsync(action);
            await Task.Delay(60);
            window.ShowDemo(DemoRegistry.Find("engine/spec-gallery")!);
            var canceled = false;
            try { await pendingAction.WaitAsync(TimeSpan.FromSeconds(1)); }
            catch (OperationCanceledException) { canceled = true; }
            Require(canceled, "切换页面取消旧演示的开发工具动作");
            checks.Add("navigation cancels developer action: passed");

            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            foreach (var descriptor in DemoRegistry.Pages.SelectMany(page => page.Demos))
            {
                window.ShowDemo(descriptor);
                await LayoutAsync(window);
                var instance = window.CurrentFrame!.Current!;
                foreach (var stage in instance.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("demo-stage")))
                {
                    var position = stage.TranslatePoint(default, instance)!.Value;
                    Require(position.X >= -1 && position.X + stage.Bounds.Width <= instance.Bounds.Width + 1,
                        $"{descriptor.Id} 舞台在最小窗口宽度内完整显示");
                }
                Capture(window, Path.Combine(outputDirectory, $"compact-{descriptor.Id.Replace('/', '-')}.png"));
            }
            checks.Add("minimum window size: passed");
            File.WriteAllText(Path.Combine(outputDirectory, "ui-verification.json"), JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"[PASS] Catalog UI: {checks.Count} checks; {outputDirectory}");
            return 0;
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(outputDirectory, "failure.txt"), error.ToString());
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static async Task VerifyDetailsMotionAsync(CatalogWindow window, string outputDirectory)
    {
        window.RequestedThemeVariant = ThemeVariant.Light;
        window.ShowDemo(DemoRegistry.Find("properties/basic")!);
        await LayoutAsync(window);
        var frame = window.CurrentFrame!;
        var demo = frame.Current!;
        var stageBounds = demo.Bounds;
        var reveal = frame.FindControl<AnimatedVisibility>("DetailsReveal")!;
        var toggle = frame.FindControl<ToggleButton>("DetailsToggle")!;
        var clock = (ProbeFrameSource)window.AnimationClock!;

        async Task StepAsync(int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                clock.Step(TimeSpan.FromSeconds(1d / 60));
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
            }
        }

        toggle.Focus();
        toggle.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space });
        await Task.Delay(100);
        Require(toggle.IsPressed, "展开栏进入真实按下状态");
        Require(toggle.RenderTransform is null || toggle.RenderTransform.Value == Matrix.Identity, "按住展开栏不缩放");
        Capture(window, Path.Combine(outputDirectory, "details-pressed.png"));
        toggle.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space });
        Require(frame.DetailsExpanded, "空格键释放后展开");
        await StepAsync(5);
        var fullHeight = ((Control)reveal.Content!).DesiredSize.Height;
        Require(reveal.Bounds.Height > 0 && reveal.Bounds.Height < fullHeight - 1, "展开有中间高度");
        Require(frame.FindControl<StackPanel>("DetailsPanel")!.Bounds.Width >= reveal.Bounds.Width - 49,
            "纵向动画期间调试表格保持完整宽度");
        Capture(window, Path.Combine(outputDirectory, "details-expanding.png"));

        var beforeReverse = reveal.Bounds.Height;
        frame.DetailsExpanded = false;
        window.UpdateLayout();
        Require(Math.Abs(reveal.Bounds.Height - beforeReverse) < 1, "反向收起不跳变");
        await StepAsync(2);
        beforeReverse = reveal.Bounds.Height;
        frame.DetailsExpanded = true;
        window.UpdateLayout();
        Require(Math.Abs(reveal.Bounds.Height - beforeReverse) < 1, "再次展开不跳变");
        await StepAsync(60);
        Require(reveal.IsIdle && Math.Abs(reveal.Bounds.Height - fullHeight) < 1, "快速反转后完全展开");
        Require(ReferenceEquals(frame.Current, demo) && demo.Bounds == stageBounds, "动画不重建或移动演示");

        frame.DetailsExpanded = false;
        await StepAsync(5);
        Require(reveal.Bounds.Height > 0 && reveal.Bounds.Height < fullHeight - 1, "收起有中间高度");
        Require(reveal.IsContentPresent, "收起动画结束前保留调试内容");
        Capture(window, Path.Combine(outputDirectory, "details-collapsing.png"));
        await StepAsync(60);
        Require(reveal.IsIdle && !reveal.IsContentPresent && reveal.DesiredSize.Height == 0, "收起结束释放占位");
        window.ShowDemo(DemoRegistry.Find("engine/spec-gallery")!);
    }

    private static async Task WaitForDetailsAsync(Window window, DemoFrame frame)
    {
        var reveal = frame.FindControl<AnimatedVisibility>("DetailsReveal")!;
        for (var i = 0; i < 100; i++)
        {
            await LayoutAsync(window);
            if (reveal.IsIdle) return;
        }
        throw new InvalidOperationException("展开栏动画未在时限内完成。");
    }

    private static async Task<double> MeasureWidthAsync(CatalogWindow window, int speedIndex)
    {
        var frame = window.CurrentFrame!;
        frame.Restart();
        await LayoutAsync(window);
        frame.FindControl<ComboBox>("SpeedPicker")!.SelectedIndex = speedIndex;
        var demo = frame.Current!;
        demo.FindControl<Button>("ToggleButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (var i = 0; i < 24; i++) await StepClockAsync(window, TimeSpan.FromSeconds(1d / 60));
        return demo.FindControl<Border>("Box")!.Width;
    }

    private static async Task LayoutAsync(Window window)
    {
        window.UpdateLayout();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        await Task.Delay(50);
        await StepClockAsync(window, TimeSpan.FromMilliseconds(50));
    }

    private static async Task StepClockAsync(Window window, TimeSpan elapsed)
    {
        if (window is CatalogWindow { AnimationClock: ProbeFrameSource clock }) clock.Step(elapsed);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        window.UpdateLayout();
    }

    private static void Capture(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(PixelSize.FromSize(window.ClientSize, 1), new Vector(96, 96));
        bitmap.Render(window);
        bitmap.Save(path, new PngBitmapEncoderOptions());
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
