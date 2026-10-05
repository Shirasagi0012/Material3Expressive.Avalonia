using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Catalog.Framework;
using ComposeAnimation;

namespace Catalog;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = CatalogArguments.Current;
            switch (options.Mode)
            {
                case CatalogMode.Demo:
                {
                    var demo = DemoRegistry.Find(options.DemoId!)!;
                    var window = new Window();
                    VisualAnimationScope.Initialize(window);
                    window.Title = $"{demo.Title} — ComposeAnimation Catalog";
                    window.Width = 960;
                    window.Height = 720;
                    window.Content = new ScrollViewer
                    {
                        Content = new DemoFrame(demo, DemoContext.Ambient) { Margin = new Thickness(16) }
                    };
                    desktop.MainWindow = window;
                    break;
                }
                case CatalogMode.Run:
                {
                    // 自动化截图要可复现：锁定浅色主题，不跟随系统。
                    RequestedThemeVariant = ThemeVariant.Light;
                    var demos = options.RunAll
                        ? DemoRegistry.Pages.SelectMany(page => page.Demos).ToArray()
                        : [DemoRegistry.Find(options.DemoId!)!];
                    // 通过 Shutdown(exitCode) 退出：ClassicDesktopStyleApplicationLifetime
                    // 在主循环结束时用该码覆写 Environment.ExitCode 并作为进程退出码。
                    desktop.MainWindow = new DemoRunnerWindow(
                        demos, options.ActionName, options.OutputDirectory!,
                        exitCode => desktop.Shutdown(exitCode));
                    break;
                }
                case CatalogMode.VerifyUi:
                {
                    var window = new CatalogWindow(new ProbeClock());
                    window.Opened += async (_, _) => desktop.Shutdown(
                        await CatalogUiVerification.RunAsync(window, options.OutputDirectory!));
                    desktop.MainWindow = window;
                    break;
                }
                default:
                    desktop.MainWindow = new CatalogWindow();
                    break;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
