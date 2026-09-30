using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using MaterialSymbols;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;
using Button = Avalonia.Controls.Button;

namespace Material3Expressive.Parity;

public sealed class ParityApplication : Application;
public static class ParityAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<ParityApplication>()
        .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions
        { UseHeadlessDrawing = false, ShouldRenderOnUIThread = true, Fps = 60 })
        .UseRoundedSymbols().WithRobotoFont();
}

public static class AvaloniaCapture
{
    public static async Task<Capture> RunAsync(Scenario s, string input, string output, string root)
    {
        s.Validate();
        if (s.Density != 1 || s.FontScale != 1)
            throw new NotSupportedException("Density/fontScale other than 1 are not implemented in this runner.");
        if (Directory.Exists(output)) throw new IOException($"Output already exists: {output}");
        Directory.CreateDirectory(output);
        using var clock = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock)
        {
            Width = s.ViewportWidth, Height = s.ViewportHeight,
            Background = Brush.Parse(s.Background), RequestedThemeVariant = s.Dark ? ThemeVariant.Dark : ThemeVariant.Light,
            FlowDirection = s.Direction == "RTL" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        var failures = new List<string>();
        window.AnimationRoot.AnimationFailed += (_, e) => failures.Add($"{e.OperationLabel}: {e.Exception}");
        window.AnimationRoot.DurationScale = 1;
        var clicks = 0;
        Control control;
        var ink = Brush.Parse(s.Ink);
        var font = new FontFamily(RobotoFontCollection.FamilyUri);
        var motionCanvas = new Canvas();
        Control? content = null;
        switch (s.Component)
        {
            case "surface":
                control = new MaterialSurface { Width = s.Width, Height = s.Height, CornerRadius = new(s.Radius),
                    Background = ink, ShadowElevation = s.Elevation };
                break;
            case "button":
                content = s.Content == "slot" ? new Border { Width = 18, Height = 18, Background = Brushes.White } : null;
                var button = new Button { Content = (object?)content ?? s.Text, FontFamily = font };
                control = button;
                var theme = s.Variant switch { "filled" => "MaterialFilledButtonTheme", "tonal" => "MaterialTonalButtonTheme",
                    "outlined" => "MaterialOutlinedButtonTheme", "text" => "MaterialTextButtonTheme", "elevated" => "MaterialElevatedButtonTheme",
                    _ => throw new NotSupportedException(s.Variant) };
                button.Theme = (ControlTheme)window.FindResource(theme)!;
                button.Background = s.Variant is "outlined" or "text" ? Brushes.Transparent : ink;
                button.Foreground = s.Variant is "outlined" or "text" ? ink : Brushes.White;
                button.Click += (_, _) => clicks++;
                break;
            case "iconbutton":
                var icon = new IconButton { Symbol = MaterialSymbolCodepoint.Home };
                control = icon;
                var iconTheme = s.Variant switch { "filled" => "MaterialFilledIconButtonTheme", "tonal" => "MaterialTonalIconButtonTheme",
                    "outlined" => "MaterialOutlinedIconButtonTheme", "standard" => "MaterialStandardIconButtonTheme",
                    _ => throw new NotSupportedException(s.Variant) };
                icon.Theme = (ControlTheme)window.FindResource(iconTheme)!;
                icon.Background = s.Variant is "outlined" or "standard" ? Brushes.Transparent : ink;
                icon.Foreground = s.Variant is "outlined" or "standard" ? ink : Brushes.White;
                icon.Click += (_, _) => clicks++;
                break;
            case "text":
                control = new TextBlock { Width = s.Width, Text = s.Text, FontFamily = font, Foreground = ink,
                    FontSize = s.FontSize, FontWeight = (FontWeight)s.FontWeight, LineHeight = s.LineHeight,
                    LetterSpacing = s.LetterSpacing,
                    TextWrapping = s.Wrap ? TextWrapping.Wrap : TextWrapping.NoWrap };
                break;
            case "motion":
                control = new Border { Width = 18, Height = 18, Background = ink };
                Canvas.SetLeft(control, 24); Canvas.SetTop(control, 64);
                motionCanvas.Children.Add(control);
                break;
            default: throw new NotSupportedException(s.Component);
        }
        control.IsEnabled = s.Enabled;
        if (s.Component != "motion")
        {
            control.HorizontalAlignment = HorizontalAlignment.Center;
            control.VerticalAlignment = VerticalAlignment.Center;
        }
        window.Content = s.Component == "motion" ? motionCanvas : new Grid { Children = { control } };
        var frames = new List<FrameObservation>();
        Animatable<double>? value = null;
        bool detached = false;
        try
        {
            window.Show();
            Render();
            // Same explicit pre-input mounting timestamps as Compose.
            clock.SendFrame(TimeSpan.FromMilliseconds(998)); Dispatcher.UIThread.RunJobs(); Render();
            clock.SendFrame(TimeSpan.FromMilliseconds(999)); Dispatcher.UIThread.RunJobs(); Render();
            if (s.Component == "motion")
            {
                var scope = VisualAnimationScope.Create(control);
                value = scope.CreateAnimatable(0d);
                scope.SubscribeValue(value, x => Canvas.SetLeft(control, 24 + x));
            }
            var eventIndex = 0;
            foreach (var (time, index) in s.FramesUs.Select((t, i) => (t, i)))
            {
                while (eventIndex < s.Events.Length && s.Events[eventIndex].AtUs <= time)
                {
                    var e = s.Events[eventIndex++];
                    var b = ReadBounds(control, window);
                    var point = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
                    switch (e.Action)
                    {
                        case "down": window.MouseDown(point, MouseButton.Left); break;
                        case "up": window.MouseUp(point, MouseButton.Left); break;
                        case "move": window.MouseMove(point); break;
                        case "leave": window.MouseMove(new Point(-1, -1)); break;
                        case "disable": control.IsEnabled = false; break;
                        case "detach":
                            if (control.Parent is Panel parent) parent.Children.Remove(control);
                            detached = true;
                            break;
                        case "target":
                            if (value is null) throw new InvalidDataException("target requires motion fixture.");
                            IAnimationSpec<double> spec = s.MotionSpec == "spring"
                                ? new SpringSpec<double>(stiffness: 1600, visibilityThreshold: .01)
                                : new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: ComposeEasings.Linear);
                            _ = window.AnimationRoot.Launch(ct => value.AnimateToAsync(e.Value, spec, cancellationToken: ct), "Motion probe");
                            break;
                    }
                    Dispatcher.UIThread.RunJobs();
                }
                clock.SendFrame(TimeSpan.FromTicks(10_000_000 + time * 10));
                Dispatcher.UIThread.RunJobs();
                Render();
                var nodes = detached ? [] : ReadNodes(s, control, content, window);
                var values = value is null ? new Dictionary<string, double>() : new()
                { ["value"] = value.Value, ["velocity"] = value.Velocity, ["running"] = value.IsRunning ? 1 : 0 };
                string? image = null;
                string? withoutText = null;
                if (s.Checkpoints.Contains(index))
                {
                    image = $"frames/{index:D4}.png";
                    Directory.CreateDirectory(Path.Combine(output, "frames"));
                    // Passive read: never use CaptureRenderedFrame, which pumps render ticks.
                    using var bitmap = window.GetLastRenderedFrame() ?? throw new IOException("No rendered frame.");
                    using var stream = File.Create(Path.Combine(output, image));
                    bitmap.Save(stream, PngBitmapEncoderOptions.Default);
                    var label = control as TextBlock ?? control.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
                    if (!detached && label is not null)
                    {
                        var suppression = label.SetValue(Visual.OpacityProperty, 0d, BindingPriority.Animation)
                            ?? throw new InvalidOperationException("Cannot create temporary text paint suppression.");
                        try
                        {
                            // Paint suppression preserves the measured tree and never sends a clock frame.
                            Render();
                            withoutText = $"without-text/{index:D4}.png";
                            Directory.CreateDirectory(Path.Combine(output, "without-text"));
                            using var background = window.GetLastRenderedFrame() ?? throw new IOException("No diagnostic frame.");
                            using var diagnostic = File.Create(Path.Combine(output, withoutText));
                            background.Save(diagnostic, PngBitmapEncoderOptions.Default);
                        }
                        finally { suppression.Dispose(); Render(); }
                        var after = ReadNodes(s, control, content, window);
                        if (System.Text.Json.JsonSerializer.Serialize(nodes, ParityJson.Options) != System.Text.Json.JsonSerializer.Serialize(after, ParityJson.Options))
                            throw new InvalidOperationException("Text paint suppression changed measured layout.");
                    }
                }
                frames.Add(new(index, time, image, nodes, values, clicks, control.IsEnabled, withoutText));
            }
        }
        finally
        {
            window.Close();
            await window.AnimationRoot.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            if (!window.AnimationRoot.IsEnding) failures.Add("Window close did not end animation environment.");
            if (window.AnimationRoot.ActiveTaskCount != 0 || window.AnimationRoot.ActiveChildCount != 0 || window.AnimationRoot.PendingAnimationCount != 0)
                failures.Add("Animation environment retained work after cleanup.");
        }
        var environment = new Dictionary<string, string>
        {
            ["platform"] = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : "macos",
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            ["density"] = "1", ["fontScale"] = "1", ["colorSpace"] = "sRGB",
            ["fontSha256"] = ParityJson.Hash(Path.Combine(root, "modules/MaterialTypography.Roboto/Assets/Roboto.ttf")),
            ["symbolSha256"] = ParityJson.Hash(Path.Combine(root, "modules/MaterialSymbols/assets/MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf")),
            ["avalonia"] = typeof(Application).Assembly.GetName().Version!.ToString(),
            ["skia"] = typeof(SkiaSharp.SKBitmap).Assembly.GetName().Version!.ToString(),
            ["composeAnimation"] = typeof(VisualAnimationScope).Assembly.GetName().Version!.ToString(),
            ["composeAnimationDllSha256"] = ParityJson.Hash(typeof(VisualAnimationScope).Assembly.Location),
            ["composeAnimationRuntimeDllSha256"] = ParityJson.Hash(typeof(Animatable<double>).Assembly.Location),
            ["composeAnimationTestSupportDllSha256"] = ParityJson.Hash(typeof(ManualAnimationFrameClock).Assembly.Location),
            ["materialDllSha256"] = ParityJson.Hash(typeof(MaterialSurface).Assembly.Location),
            ["runnerDllSha256"] = ParityJson.Hash(typeof(AvaloniaCapture).Assembly.Location),
            ["applicationBinaries"] = string.Join("\n", Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll", SearchOption.AllDirectories)
                .Order().Select(x => Path.GetRelativePath(AppContext.BaseDirectory, x).Replace('\\', '/') + ":" + ParityJson.Hash(x))),
            ["dependencyLockSha256"] = ParityJson.Hash(Path.Combine(root, "tests/VisualParity/Material3Expressive.Parity/packages.lock.json")),
            ["dotnet"] = Environment.Version.ToString()
        };
        var capabilities = new List<string> { "layout", "raster", "behavior", "text", "resolvedFont" };
        if (s.Component == "text" || s.Component == "button" && s.Content == "text") capabilities.Add("textLayers");
        if (s.Component == "motion") capabilities.Add("motion");
        var capture = new Capture("avalonia", ParityJson.Hash(input), ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")),
            environment, capabilities.ToArray(), frames.ToArray(), failures.ToArray());
        ParityJson.Write(Path.Combine(output, "observations.json"), capture);
        ParityJson.Write(Path.Combine(output, "trace.json"), new { bootstrapUs = new[] { -2000, -1000 }, events = s.Events,
            frames = frames.Select(x => new { x.Index, x.TimeUs, x.Values, x.Clicks, x.Enabled }), errors = failures });
        ParityJson.Seal(output, "avalonia", environment);
        return capture;
    }

    private static void Render()
    {
        // Bounded progress; exhausting the bound is an infrastructure failure, never success.
        for (var turn = 0; turn < 20; turn++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            if (!Dispatcher.UIThread.HasJobsWithPriority(DispatcherPriority.SystemIdle)) return;
        }
        throw new TimeoutException("Layout/render did not settle within 20 turns.");
    }
    private static Bounds ReadBounds(Control control, Window root)
    {
        var position = control.TranslatePoint(default, root) ?? throw new InvalidOperationException("Detached probe.");
        return new(position.X, position.Y, control.Bounds.Width, control.Bounds.Height);
    }
    private static Node[] ReadNodes(Scenario s, Control control, Control? content, Window window)
    {
        var nodes = new List<Node>();
        if (s.Component != "text") nodes.Add(new(s.Component is "button" or "iconbutton" ? "allocation" : "container", ReadBounds(control, window)));
        if (content is not null) nodes.Add(new("content", ReadBounds(content, window)));
        var block = control as TextBlock ?? control.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        if (block is not null)
        {
            var b = ReadBounds(block, window);
            var lines = new List<TextLine>();
            var top = b.Y;
            foreach (var line in block.TextLayout.TextLines)
            {
                lines.Add(new(top + line.Baseline, line.Width, line.Height, line.HasCollapsed));
                top += line.Height;
            }
            var fonts = block.TextLayout.TextLines.SelectMany(x => x.TextRuns).OfType<ShapedTextRun>()
                .Select(x => x.GlyphRun.GlyphTypeface.FamilyName).Distinct().ToArray();
            nodes.Add(new("label", b, new(block.Text ?? "", block.FontSize, (int)block.FontWeight, lines.ToArray(), fonts, block.LetterSpacing)));
        }
        if (s.Component == "iconbutton")
        {
            var icon = control.GetVisualDescendants().OfType<Material3Expressive.Foundation.Rendering.MaterialSymbol>().First();
            nodes.Add(new("icon", ReadBounds(icon, window)));
        }
        return nodes.ToArray();
    }
}
