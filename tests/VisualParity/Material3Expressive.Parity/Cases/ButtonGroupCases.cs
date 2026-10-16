using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ButtonGroupConfig(bool Rtl = false, int Space = 400, bool FixedWidth = false,
    string Arrangement = "SpacedBy", string Weights = "none", string Alignment = "Top",
    string Content = "blocks", string Action = "none", int PressIndex = 1,
    double Ratio = .15, double Compression = 24, bool Checked = false, string SpacingAlignment = "Left");

[ParitySuite("button-group")]
public sealed class ButtonGroupCases
{
    public static IEnumerable<CaseData<ButtonGroupConfig>> StaticConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        {
            var direction = rtl ? "rtl" : "ltr";
            foreach (var width in new[] { 400, 152, 151, 140, 108, 100, 60, 32, 20, 0 })
                yield return new($"static.{direction}.width-{width}", "Intrinsic sizing and overflow boundaries", new(rtl, width));

            foreach (var arrangement in Enum.GetNames<ButtonGroupArrangement>())
                yield return new($"static.{direction}.arrange-{arrangement.ToLowerInvariant()}", "Horizontal arrangement with remaining space",
                    new(rtl, 240, true, arrangement));

            foreach (var alignment in new[] { "Center", "Right" })
                yield return new($"static.{direction}.spaced-{alignment.ToLowerInvariant()}", "Spaced group alignment",
                    new(rtl, 240, true, SpacingAlignment: alignment));

            foreach (var weights in new[] { "all", "mixed" })
                yield return new($"static.{direction}.weight-{weights}", "Weighted remainder distribution", new(rtl, 241, Weights: weights));

            foreach (var alignment in new[] { "Center", "Bottom" })
                yield return new($"static.{direction}.align-{alignment.ToLowerInvariant()}", "Unequal item heights and per-item alignment",
                    new(rtl, Alignment: alignment));

            foreach (var selected in new[] { false, true })
                yield return new($"static.{direction}.connected-{(selected ? "on" : "off")}", "Connected leading, middle and trailing shapes",
                    new(rtl, Content: "connected", Checked: selected));
        }
    }

    public static IEnumerable<CaseData<ButtonGroupConfig>> DynamicConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        {
            var direction = rtl ? "rtl" : "ltr";
            foreach (var index in new[] { 0, 1, 2 })
                yield return new($"dynamic.{direction}.press-{index}", "Expand pressed item and compress its neighbours",
                    new(rtl, Content: "buttons", Action: "press", PressIndex: index, Ratio: .5));

            foreach (var action in new[] { "quick", "repress", "cancel", "disable" })
                yield return new($"dynamic.{direction}.{action}", "Press interruption and release threshold",
                    new(rtl, Content: "buttons", Action: action, Ratio: .5));

            yield return new($"dynamic.{direction}.zero-ratio", "Zero expansion preserves item widths", new(rtl, Content: "buttons", Action: "press", Ratio: 0));
            yield return new($"dynamic.{direction}.single-visible", "Pressed prefix with a hidden neighbour", new(rtl, 120, Content: "buttons", Action: "press", PressIndex: 0, Ratio: .5));
            yield return new($"dynamic.{direction}.compression-cap", "Neighbour compression limit", new(rtl, Content: "buttons", Action: "press", Ratio: 2, Compression: 3));
            yield return new($"dynamic.{direction}.connected", "Connected press and checked shapes", new(rtl, Content: "connected", Action: "press", Ratio: .5));

            foreach (var action in new[] { "menu", "outside", "reverse", "resize" })
                yield return new($"dynamic.{direction}.{action}", "Overflow menu input and resizing", new(rtl, 100, Action: action));
        }
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);

    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<ButtonGroupConfig>();
        await using var host = new AvaloniaHost(writer, 420, 240);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.SurfaceContainer] = Color.Parse("#F3EDF7");
        var group = new ButtonGroup
        {
            MaxWidth = config.Space,
            Width = config.FixedWidth ? config.Space : double.NaN,
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            HorizontalArrangement = Enum.Parse<ButtonGroupArrangement>(config.Arrangement),
            ItemVerticalAlignment = Enum.Parse<VerticalAlignment>(config.Alignment),
            SpacingAlignment = Enum.Parse<HorizontalAlignment>(config.SpacingAlignment),
            ExpandedRatio = config.Ratio
        };
        Button? indicator = null;
        group.OverflowIndicatorTemplate = new FuncDataTemplate<ButtonGroupMenuState>((state, _) =>
        {
            indicator = new Button
            {
                Template = new FuncControlTemplate<Button>((_, _) => new Marker(32, 40, Brushes.Green))
            };
            indicator.Click += (_, _) => { if (state!.IsShowing) state.Dismiss(); else state.Show(); };
            return indicator;
        });

        var items = new List<ButtonGroupCustomItem>();
        var menuItems = new List<DropdownMenuItem>();
        var buttons = new List<Button>();
        var contentMarkers = new List<Marker>();
        var clicks = 0;
        var sizes = new[] { new Size(40, 24), new Size(56, 32), new Size(32, 40) };
        for (var index = 0; index < sizes.Length; index++)
        {
            var item = new ButtonGroupCustomItem { CompressionLimit = config.Compression };
            if (config.Content == "blocks")
            {
                item.Content = new Marker(sizes[index].Width, sizes[index].Height, Brushes.Blue);
            }
            else
            {
                Button button = config.Content == "connected" ? new ToggleButton { IsChecked = config.Checked } : new Button();
                var contentMarker = new Marker(new[] { 32, 48, 24 }[index], 16);
                button.Content = contentMarker;
                contentMarkers.Add(contentMarker);
                // Explicit equal inputs isolate the group's width animation from Button's two padding overloads.
                button.Padding = new Thickness(16, 10);
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                button.Click += (_, _) => clicks++;
                buttons.Add(button);
                item.Content = button;
                item.PressSource = button;
            }

            if (config.Weights == "all" || config.Weights == "mixed" && index != 1)
                item.Weight = index + 1;
            if (index == 1 && config.Alignment != "Top")
                item.Alignment = VerticalAlignment.Top;
            var menuItem = new DropdownMenuItem { Content = new Marker(48, 16) };
            menuItem.Click += (_, _) => { clicks++; group.MenuState.Dismiss(); };
            menuItems.Add(menuItem);
            item.MenuContent = menuItem;
            items.Add(item);
            group.Items.Add(item);
        }

        var canvas = new Canvas { Background = Brushes.White, Children = { group } };
        Canvas.SetLeft(group, 32);
        Canvas.SetTop(group, 24);
        host.Mount(canvas);
        if (config.Content == "connected")
        {
            var positions = new[] { "Leading", "Middle", "Trailing" };
            for (var index = 0; index < buttons.Count; index++)
                buttons[index].Theme = (ControlTheme)buttons[index].FindResource($"MaterialConnected{positions[index]}ToggleButtonTheme")!;
        }
        host.Frame(-1000);
        host.Frame(0);

        long nextFrame = 16000;
        void Advance(long time)
        {
            while (nextFrame <= time)
            {
                host.Frame(nextFrame);
                nextFrame += 16000;
            }
            host.AdvanceTo(time);
        }

        void Sample(string id)
        {
            var points = new List<ProbePoint>
            {
                new("group", "container", null, AvaloniaMeasurements.Geometry(group.Layout!.Row, host.Window).Where(measurement => measurement.Name is "box" or "size").ToArray()),
                new("state", "state", null,
                [Measurement.Flag("expanded", group.MenuState.IsShowing, definition: "button-group.menu.showing"),
                 Measurement.Numeric("clicks", clicks, "count", definition: "button-group.clicks")])
            };
            // Resizing and menu cases observe the group and real popup. Unplaced Compose children
            // can retain old LayoutCoordinates, so those scenarios do not use child geometry.
            if (config.Action is not ("menu" or "outside" or "reverse" or "resize"))
            {
                for (var index = 0; index < group.Layout!.VisibleCount; index++)
                {
                    points.Add(new($"item-{index}", "item", "group", AvaloniaMeasurements.Geometry(items[index], host.Window)
                        .Where(measurement => measurement.Name is "box" or "size").ToArray()));
                    if (contentMarkers.Count > index)
                        points.Add(new($"content-{index}", "content", $"item-{index}", AvaloniaMeasurements.Geometry(contentMarkers[index], host.Window)
                            .Where(measurement => measurement.Name is "box" or "size").ToArray()));
                }
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray(), image: id != "time-32000"));
        }

        void Times(params long[] times)
        {
            foreach (var time in times)
            {
                Advance(time);
                host.Draw();
                Sample($"time-{time}");
            }
        }

        void Mutate(long time, string id, Action action)
        {
            Advance(time);
            action();
            writer.Record(time, "mutation", id);
            host.Draw();
            host.Frame(time);
            Sample(id);
        }

        Point Center(Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), host.Window)!.Value;
        void Click(Control control)
        {
            var point = Center(control);
            host.Input("move", point);
            host.Input("down", point);
            host.Input("up", point);
        }

        Sample("initial");
        if (!dynamic)
            return;
        if (config.Action == "resize")
        {
            Mutate(1000, "all-visible", () => group.MaxWidth = 400);
            Mutate(2000, "none-visible", () => group.MaxWidth = 20);
            Mutate(3000, "restore", () => group.MaxWidth = config.Space);
            return;
        }
        if (config.Content == "blocks")
        {
            Mutate(1000, "open", () => Click(indicator!));
            Times(16000, 32000, 64000);
            if (config.Action == "reverse")
            {
                Mutate(65000, "close", group.MenuState.Dismiss);
                Times(80000, 96000);
                Mutate(97000, "reopen", group.MenuState.Show);
                Times(112000, 224000, 800000);
            }
            else
            {
                Times(224000, 800000);
                Mutate(801000, "select-or-dismiss", () =>
                {
                    if (config.Action == "outside")
                    {
                        host.Input("move", new(8, 8));
                        host.Input("down", new(8, 8));
                        host.Input("up", new(8, 8));
                    }
                    else
                        Click(menuItems[^1]);
                });
                Times(816000, 960000, 1200000);
            }
            return;
        }

        var pressedButton = buttons[config.PressIndex];
        var center = Center(pressedButton);
        void Input(long time, string operation, Point point)
        {
            Advance(time);
            host.Input(operation, point);
            host.Draw();
        }
        Input(1000, "move", center);
        Input(2000, "down", center);
        Sample("press");
        if (config.Action == "quick")
        {
            Input(5000, "up", center);
            Sample("release");
            Times(16000, 32000, 64000, 80000, 160000, 400000, 800000);
        }
        else
        {
            Times(16000, 32000, 64000);
            if (config.Action == "cancel")
                Input(65000, "move", new(415, 235));
            if (config.Action == "disable")
                Mutate(65000, "disabled", () => pressedButton.IsEnabled = false);
            Input(81000, "up", config.Action == "cancel" ? new(415, 235) : center);
            Sample("release");
            Times(96000);
            if (config.Action == "repress")
            {
                Input(97000, "down", center);
                Sample("repress");
                Times(112000, 160000);
                Input(161000, "up", center);
                Sample("second-release");
            }
            Times(240000, 400000, 800000);
        }
    }

    private sealed class Marker(double width, double height, IBrush? brush = null) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.FillRectangle(brush ?? Avalonia.Controls.Documents.TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}
