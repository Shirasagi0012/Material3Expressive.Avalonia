using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using MaterialTypography.Roboto;

namespace Material3Expressive.Parity;

public sealed record ButtonConfig(bool Enabled, string Text);

[ParitySuite("button")]
public sealed class ButtonCases
{
    public static IEnumerable<CaseData<ButtonConfig>> StaticConfigurations()
    {
        foreach (var enabled in new[] { true, false })
        {
            var state = enabled ? "enabled" : "disabled";
            yield return new($"filled.{state}", enabled ? "Filled button defaults" : "Disabled filled button", new(enabled, "Save"));
        }
    }

    public static IEnumerable<CaseData<ButtonConfig>> PressConfigurations() =>
        [new("filled.press", "Press and release timeline", new(true, "Save"))];


    static Button BuildButton(AvaloniaHost host, ButtonConfig config) => new()
    {
        Content = config.Text,
        FontFamily = new(RobotoFontCollection.FamilyUri),
        IsEnabled = config.Enabled,
        Theme = (ControlTheme)host.Window.FindResource("MaterialFilledButtonTheme")!
    };

    static ProbePoint[] ReadButton(Button button, AvaloniaHost host, int clicks)
    {
        // Named template parts are bound explicitly. Do not silently select the first TextBlock in a tree.
        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().Single(x => x.Name == "PART_ContentPresenter");
        if (presenter.Child is not TextBlock text) throw new InvalidOperationException("String content is not a TextBlock.");
        return
        [
            new("button", "allocation", null, [
                .. AvaloniaMeasurements.Geometry(button, host.Window),
                Measurement.Flag("enabled", button.IsEnabled), Measurement.Numeric("clicks", clicks, "count")
            ]),
            AvaloniaMeasurements.Text("label", "container", text, host.Window), .. ButtonMeasurements.Read(button, host)
        ];
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task ButtonStatic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonConfig>();
        await using var host = new AvaloniaHost(writer);
        var button = BuildButton(host, config);
        host.Mount(CaseDefaults.Center(button));
        host.Frame(-2000);
        host.Frame(-1000);
        host.Frame(0);
        writer.Submit(host.Snapshot("initial", config.Enabled ? "enabled" : "disabled", ReadButton(button, host, 0)));
    }

