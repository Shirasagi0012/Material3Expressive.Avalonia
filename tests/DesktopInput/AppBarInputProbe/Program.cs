using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using System.Runtime.InteropServices;
using Avalonia.Input.Raw;
using System.Reflection;

internal static class Program
{
    internal static bool RawInput;
    internal static int Result;
    [STAThread] public static int Main(string[] args)
    {
        RawInput = args.Contains("--raw-input");
        var result = AppBuilder.Configure<ProbeApp>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
        return Result != 0 ? Result : result;
    }
}
public sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new ProbeWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
public sealed class ProbeWindow : Window
{
    private readonly TopAppBarState _top = new();
    private readonly BottomAppBarState _bottom = new();
    private readonly ScrollViewer _viewer = new() { Content = new Border { Height = 1400, Background = Brushes.LightGray } };
    private readonly AppBarScrollScope _scope;
    private readonly VisualAnimationScope _motion;
    private int _touchEvents;
    public ProbeWindow()
    {
        Width = 500; Height = 580; Title = "AppBar desktop input verification"; Topmost = true; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        _motion = VisualAnimationScope.Initialize(this);
        var top = TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(_top);
        var bottom = BottomAppBarDefaults.ExitAlwaysScrollBehavior(_bottom);
        var screen = new Scaffold { TopBar = new MediumTopAppBar { ScrollBehavior = top }, BottomBar = new BottomAppBar { ScrollBehavior = bottom } };
        var body = new Border { Child = _viewer };
        body.Bind(Border.WidthProperty, new Avalonia.Data.Binding("Bounds.Width") { Source = screen });
        body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = screen });
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty)); screen.Content = body;
        Content = _scope = new AppBarScrollScope { Child = screen, TopScrollBehavior = top, BottomScrollBehavior = bottom };
        AddHandler(PointerPressedEvent, (_, e) => { if (e.Pointer.Type == PointerType.Touch) _touchEvents++; }, RoutingStrategies.Tunnel, true);
        Opened += async (_, _) => await Run();
    }
    private void Observe(string stage) => Console.WriteLine($"{stage}: dpi={RenderScaling}, touchEvents={_touchEvents}, top={_top.HeightOffset}/{_top.HeightOffsetLimit}, bottom={_bottom.HeightOffset}, content={_viewer.Offset.Y}, motionError={_motion.LastError}");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private async Task Run()
    {
        try
        {
            Console.WriteLine(Program.RawInput ? "MODE: RawTouch in Win32 host (no OS touch delivery claim)" : "MODE: guarded Win32 OS touch injection");
            using var rawTouch = new TouchDevice();
            await Task.Delay(500); Activate(); await Task.Delay(100);
            Native.GetWindowRect(TryGetPlatformHandle()!.Handle, out var rect); Console.WriteLine($"nativeRect={rect.Left},{rect.Top},{rect.Right},{rect.Bottom}; visible={Native.IsWindowVisible(TryGetPlatformHandle()!.Handle)}");
            Native.DwmGetWindowAttribute(TryGetPlatformHandle()!.Handle, 14, out var cloaked, 4); Console.WriteLine($"DWM cloaked={cloaked}");
            Require(_viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>().Count() == 1, "Native template was not adapted");
            Observe("initial");
            if (!Program.RawInput) Require(Native.InitializeTouchInjection(1, 3), $"InitializeTouchInjection: {Marshal.GetLastWin32Error()}");
            void Touch(uint flags, double y)
            {
                if (Program.RawInput)
                {
                    // Avalonia 12.1.2 WindowImpl.Owner is the real native input root.
                    // Access remains local to this diagnostic executable.
                    var platform = PlatformImpl!;
                    var root = (IInputRoot)platform.GetType().GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(platform)!;
                    var type = flags == 0x10006 ? RawPointerEventType.TouchBegin : flags == 0x40000 ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchUpdate;
                    platform.Input!(new RawTouchEventArgs(rawTouch, (ulong)Environment.TickCount64, root, type, new Point(180, y), RawInputModifiers.None, 1));
                    return;
                }
                var point = this.PointToScreen(new Point(180, y));
                var hit = Native.WindowFromPoint(new(point.X, point.Y)); var target = TryGetPlatformHandle()!.Handle;
                var ancestor = Native.GetAncestor(hit, 2);
                Console.WriteLine($"inject flags={flags:X} at {point}, window={target:X}, hit={hit:X}, ancestor={ancestor:X}, position={Position}");
                var hitClass = new System.Text.StringBuilder(256); Native.GetClassName(hit, hitClass, hitClass.Capacity);
                Native.GetWindowThreadProcessId(hit, out var hitPid); Console.WriteLine($"hit class={hitClass}, process={hitPid}, own process={Environment.ProcessId}, enabled={Native.IsWindowEnabled(target)}");
                Require(ancestor == target, "Injected touch location is outside the probe window");
                var contact = new Native.Contact { Info = new Native.Info { Type = 2, Id = 0, Flags = flags, Point = new(point.X, point.Y) },
                    Mask = 7, Area = new(point.X - 2, point.Y - 2, point.X + 2, point.Y + 2), Orientation = 90, Pressure = 32000 };
                Require(Native.InjectTouchInput(1, [contact]), $"InjectTouchInput: {Marshal.GetLastWin32Error()}");
            }
            Touch(0x10006, 360); await Task.Delay(30);
            Touch(0x20006, 315); await Task.Delay(30);
            Touch(0x20006, 265); await Task.Delay(30);
            Touch(0x20006, 205); await Task.Delay(5);
            Require(_touchEvents > 0, "Win32 touch was not delivered as Touch");
            Require(_top.HeightOffset == _top.HeightOffsetLimit && _viewer.Offset.Y > 0 && _bottom.HeightOffset < 0, "Top/content/bottom drag order");
            Touch(0x40000, 205); var released = _viewer.Offset.Y; Observe("drag"); await Task.Delay(100); Observe("inertia");
            Require(_viewer.Offset.Y > released, "Content inertia did not continue");
            Touch(0x10006, 220); await Task.Delay(30); var stopped = _viewer.Offset.Y; await Task.Delay(100); Observe("interrupt");
            Require(Math.Abs(_viewer.Offset.Y - stopped) < .01, "New press did not interrupt inertia");
            Touch(0x20006, 220); await Task.Delay(15); Touch(0x40000, 220); await Task.Delay(60);
            _scope.Interrupt(); _top.HeightOffset = 0; _top.ContentOffset = 0; _viewer.Offset = default; await Task.Delay(80);
            Touch(0x10006, 90); await Task.Delay(30); Touch(0x20006, 58); await Task.Delay(100); var dragged = _top.HeightOffset; Observe("bar-drag");
            Require(dragged < 0 && dragged > _top.HeightOffsetLimit, "Direct touch bar drag");
            Touch(0x40000, 58); await Task.Delay(160); Observe("bar-snap"); Require(_top.HeightOffset > dragged, "Direct bar snap did not expand");
            Require(_motion.LastError is null, "Animation failed"); Console.WriteLine($"PASS: {(Program.RawInput ? "Win32-host RawTouch" : "Win32 OS-injected touch")}, combined bars, inertia, interruption, direct drag and snap");
        }
        catch (Exception e) { Console.WriteLine($"FAIL: {e}"); Program.Result = 1; }
        finally { Close(); }
    }
}
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point(int x, int y) { public int X = x, Y = y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect(int l, int t, int r, int b) { public int Left = l, Top = t, Right = r, Bottom = b; }
    [StructLayout(LayoutKind.Sequential)] internal struct Info
    { public uint Type, Id, Frame, Flags; public nint Device, Window; public Point Point, Himetric, RawPoint, RawHimetric; public uint Time, History; public int Input; public uint Keys; public ulong Performance; public int Button; }
    [StructLayout(LayoutKind.Sequential)] internal struct Contact
    { public Info Info; public uint Flags, Mask; public Rect Area, RawArea; public uint Orientation, Pressure; }
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool InitializeTouchInjection(uint count, uint mode);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool InjectTouchInput(uint count, [In] Contact[] contacts);
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(Point point);
    [DllImport("user32.dll")] internal static extern nint GetAncestor(nint window, uint flag);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] internal static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int GetClassName(nint window, System.Text.StringBuilder name, int count);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint window, uint attribute, out int value, uint size);
}
