using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record SecureImeConfig(bool Outlined = false, string Finish = "cancel");

[ParitySuite("secure-ime")]
public sealed class SecureTextFieldImeCases
{
    public static IEnumerable<CaseData<SecureImeConfig>> Configurations()
    {
        foreach (var outlined in new[] { false, true })
        foreach (var finish in new[] { "cancel", "commit" })
        {
            var id = $"dynamic.{(outlined ? "outlined" : "filled")}.{finish}";
            yield return new(id, id, new(outlined, finish));
        }
    }

    [ParityScenario("composition", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SecureImeConfig>();
        await using var host = new AvaloniaHost(writer, 360, 160, new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess));
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.Outline] = Color.Parse("#79747E");
        host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse("#E6E0E9");
        host.Window.Resources[Sys.Color.Error] = Color.Parse("#B3261E");
        var field = new SecureTextField
        {
            Text = "abc", Width = 280, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, CaretBrush = Brushes.Transparent,
            Theme = (ControlTheme)host.Window.FindResource(config.Outlined ? "MaterialOutlinedSecureTextFieldTheme" : "MaterialFilledSecureTextFieldTheme")!
        };
        host.Mount(new Grid { Children = { field } }); host.Frame(0);
        field.Focus(); field.CaretIndex = 3; host.Draw(); host.Frame(0);
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        field.RaiseEvent(request);
        var client = request.Client ?? throw new InvalidOperationException("Secure field did not open its native IME client.");
        var presenter = field.GetVisualDescendants().OfType<SecureTextPresenter>().Single();
        long next = 16000;
        void Advance(long time)
        {
            while (next <= time) { host.Frame(next); next += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("field", "secure-field", null, [
            Measurement.Text("display", presenter.DisplayedText, "text", "secure-field.displayed-text"),
            Measurement.Numeric("committed-length", field.Text?.Length ?? 0, "utf16", definition: "secure-field.committed-length"),
            Measurement.Numeric("composition-length", presenter.PreeditText?.Length ?? 0, "utf16", definition: "secure-field.composition-length"),
            Measurement.Flag("focused", field.IsFocused, definition: "secure-field.focused")])]));
        void Preedit(string? value)
        {
            client.SetPreeditText(value, value?.Length);
            writer.Record(host.TimeUs, "ime", $"composing={value?.Length ?? 0}");
            host.Draw(); host.Frame(host.TimeUs);
        }
        Advance(16000); host.Window.KeyTextInput("d"); host.Draw(); host.Frame(16000); Sample("typed-before-composition");
        Advance(516000);
        for (var i = 0; i < 4; i++)
        {
            host.Window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.None, null);
            host.Window.KeyRelease(Key.Left, RawInputModifiers.None, PhysicalKey.None, null);
        }
        if (field.CaretIndex != 0) throw new InvalidOperationException("Native keyboard did not move before the revealed character.");
        Preedit("x"); Sample("preedit-before-reveal");
        Advance(716000); Preedit("😀"); Sample("preedit-supplementary");
        Advance(916000); Preedit("yz"); Sample("preedit-multiple");
        Advance(1116000); Preedit(null);
        if (config.Finish == "commit")
        {
            writer.Record(1116000, "ime", "commit utf16=1 through native TextInput");
            host.Window.KeyTextInput("z");
        }
        host.Draw(); host.Frame(1116000); Sample($"composition-{config.Finish}");
        foreach (var time in new long[] { 1515000, 1516000, 2615000, 2616000 })
        {
            Advance(time); host.Draw(); Sample($"time-{time}");
        }
    }
}
