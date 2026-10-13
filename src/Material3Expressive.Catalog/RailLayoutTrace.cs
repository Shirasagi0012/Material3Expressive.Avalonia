using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

// Temporary diagnostics for the native Catalog. Only observes the existing layout
// and animation values; it never sends frames, invalidates layout or changes sizing.
internal sealed class RailLayoutTrace : IDisposable
{
    private readonly Window _window;
    private readonly WideNavigationRail[] _rails;
    private readonly Stopwatch _time = new();
    private readonly List<object> _samples = [];
    private readonly DispatcherTimer _finishTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Dictionary<(Type Type, string Name), FieldInfo> _fields = [];
    private WideNavigationRail? _activeRail;
    private double _lastMotionMs;
    private int _pass;
    private string? _path;

    internal RailLayoutTrace(Window window, params WideNavigationRail[] rails)
    {
        _window = window;
        _rails = rails;
        _window.LayoutUpdated += OnLayoutUpdated;
        foreach (var rail in rails) rail.State.PropertyChanged += OnStateChanged;
        _finishTimer.Tick += OnFinishTimer;
    }

    private void OnStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != WideNavigationRailState.TargetValueProperty) return;
        var rail = _rails.First(r => ReferenceEquals(r.State, sender));
        if (_activeRail is not null) Flush("interrupted-by-new-target");
        _activeRail = rail;
        _pass = 0;
        _lastMotionMs = 0;
        _time.Restart();
        var folder = Path.Combine(Path.GetTempPath(), "Material3Expressive", "rail-layout-traces");
        _path = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}Z-{rail.Name}-{rail.State.TargetValue}-{Guid.NewGuid():N}.jsonl");
        _samples.Add(new
        {
            kind = "start", rail = rail.Name, target = rail.State.TargetValue.ToString(),
            durationScale = AnimationScope.GetDurationScale(_window), scale = _window.RenderScaling,
            timebase = "Stopwatch elapsed ms from target change; pass is a layout observation, not a presented frame",
            coordinates = "window client physical pixels; unrounded double values; boxes are x,y,width,height",
            layoutModel = "continuous vertical geometry with Compose integer endpoints",
            assembly = typeof(WideNavigationRail).Assembly.ManifestModule.ModuleVersionId.ToString()
        });
        Capture("target-changed-before-layout");
        _finishTimer.Start();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_activeRail is not null) Capture("layout-updated");
    }

    private void Capture(string stage)
    {
        var rail = _activeRail!;
        var scale = _window.RenderScaling;
        var items = rail.Items.OfType<WideNavigationRailItem>().Select((item, index) =>
        {
            var layout = FindItemLayout(item);
            var railLayout = layout?.GetVisualAncestors().OfType<Control>()
                .FirstOrDefault(c => c.GetType().Name == "WideNavigationRailLayout");
            var itemsPanel = layout?.GetVisualAncestors().OfType<Control>()
                .FirstOrDefault(c => c.GetType().Name == "WideNavigationRailItemsPanel");
            var parts = layout?.GetVisualDescendants().OfType<Control>().ToArray() ?? [];
            var icon = parts.FirstOrDefault(c => c.Name == "PART_Icon");
            var indicator = parts.FirstOrDefault(c => c.Name == "PART_Indicator");
            var label = parts.FirstOrDefault(c => c.Name == "PART_Label");
            return new
            {
                index, label = item.Label?.ToString(), item = Box(item), layout = Box(layout),
                icon = Box(icon), iconContent = Box(item.Icon as Control), indicator = Box(indicator),
                labelPresenter = Box(label), labelText = Box(label?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()),
                itemsPanel = Box(itemsPanel), railLayout = Box(railLayout),
                railMotion = railLayout is null ? null : new
                {
                    minimumHeightDp = Read<float>(railLayout, "_minimumHeight"),
                    spacingDp = Read<float>(railLayout, "_spacing"),
                    minimumHeightPx = railLayout is null ? (double?)null : item.GetValue(
                        (AttachedProperty<double>)railLayout.GetType().GetField("MinimumItemHeightProperty",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!) * scale,
                    spacingPx = itemsPanel is null ? (double?)null : (double)itemsPanel.GetType().GetProperty("Spacing", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(itemsPanel)!
                },
                calculated = layout is null ? null : new
                {
                    positionProgress = (float)layout.GetType().GetProperty("IconPositionProgress")!.GetValue(layout)!,
                    inheritedMinimumHeightPx = railLayout is null ? (double?)null : item.GetValue(
                        (AttachedProperty<double>)railLayout.GetType().GetField("MinimumItemHeightProperty",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!) * scale,
                    paddingDp = Padding(Read<Thickness>(layout, "_tracePadding")),
                    innerSizePx = SizeArray(Read<Size>(layout, "_inner")),
                    innerHeightBeforeEndpointCorrectionPx = Read<double>(layout, "_traceInnerHeightRaw"),
                    iconBoxPx = RectArray(Read<Rect>(layout, "_iconBox")),
                    indicatorBoxPx = RectArray(Read<Rect>(layout, "_indicatorBox")),
                    labelBoxPx = RectArray(Read<Rect>(layout, "_labelBox")),
                    iconYBeforeEndpointCorrectionPx = Read<double>(layout, "_traceIconYRaw"),
                    centerOffsetYBeforeEndpointCorrectionPx = Read<double>(layout, "_traceCenterOffsetYRaw"),
                    centerOffsetYPx = Read<double>(layout, "_traceCenterOffsetY")
                }
            };
        }).ToArray();
        _samples.Add(new
        {
            kind = "sample", tMs = _time.Elapsed.TotalMilliseconds, pass = ++_pass, stage, scale,
            target = rail.State.TargetValue.ToString(), stateAnimating = rail.State.IsAnimating,
            rail = Box(rail), items
        });

        object? Box(Control? control)
        {
            if (control is null) return null;
            var transform = control.TransformToVisual(_window);
            var windowBox = transform is { } matrix ? new Rect(control.Bounds.Size).TransformToAABB(matrix) : (Rect?)null;
            return new
            {
                windowPx = windowBox is { } box ? RectArray(new Rect(box.X * scale, box.Y * scale, box.Width * scale, box.Height * scale)) : null,
                localPx = RectArray(new Rect(control.Bounds.X * scale, control.Bounds.Y * scale, control.Bounds.Width * scale, control.Bounds.Height * scale)),
                desiredPx = new[] { control.DesiredSize.Width * scale, control.DesiredSize.Height * scale },
                rounding = control.UseLayoutRounding, visible = control.IsVisible, attached = control.IsAttachedToVisualTree()
            };
        }
    }

    private void OnFinishTimer(object? sender, EventArgs e)
    {
        if (_activeRail is not { } rail) return;
        var now = _time.Elapsed.TotalMilliseconds;
        var moving = rail.State.IsAnimating;
        if (rail is ModalWideNavigationRail modal)
            moving |= Running(modal, "_position", "_offset", "_alpha");
        foreach (var item in rail.Items.OfType<WideNavigationRailItem>())
        {
            moving |= Running(item, "_position", "_selection");
            if (FindItemLayout(item)?.GetVisualAncestors().FirstOrDefault(v => v.GetType().Name == "WideNavigationRailLayout") is { } layout)
                moving |= Running(layout, "_minWidth", "_widthFullRange", "_itemSpacing", "_itemHeight");
        }
        if (moving) _lastMotionMs = now;
        if (now > Math.Max(15_000, 5_000 * AnimationScope.GetDurationScale(_window)) || _samples.Count >= 20_000)
        {
            Capture("capture-limit");
            Flush("capture-limit");
        }
        else if (now >= 500 && now - _lastMotionMs >= 350)
        {
            Capture("settled");
            Flush("settled");
        }
    }

    private static Control? FindItemLayout(WideNavigationRailItem item) => item.GetVisualDescendants().OfType<Control>()
        .FirstOrDefault(c => c.GetType().Name == "WideNavigationRailItemLayout");
    private bool Running(object owner, params string[] names) => names.Any(name => InMotion(Read<object?>(owner, name)));
    private static bool InMotion(object? value) => value switch
    {
        Animatable<float> animatable => animatable.IsRunning,
        AnimatedState<float> state => state.IsRunning || state.TargetValue != state.Value,
        _ => false
    };

    private T Read<T>(object owner, string name)
    {
        var key = (owner.GetType(), name);
        if (!_fields.TryGetValue(key, out var field))
        {
            field = key.Item1.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Rail diagnostics expected {key.Item1.Name}.{name}; update the temporary reader to match the component.");
            _fields.Add(key, field);
        }
        return (T)field.GetValue(owner)!;
    }

    private void Flush(string reason)
    {
        _finishTimer.Stop();
        _samples.Add(new { kind = "end", reason, tMs = _time.Elapsed.TotalMilliseconds, samples = _pass });
        var lines = _samples.Select(sample => JsonSerializer.Serialize(sample)).ToArray();
        _samples.Clear();
        _activeRail = null;
        _time.Stop();
        foreach (var line in lines) Debug.WriteLine($"RailTrace {line}");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path!)!);
            File.WriteAllLines(_path!, lines, new UTF8Encoding(false));
            Debug.WriteLine($"RailTrace saved: {_path}");
            Console.WriteLine($"RailTrace saved: {_path}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"RailTrace file output failed: {e.Message}");
            Console.Error.WriteLine($"RailTrace file output failed: {e.Message}");
        }
    }

    private static double[] RectArray(Rect r) => [r.X, r.Y, r.Width, r.Height];
    private static double[] SizeArray(Size s) => [s.Width, s.Height];
    private static double[] Padding(Thickness p) => [p.Left, p.Top, p.Right, p.Bottom];

    public void Dispose()
    {
        _window.LayoutUpdated -= OnLayoutUpdated;
        foreach (var rail in _rails) rail.State.PropertyChanged -= OnStateChanged;
        _finishTimer.Tick -= OnFinishTimer;
        _finishTimer.Stop();
        if (_activeRail is not null) Flush("window-closed");
    }
}
