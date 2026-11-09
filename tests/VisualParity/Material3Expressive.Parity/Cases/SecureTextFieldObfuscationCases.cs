using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record SecureObfuscationConfig(bool Outlined = false, string Mode = "RevealLastTyped", string Action = "typing");

[ParitySuite("secure-obfuscation")]
public sealed class SecureTextFieldObfuscationCases
{
    public static IEnumerable<CaseData<SecureObfuscationConfig>> Configurations()
    {
        foreach (var outlined in new[] { false, true })
        {
            foreach (var mode in new[] { "RevealLastTyped", "Hidden", "Visible", "System" })
            {
                var id = $"dynamic.{(outlined ? "outlined" : "filled")}.{mode.ToLowerInvariant()}.typing";
                yield return new(id, id, new(outlined, mode));
            }
            foreach (var action in new[] { "replacement", "multi", "program", "blur", "hidden", "supplementary", "disable" })
            {
                var id = $"dynamic.{(outlined ? "outlined" : "filled")}.{action}";
                yield return new(id, id, new(outlined, Action: action));
            }
        }
    }

    [ParityScenario("input", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SecureObfuscationConfig>();
        await using var host = new AvaloniaHost(writer, 360, 160, new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess));
        CaseDefaults.SetThemeColors(host, false);
        // Same fixed lightColorScheme inputs as the existing field decoration suite.
        host.Window.Resources[Sys.Color.Outline] = Color.Parse("#79747E");
        host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse("#E6E0E9");
        host.Window.Resources[Sys.Color.Error] = Color.Parse("#B3261E");
        var field = new SecureTextField
        {
            Width = 280,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextObfuscationMode = Enum.Parse<TextObfuscationMode>(config.Mode),
            // Cursor blinking is unrelated to obfuscation and uses a platform timer.
            // Both public APIs receive a transparent cursor; the actual text stays visible.
            CaretBrush = Brushes.Transparent,
            Theme = (ControlTheme)host.Window.FindResource(config.Outlined ? "MaterialOutlinedSecureTextFieldTheme" : "MaterialFilledSecureTextFieldTheme")!
        };
        var other = new Border { Width = 1, Height = 1, Focusable = true, HorizontalAlignment = HorizontalAlignment.Left };
        host.Mount(new Grid { Children = { other, field } }); host.Frame(0);
        field.Focus(); host.Draw(); host.Frame(0);
        long next = 16000;
        void Advance(long time)
        {
            while (next <= time) { host.Frame(next); next += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id)
        {
            var presenter = field.GetVisualDescendants().OfType<SecureTextPresenter>().Single();
            // Source UTF-16 buffers retain native editor indices. Read the actual
            // shaped display runs before their glyph clusters are projected back.
            var display = presenter.DisplayedText;
            writer.Submit(host.Snapshot(id, id, [new("field", "secure-field", null, [
                Measurement.Text("display", display, "text", "secure-field.displayed-text"),
                Measurement.Numeric("length", field.Text?.Length ?? 0, "utf16", definition: "secure-field.input-length"),
                Measurement.Flag("focused", field.IsFocused, definition: "secure-field.focused")])]));
        }
        Advance(16000); host.Window.KeyTextInput("a"); host.Draw(); host.Frame(16000); Sample("first-input");
        Advance(516000);
        switch (config.Action)
        {
            case "replacement": field.SelectAll(); host.Window.KeyTextInput("b"); break;
            case "multi": host.Window.KeyTextInput("cd"); break;
            case "program": field.Text = "z"; break;
            case "blur": other.Focus(); break;
            case "hidden": field.TextObfuscationMode = TextObfuscationMode.Hidden; break;
            case "supplementary": host.Window.KeyTextInput("😀"); break;
            case "disable": field.IsEnabled = false; break;
            default: host.Window.KeyTextInput("b"); break;
        }
        writer.Record(516000, "input", config.Action); host.Draw(); host.Frame(516000); Sample("second-input");
        foreach (var time in new long[] { 532000, 1515000, 1516000, 2015000, 2016000 })
        {
            Advance(time); host.Draw(); Sample($"time-{time}");
        }
    }
}
