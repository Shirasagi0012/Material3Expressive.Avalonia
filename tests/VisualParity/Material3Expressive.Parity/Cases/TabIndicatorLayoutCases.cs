using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record TabIndicatorLayoutConfig(bool Native = false, bool Scrollable = false, bool Rtl = false,
    string Mode = "elastic", string Action = "none");

[ParitySuite("tab-indicator-layout")]
public sealed class TabIndicatorLayoutCases
{
    public static IEnumerable<CaseData<TabIndicatorLayoutConfig>> StaticConfigurations()
    {
        foreach (var native in new[] { false, true })
        foreach (var scrollable in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var mode in new[] { "elastic", "oversize", "undersize", "tall" })
            yield return Data($"static.{mode}.{native}.{scrollable}.{rtl}".ToLowerInvariant(), new(native, scrollable, rtl, mode));
    }
    public static IEnumerable<CaseData<TabIndicatorLayoutConfig>> DynamicConfigurations()
    {
        foreach (var native in new[] { false, true })
        foreach (var scrollable in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "reverse", "content" })
            yield return Data($"dynamic.{action}.{native}.{scrollable}.{rtl}".ToLowerInvariant(), new(native, scrollable, rtl, Action: action));
    }
    private static CaseData<TabIndicatorLayoutConfig> Data(string id, TabIndicatorLayoutConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 384, 180);
        var (scene, indicator) = Create(host, writer.Metadata.ReadConfig<TabIndicatorLayoutConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", Measure(host, indicator)));
    }
    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 384, 180);
        var config = writer.Metadata.ReadConfig<TabIndicatorLayoutConfig>(); var (scene, indicator) = Create(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, Measure(host, indicator), image));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        if (config.Action == "reverse")
        {
            Change(1000, "last", () => scene.Select(4)); Times(16000, 32000, 64000);
            Change(65000, "first", () => scene.Select(0)); Times(80000, 96000, 160000, 400000, 1000000);
        }
        else
        {
            Change(1000, "select-second", () => scene.Select(1)); Times(16000, 64000);
            Change(65000, "grow-first", () => scene.Labels[0].PreferredWidth = 112); Times(80000, 128000, 400000);
            Change(401000, "shrink-first", () => scene.Labels[0].PreferredWidth = 24); Times(416000, 480000, 1000000);
        }
        Advance(1100000); host.Draw(); Sample("passive-late", false);
    }
    private static (TabRowCases.TabScene Scene, LayoutIndicator Indicator) Create(AvaloniaHost host, TabIndicatorLayoutConfig config)
    {
        var scene = new TabRowCases.TabScene(host, new TabRowConfig(Scrollable: config.Scrollable, Native: config.Native, Rtl: config.Rtl, Count: 5));
        var indicator = new LayoutIndicator { Mode = config.Mode };
        TabRowAppearance.SetIndicator(scene.Row, new FuncControlTemplate((_, _) => indicator));
        return (scene, indicator);
    }
    private static ProbePoint[] Measure(AvaloniaHost host, LayoutIndicator indicator) =>
    [
        new("indicator", "indicator", null, AvaloniaMeasurements.Geometry(indicator.Child!, host.Window).Where(x => x.Name is "box" or "size").ToArray()),
        new("layout", "state", null,
        [
            Measurement.Numeric("selected", indicator.SelectedIndex, "index", definition: "tab-custom.selected-index"),
            Measurement.Numeric("minWidth", indicator.MinimumSize.Width, "dip", definition: "tab-custom.min-width"),
            Measurement.Numeric("maxWidth", indicator.MaximumSize.Width, "dip", definition: "tab-custom.max-width"),
            Measurement.Numeric("maxHeight", indicator.MaximumSize.Height, "dip", definition: "tab-custom.max-height"),
            Measurement.Numeric("selectedStart", indicator.TabPositions[indicator.SelectedIndex].Left, "dip", definition: "tab-custom.selected-start"),
            Measurement.Numeric("selectedEnd", indicator.TabPositions[indicator.SelectedIndex].Right, "dip", definition: "tab-custom.selected-end")
        ])
    ];

    // The sample's two independent Animatable<Dp> edges, with the same direction-dependent springs.
    private sealed class LayoutIndicator : TabIndicatorLayout
    {
        public string Mode { get; init; } = "elastic";
        private AnimationChildScope? _scope;
        private Animatable<float>? _start, _end;
        private float _startTarget, _endTarget;
        private double _left, _width;
        public LayoutIndicator() => Child = new Border { Background = new SolidColorBrush(Color.Parse("#8055AADD")) };
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e); _scope = AnimationScope.Get(this).CreateChild("Parity custom indicator");
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _scope?.Dispose(); _scope = null; _start = _end = null; base.OnDetachedFromVisualTree(e);
        }
        protected override Size MeasureIndicator(Size availableSize)
        {
            if (Mode != "elastic")
            {
                _left = 0; _width = Mode == "undersize" ? 20 : 315;
                var height = Mode == "undersize" ? 12 : Mode == "tall" ? 72 : 48;
                Child!.Measure(new Size(_width, height)); return new Size(_width, height);
            }
            var position = TabPositions[SelectedIndex];
            if (_start is null && _scope is { } scope)
            {
                _start = new Animatable<float>(new FloatConverter(), _startTarget = (float)position.Left, .01f);
                _end = new Animatable<float>(new FloatConverter(), _endTarget = (float)position.Right, .01f);
                scope.SubscribeValue(_start, _ => InvalidateMeasure()); scope.SubscribeValue(_end, _ => InvalidateMeasure());
            }
            if (_scope is { } motion && _start is { } start && _end is { } end)
            {
                Animate(end, (float)position.Right, ref _endTarget, true);
                Animate(start, (float)position.Left, ref _startTarget, false);
                void Animate(Animatable<float> value, float target, ref float requested, bool trailing)
                {
                    if (requested == target) return;
                    requested = target;
                    _ = motion.Launch(async ct =>
                    {
                        if (target != (trailing ? _endTarget : _startTarget)) return;
                        await value.AnimateToAsync(motion, target,
                            new SpringSpec<float>(1, (value.TargetValue < target) == trailing ? 1000 : 50, .01f), cancellationToken: ct);
                    }, "Custom edge");
                }
            }
            var density = (float)LayoutHelper.GetLayoutScale(this);
            int Px(double value) => (int)MathF.Floor((float)value * density + .5f);
            var startPx = Px(_start?.Value ?? position.Left); var endPx = Px(_end?.Value ?? position.Right);
            _left = (startPx - (IsScrollable ? (Px(position.Width) - Px(position.ContentWidth)) / 2 : 0)) / (double)density;
            _width = (endPx - startPx) / (double)density;
            Child!.Measure(new Size(_width, availableSize.Height)); return availableSize;
        }
        protected override Size ArrangeIndicator(Size finalSize)
        {
            Child!.Arrange(new Rect(_left, 0, _width, finalSize.Height)); return finalSize;
        }
    }
}