    [ParityScenario("press", Data = nameof(PressConfigurations))]
    public async Task ButtonPressed(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonConfig>();
        await using var host = new AvaloniaHost(writer);
        var button = BuildButton(host, config);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        host.Mount(CaseDefaults.Center(button));
        host.Frame(-2000);
        host.Frame(-1000);
        host.Frame(0);
        writer.Submit(host.Snapshot("initial", config.Enabled ? "enabled" : "disabled", ReadButton(button, host, clicks)));
        var box = AvaloniaMeasurements.Geometry(button, host.Window)[0].Numbers!;
        var center = new Point(box[0] + box[2] / 2, box[1] + box[3] / 2);
        host.Input("move", center);
        host.Input("down", center);
        foreach (var time in new long[] { 16000, 32000, 80000 })
        {
            host.Frame(time);
            writer.Submit(host.Snapshot("pressed-" + time, "pressed", ReadButton(button, host, clicks), time != 32000));
        }

        host.AdvanceTo(88000);
        host.Input("up", center);
        host.Frame(96000);
        writer.Submit(host.Snapshot("released", "released", ReadButton(button, host, clicks)));
        host.Frame(300000);
        writer.Submit(host.Snapshot("late-300000", "released", ReadButton(button, host, clicks)));
    }

}


    public static class ButtonMeasurements
    {
        static object? Member(object owner, string name)
        {
            for (var type = owner.GetType(); type is not null; type = type.BaseType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                if (type.GetField(name, flags) is { } field) return field.GetValue(owner);
                if (type.GetProperty(name, flags) is { } property) return property.GetValue(owner);
            }

            throw new MissingMemberException(owner.GetType().FullName, name);
        }

        public static ProbePoint[] Read(Button button, AvaloniaHost host)
        {
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");
            var chrome = (Border)Member(surface, "_chrome")!;
            var controller = Member(surface, "_interactionController") ??
                             throw new InvalidOperationException("Button has no attached interaction controller.");
            var corners = chrome.CornerRadius;
            var fields = new List<Measurement>(AvaloniaMeasurements.Geometry(surface, host.Window));
            fields.Add(Measurement.Vector("nativeCornerRadii", "quad",
                new[] { corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft }.SelectMany(r => new[] { r, r })
                    .ToArray(),
                "dip", "container", "motion", "avalonia.border.raw-corner-radii.xy", "PART_Container._chrome.CornerRadius",
                ["topLeft.x", "topLeft.y", "topRight.x", "topRight.y", "bottomRight.x", "bottomRight.y", "bottomLeft.x", "bottomLeft.y"]));
            fields.Add(chrome.Background is ISolidColorBrush brush
                ? Measurement.Vector("fillColor", "color",
                    [brush.Color.R / 255d, brush.Color.G / 255d, brush.Color.B / 255d, brush.Color.A / 255d * brush.Opacity],
                    "srgb", "none", "properties", "container.fill.solid-srgba", "PART_Container._chrome.Background")
                : Measurement.Unavailable("fillColor", "properties", "color", "srgb", "none", "Chrome has no solid background.",
                    "container.fill.solid-srgba"));
            // Filled Button has no border. Use the same native rounded-rectangle conversion as Avalonia's
            // Skia Border renderer; it normalizes Full and overlapping radii without an empirical clamp.
            if (chrome.BorderThickness == default(Thickness))
            {
                using var rounded = new RoundedRect(new Rect(chrome.Bounds.Size), corners).ToSKRoundRect();
                fields.Add(Measurement.Vector("cornerRadii", "quad", rounded.Radii.SelectMany(r => new double[] { r.X, r.Y }).ToArray(),
                    "dip", "container", "motion", "container.rounded-outline.corner-radii.xy",
                    "derived: actual chrome Bounds/CornerRadius -> Avalonia Skia ToSKRoundRect -> native normalized radii; border thickness=0",
                    [
                        "topLeft.x", "topLeft.y", "topRight.x", "topRight.y", "bottomRight.x", "bottomRight.y", "bottomLeft.x",
                        "bottomLeft.y"
                    ]));
            }
            else
                fields.Add(Measurement.Unavailable("cornerRadii", "motion", "quad", "dip", "container",
                    "Border inset and complex border paths need a separate renderer reader.", "container.rounded-outline.corner-radii.xy"));

            fields.Add(Measurement.Numeric("shadowElevation", surface.ShadowElevation, "dip", category: "properties",
                definition: "container.shadow-elevation", source: "PART_Container.ShadowElevation"));
            var interaction = (InteractionState)Member(controller, "Interaction")!;
            var feedback = new List<Measurement>
            {
                Measurement.Flag("pressed", interaction.HasFlag(InteractionState.Pressed), definition: "interaction.press.active",
                    source: "MaterialInteractionController.Interaction"),
                Measurement.Flag("hovered", interaction.HasFlag(InteractionState.Hovered), definition: "interaction.hover.active",
                    source: "MaterialInteractionController.Interaction"),
                Measurement.Numeric("stateLayerOpacity", (double)Member(controller, "StateLayerOpacity")!, "ratio", category: "motion",
                    definition: "interaction.state-layer.alpha", source: "MaterialInteractionController.StateLayerOpacity"),
                Measurement.Unavailable("hitBounds", "geometry", "rect", "dip", "scene", "No complete native hit-region reader installed.",
                    "interaction.hit-region.aabb")
            };
            var ripples = ((IEnumerable)Member(controller, "_ripples")!).Cast<RippleAnimation>().ToArray();
            feedback.Add(Measurement.Numeric("rippleCount", ripples.Length, "count", definition: "interaction.active-ripple-count",
                source: "MaterialInteractionController._ripples"));
            if (ripples.Length > 1)
                throw new InvalidOperationException("This single-press example needs event identities before observing multiple ripples.");
            foreach (var ripple in ripples)
            {
                feedback.AddRange([
                    Measurement.Numeric("ripple.0.alphaFraction", ripple.Value, "ratio", category: "motion",
                        definition: "ripple.animation.alpha-fraction", source: "RippleAnimation.Value (including early-release override)"),
                    Measurement.Numeric("ripple.0.radius", ripple.Radius, "dip", "container", "motion", "ripple.draw.radius",
                        "RippleAnimation.Radius"),
                    Measurement.Vector("ripple.0.center", "point", [ripple.Center.X, ripple.Center.Y], "dip", "container", "motion",
                        "ripple.draw.center", "RippleAnimation.Center"),
                    Measurement.Flag("ripple.0.running", ripple.IsRunning, "motion", "ripple.animation.running",
                        "RippleAnimation.IsRunning")
                ]);
            }

            return
            [
                new("container", "container", "button", fields.ToArray()),
                new("interaction", "interaction", "container", feedback.ToArray())
            ];
        }
    }
