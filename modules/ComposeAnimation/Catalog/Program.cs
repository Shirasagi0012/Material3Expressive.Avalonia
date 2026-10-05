using System;
using System.Linq;
using Avalonia;
using Catalog.Framework;

namespace Catalog;

internal enum CatalogMode
{
    Catalog,
    Demo,
    Run,
    VerifyUi,
    ListActions
}

/// <summary>CLI 解析结果。解析成功后存入 <see cref="CatalogArguments.Current"/> 供 App 读取。</summary>
internal sealed class CatalogArguments
{
    public CatalogMode Mode { get; private set; } = CatalogMode.Catalog;
    public string? DemoId { get; private set; }
    public bool RunAll { get; private set; }
    public string? OutputDirectory { get; private set; }
    public string? ActionName { get; private set; }
    public string? Error { get; private set; }

    public static CatalogArguments Current { get; set; } = new();

    public static CatalogArguments Parse(string[] args)
    {
        var result = new CatalogArguments();
        var modeSeen = false;

        bool SetMode(CatalogMode mode)
        {
            if (modeSeen)
            {
                result.Error = "只能指定一个模式参数（--demo / --run / --list-actions / --verify-ui）。";
                return false;
            }
            modeSeen = true;
            result.Mode = mode;
            return true;
        }

        string? TakeValue(string[] arguments, ref int index, string option)
        {
            if (index + 1 >= arguments.Length)
            {
                result.Error = $"参数 {option} 缺少值。";
                return null;
            }
            return arguments[++index];
        }

        for (var i = 0; i < args.Length && result.Error is null; i++)
        {
            switch (args[i])
            {
                case "--demo":
                {
                    var id = TakeValue(args, ref i, "--demo");
                    if (id is null || !SetMode(CatalogMode.Demo)) break;
                    result.DemoId = id;
                    break;
                }
                case "--verify-ui":
                    if (SetMode(CatalogMode.VerifyUi))
                        result.OutputDirectory = TakeValue(args, ref i, "--verify-ui");
                    break;
                case "--run":
                {
                    var id = TakeValue(args, ref i, "--run");
                    if (id is null || !SetMode(CatalogMode.Run)) break;
                    if (id == "--all") result.RunAll = true;
                    else result.DemoId = id;
                    break;
                }
                case "--out":
                    result.OutputDirectory = TakeValue(args, ref i, "--out");
                    break;
                case "--action":
                    result.ActionName = TakeValue(args, ref i, "--action");
                    break;
                case "--list-actions":
                {
                    var id = TakeValue(args, ref i, "--list-actions");
                    if (id is null || !SetMode(CatalogMode.ListActions)) break;
                    result.DemoId = id;
                    break;
                }
                default:
                    result.Error = $"未知参数: {args[i]}";
                    break;
            }
        }

        if (result.Error is null)
        {
            if (result.Mode == CatalogMode.Run && result.OutputDirectory is null)
                result.Error = "--run 需要 --out <dir> 指定输出目录。";
            else if (result.Mode is not (CatalogMode.Run or CatalogMode.VerifyUi) && result.OutputDirectory is not null)
                result.Error = "--out 只能与 --run 一起使用；--verify-ui 直接接输出目录。";
            else if (result.Mode != CatalogMode.Run && result.ActionName is not null)
                result.Error = "--action 只能与 --run 一起使用。";
        }

        return result;
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("""
            用法:
              Catalog                                  打开目录窗口
              Catalog --demo <id>                      单独打开一个 demo
              Catalog --run <id|--all> --out <dir> [--action <name>]
                                                       自动化运行 demo 动作，输出截图与 manifest（会短暂开窗）
              Catalog --list-actions <id>              列出 demo 的动作（不启动界面）
              Catalog --verify-ui <dir>                检查目录外壳、主题、展开栏并输出截图
            """);
    }
}

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var options = CatalogArguments.Parse(args);
        if (options.Error is not null)
        {
            Console.Error.WriteLine(options.Error);
            CatalogArguments.PrintUsage();
            return 2;
        }
        CatalogArguments.Current = options;

        // --list-actions 只走反射，不启动 Avalonia。
        if (options.Mode == CatalogMode.ListActions)
        {
            var demo = DemoRegistry.Find(options.DemoId!);
            if (demo is null) return UnknownId(options.DemoId!);
            foreach (var action in demo.Actions)
                Console.WriteLine(action.Description is null
                    ? action.Name
                    : $"{action.Name}\t{action.Description}");
            return 0;
        }

        if (options.Mode is CatalogMode.Demo || (options.Mode == CatalogMode.Run && !options.RunAll))
        {
            if (DemoRegistry.Find(options.DemoId!) is null) return UnknownId(options.DemoId!);
        }

        if (options.Mode == CatalogMode.Run && options.ActionName is not null)
        {
            var targets = options.RunAll
                ? DemoRegistry.Pages.SelectMany(page => page.Demos).ToArray()
                : [DemoRegistry.Find(options.DemoId!)!];
            if (!targets.Any(demo => demo.Actions.Any(action => action.Name == options.ActionName)))
            {
                Console.Error.WriteLine($"没有名为 \"{options.ActionName}\" 的动作。可用动作:");
                foreach (var demo in targets)
                    foreach (var action in demo.Actions)
                        Console.Error.WriteLine($"  {demo.Id} :: {action.Name}");
                return 2;
            }
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static int UnknownId(string id)
    {
        Console.Error.WriteLine($"未知 demo id: {id}");
        Console.Error.WriteLine("可用 id:");
        foreach (var page in DemoRegistry.Pages)
            foreach (var demo in page.Demos)
                Console.Error.WriteLine($"  {demo.Id}");
        return 2;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
