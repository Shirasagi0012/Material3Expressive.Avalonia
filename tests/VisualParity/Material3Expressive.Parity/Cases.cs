using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using MaterialTypography.Roboto;
namespace Material3Expressive.Parity;
// These are example test authors' entries/helpers, not a framework scene registry.
public static class Cases
{
    public static IReadOnlyDictionary<string, Func<CaptureWriter, Task>> Entries { get; } =
        new Dictionary<string, Func<CaptureWriter, Task>>
        {
            [nameof(SurfaceStatic)] = SurfaceStatic,
            [nameof(ButtonEnabled)] = ButtonEnabled,
            [nameof(ButtonDisabled)] = ButtonDisabled,
            [nameof(ButtonPressed)] = ButtonPressed,
            [nameof(TextWrapped)] = TextWrapped,
            [nameof(MotionInterrupted)] = MotionInterrupted,
            [nameof(DirectObservation)] = DirectObservation
        };
    static Control Center(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        return new Grid { Children = { control } };
    }
    public static async Task SurfaceStatic(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer);
        var surface = new MaterialSurface { Width = 100, Height = 50, CornerRadius = new(12), Background = Brush.Parse("#6750A4") };
        host.Mount(Center(surface)); host.Frame(-2000); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "mounted", [new("surface", "container", null,
            [.. AvaloniaMeasurements.Geometry(surface, host.Window),
                Measurement.Vector("requestedColor", "color", [103, 80, 164, 255], "srgba8", "none", "properties", source: "declared")])]));
    }
    static Button BuildButton(AvaloniaHost host, bool enabled = true) => new()
    {
        Content = "Save",
        FontFamily = new(RobotoFontCollection.FamilyUri),
        IsEnabled = enabled,
        Theme = (ControlTheme)host.Window.FindResource("MaterialFilledButtonTheme")!
    };
    static ProbePoint[] ReadButton(Button button, AvaloniaHost host, int clicks)
    {
        // Named template parts are bound explicitly. Do not silently select the first TextBlock in a tree.
        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().Single(x => x.Name == "PART_ContentPresenter");
        if (presenter.Child is not TextBlock text) throw new InvalidOperationException("String content is not a TextBlock.");
        return [new("button", "allocation", null, [.. AvaloniaMeasurements.Geometry(button, host.Window),
            Measurement.Flag("enabled", button.IsEnabled), Measurement.Numeric("clicks", clicks, "count")]),
            AvaloniaMeasurements.Text("label", "button", text, host.Window)];
    }
    public static async Task ButtonEnabled(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer); var button = BuildButton(host);
        host.Mount(Center(button)); host.Frame(-2000); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "enabled", ReadButton(button, host, 0)));
    }
    public static async Task ButtonDisabled(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer); var button = BuildButton(host, false);
        host.Mount(Center(button)); host.Frame(-2000); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "disabled", ReadButton(button, host, 0)));
    }
    public static async Task ButtonPressed(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer); var button = BuildButton(host); var clicks = 0;
        button.Click += (_, _) => clicks++;
        host.Mount(Center(button)); host.Frame(-2000); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "enabled", ReadButton(button, host, clicks)));
        var box = AvaloniaMeasurements.Geometry(button, host.Window)[0].Numbers!;
        var center = new Point(box[0] + box[2] / 2, box[1] + box[3] / 2);
        host.Input("move", center); host.Input("down", center);
        foreach (var time in new long[] { 16000, 32000, 80000 })
        {
            host.Frame(time); writer.Submit(host.Snapshot("pressed-" + time, "pressed", ReadButton(button, host, clicks), time != 32000));
        }
        host.AdvanceTo(88000); host.Input("up", center); host.Frame(96000);
        writer.Submit(host.Snapshot("released", "released", ReadButton(button, host, clicks)));
        host.Frame(300000); writer.Submit(host.Snapshot("settled", "released", ReadButton(button, host, clicks)));
    }
    public static async Task TextWrapped(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer);
        var text = new TextBlock
        {
            Text = "Compose parity\nWrap a longer line with spaces.",
            Width = 120,
            FontFamily = new(RobotoFontCollection.FamilyUri),
            FontSize = 14,
            FontWeight = FontWeight.Normal,
            LineHeight = 20,
            LetterSpacing = 0,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#6750A4")
        };
        host.Mount(Center(text)); host.Frame(-2000); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "wrapped", [AvaloniaMeasurements.Text("label", null, text, host.Window)]));
    }
    public static async Task MotionInterrupted(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer);
        var marker = new Border { Width = 18, Height = 18, Background = Brush.Parse("#6750A4") };
        Canvas.SetLeft(marker, 24); Canvas.SetTop(marker, 64);
        host.Mount(new Canvas { Children = { marker } }); host.Frame(-2000); host.Frame(-1000);
        var scope = VisualAnimationScope.Create(marker); var value = scope.CreateAnimatable(0d);
        scope.SubscribeValue(value, x => Canvas.SetLeft(marker, 24 + x));
        void Target(double target)
        {
            writer.Record(host.TimeUs, "target", target.ToString(System.Globalization.CultureInfo.InvariantCulture));
            _ = host.Window.AnimationRoot.Launch(ct => value.AnimateToAsync(target,
                new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: ComposeEasings.Linear), cancellationToken: ct), "Motion probe");
        }
        void Sample(string id, bool image) => writer.Submit(host.Snapshot(id, "interrupted-tween", [new("marker", "container", null,
            [.. AvaloniaMeasurements.Geometry(marker, host.Window), Measurement.Numeric("value", value.Value, "dip", category: "motion"),
                Measurement.Numeric("velocity", value.Velocity, "dip/s", category: "motion"),
                Measurement.Flag("running", value.IsRunning, "motion")])], image));
        Target(100);
        foreach (var time in new long[] { 0, 16000, 40000 }) { host.Frame(time); Sample("forward-" + time, time != 16000); }
        Target(-20);
        foreach (var time in new long[] { 56000, 80000, 140000, 160000 }) { host.Frame(time); Sample("reverse-" + time, time != 80000); }
    }
    public static Task DirectObservation(CaptureWriter writer)
    {
        // Author-owned data and a transparent pixel fixture; no scene or animation host.
        var path = "images/pixels.png"; Directory.CreateDirectory(Path.Combine(writer.Output, "images"));
        using var bitmap = new SkiaSharp.SKBitmap(new SkiaSharp.SKImageInfo(2, 1, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, new SkiaSharp.SKColor(80, 120, 200, 0)); bitmap.SetPixel(1, 0, new SkiaSharp.SKColor(81, 121, 201, 128));
        using (var png = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)) File.WriteAllBytes(Path.Combine(writer.Output, path), png.ToArray());
        var raster = PixelData.FromPng(writer.Output, "full", path) with { Contribution = "declared-pixel-fixture" };
        writer.Submit(new("initial", 0, "author-built", [new("custom", "measurement", null,
            [Measurement.Numeric("answer", 42, "count", source: "declared")])], [raster]));
        return Task.CompletedTask;
    }
}
