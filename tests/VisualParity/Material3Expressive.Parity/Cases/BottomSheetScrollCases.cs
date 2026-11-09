using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record BottomSheetScrollConfig(string Kind = "plain", string Action = "expand", bool Rtl = false);

[ParitySuite("bottom-sheet-scroll")]
public sealed class BottomSheetScrollCases
{
    public static IEnumerable<CaseData<BottomSheetScrollConfig>> Configurations()
    {
        foreach (var kind in new[] { "plain", "scaffold", "modal" })
        foreach (var action in new[] { "expand", "reverse", "settle", "fling-up", "fling-down", "disabled", "detach", "auto-height" })
            yield return new($"{kind}.{action}", "Real nested scroll and sheet release", new(kind, action));
        yield return new("plain.rtl", "RTL real nested scroll", new(Rtl: true));
    }

    [ParityScenario("touch", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<BottomSheetScrollConfig>();
        await using var host = new AvaloniaHost(writer, 240, 480, new ManualTimeProvider());
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.SurfaceContainerLow] = Color.Parse("#F7F2FA");
        host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse("#49454F");
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var state = new SheetState(c.Action == "fling-down" ? SheetValue.Expanded : SheetValue.PartiallyExpanded);
        var viewer = new ScrollViewer
        {
            Height = c.Action == "auto-height" ? double.NaN : 360, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, IsScrollInertiaEnabled = true,
            Content = new Grid
            {
                Height = 1200, Background = new SolidColorBrush(Color.Parse("#DDEEFF")),
                Children =
                {
                    new Border { Height = 16, Margin = new Thickness(24, 16, 24, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Background = new SolidColorBrush(Color.Parse("#305080")) },
                    new Border { Height = 16, Margin = new Thickness(24, 200, 24, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Background = new SolidColorBrush(Color.Parse("#804030")) }
                }
            }
        };
        var enabled = c.Action != "disabled";
        Control owner = c.Kind switch
        {
            "modal" => new ModalBottomSheet { Content = viewer, State = state, IsOpen = true, GesturesEnabled = enabled },
            "scaffold" => new BottomSheetScaffold { Content = new Border { Background = Brushes.White },
                SheetContent = viewer, SheetPeekHeight = 240, SheetSwipeEnabled = enabled,
                State = new BottomSheetScaffoldState(state, new SnackbarHostState()) },
            _ => new BottomSheet { Content = viewer, State = state, GesturesEnabled = enabled }
        };
        var root = new Grid { Background = Brushes.White, Children = { owner } };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        if (c.Action == "fling-down") { viewer.Offset = new Vector(0, 80); host.Draw(); }
        using var touch = Activator.CreateInstance<TouchDevice>();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(host.Window)!;
        var inputRoot = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
        var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("sheet", "bottom-sheet-scroll", null,
                [Measurement.Numeric("offset", state.CurrentOffset, "dip"), Measurement.Numeric("body-offset", viewer.Offset.Y, "dip"),
                 Measurement.Text("current", state.CurrentValue.ToString()), Measurement.Text("target", state.TargetValue.ToString())])], true));
        Sample("initial");
        var release = c.Action.StartsWith("fling") ? 64000L : c.Action == "reverse" ? 240000L : 160000L;
        for (long time = 16000; time <= 1600000; time += 16000)
        {
            host.AdvanceTo(time);
            double? y = time switch
            {
                16000 => c.Action == "fling-down" ? 200 : 350,
                32000 => c.Action == "fling-down" ? 270 : c.Action == "settle" ? 310 : 280,
                48000 when c.Action != "settle" => c.Action == "fling-down" ? 340 : c.Action == "fling-up" ? 160 : 100,
                80000 when c.Action == "reverse" => 200,
                128000 when c.Action == "reverse" => 260,
                _ when time == release => c.Action == "fling-down" ? 340 : c.Action == "fling-up" ? 160 : c.Action == "reverse" ? 260 : c.Action == "settle" ? 310 : 100,
                _ => null
            };
            if (y is { } position)
            {
                var type = time == 16000 ? RawPointerEventType.TouchBegin : time == release ? RawPointerEventType.TouchEnd : RawPointerEventType.TouchUpdate;
                input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch,
                    (ulong)(1000 + time / 1000), inputRoot, type, new Point(60, position), RawInputModifiers.None, 1L)!);
                writer.Record(time, "input", $"touch {type} (60,{position})"); host.Draw();
            }
            if (time == 208000 && c.Action == "detach") { root.Children.Clear(); writer.Record(time, "mutation", "detach sheet"); }
            host.Frame(time);
            if (time is 32000 or 48000 or 64000 or 80000 or 128000 or 160000 or 176000 or 208000 or 240000 or 320000 or 480000 or 800000 or 1600000)
                Sample($"time-{time}");
        }
    }
}
