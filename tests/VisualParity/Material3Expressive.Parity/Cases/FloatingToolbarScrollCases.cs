using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record FloatingToolbarScrollConfig(string Exit = "bottom", string Action = "snap-away", bool Rtl = false);

[ParitySuite("floating-toolbar-scroll")]
public sealed class FloatingToolbarScrollCases
{
    public static IEnumerable<CaseData<FloatingToolbarScrollConfig>> Configurations()
    {
        foreach (var exit in new[] { "start", "end", "top", "bottom" })
        foreach (var action in new[] { "snap-back", "snap-away" })
            yield return new($"{exit}.{action}", "Content touch release automatically settles the floating toolbar", new(exit, action));
        foreach (var action in new[] { "reverse", "interrupt", "disconnect", "detach", "fling-up", "fling-down" })
            yield return new($"bottom.{action}", "Nested scroll release and lifecycle", new(Action: action));
        foreach (var exit in new[] { "start", "end" })
            yield return new($"{exit}.rtl", "RTL automatic toolbar release", new(exit, Rtl: true));
    }

    [ParityScenario("touch", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<FloatingToolbarScrollConfig>();
        await using var host = new AvaloniaHost(writer, 280, 320, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse("#F3EDF7");
        host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse("#49454F");
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var viewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, IsScrollInertiaEnabled = true,
            Content = new Grid { Height = 1200, Background = new SolidColorBrush(Color.Parse("#DDEEFF")),
                Children = { new Border { Height = 16, Margin = new Thickness(24, 60, 24, 0),
                    VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Color.Parse("#305080")) } } } };
        var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(Enum.Parse<FloatingToolbarExitDirection>(c.Exit, true));
        FloatingToolbar toolbar = c.Exit is "start" or "end" ? new VerticalFloatingToolbar() : new HorizontalFloatingToolbar();
        toolbar.Margin = new Thickness(16);
        toolbar.HorizontalAlignment = c.Exit == "start" ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        toolbar.VerticalAlignment = c.Exit == "top" ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        toolbar.Content = new Border { Width = 48, Height = 48, Background = new SolidColorBrush(Color.Parse("#49454F")) };
        toolbar.ScrollBehavior = behavior; toolbar.ScrollSource = viewer;
        var root = new Grid { Children = { viewer, toolbar } };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        if (c.Action == "fling-up") { viewer.Offset = new Vector(0, 840); host.Draw(); }
        if (c.Action == "fling-down") { viewer.Offset = new Vector(0, 40); behavior.State.Offset = -60; host.Draw(); }
        using var touch = Activator.CreateInstance<TouchDevice>();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(host.Window)!;
        var inputRoot = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
        var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("toolbar", "floating-toolbar-scroll", null,
                [Measurement.Numeric("offset", behavior.State.Offset, "dip"), Measurement.Numeric("limit", behavior.State.OffsetLimit, "dip"),
                 Measurement.Numeric("content-offset", behavior.State.ContentOffset, "dip"), Measurement.Numeric("body-offset", viewer.Offset.Y, "dip")])], true));
        Sample("initial");
        for (long time = 16000; time <= 1200000; time += 16000)
        {
            host.AdvanceTo(time);
            var down = c.Action == "fling-down";
            double? y = time switch
            {
                16000 => 150,
                32000 => down ? 220 : c.Action == "snap-back" ? 110 : 70,
                48000 when c.Action.StartsWith("fling") => down ? 270 : 20,
                64000 => down ? 270 : c.Action.StartsWith("fling") ? 20 : c.Action == "snap-back" ? 110 : 70,
                112000 when c.Action == "interrupt" => 100,
                128000 when c.Action == "interrupt" => 140,
                176000 when c.Action == "interrupt" => 140,
                48000 when c.Action == "reverse" => 110,
                _ => null
            };
            if (y is { } position)
            {
                var type = time is 16000 or 112000 ? RawPointerEventType.TouchBegin
                    : time == 64000 ? c.Action.StartsWith("fling") ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchCancel
                    : time == 176000 ? RawPointerEventType.TouchCancel : RawPointerEventType.TouchUpdate;
                input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch,
                    (ulong)(1000 + time / 1000), inputRoot, type, new Point(140, position), RawInputModifiers.None, 1L)!);
                writer.Record(time, "input", $"touch {type} (140,{position})"); host.Draw();
            }
            if (time == 112000 && c.Action == "disconnect") { toolbar.ScrollSource = null; writer.Record(time, "mutation", "disconnect source"); }
            if (time == 112000 && c.Action == "detach") { root.Children.Remove(toolbar); writer.Record(time, "mutation", "detach toolbar"); }
            host.Frame(time);
            if (time is 32000 or 48000 or 64000 or 80000 or 112000 or 128000 or 176000 or 240000 or 400000 or 800000 or 1200000)
                Sample($"time-{time}");
        }
    }
}
