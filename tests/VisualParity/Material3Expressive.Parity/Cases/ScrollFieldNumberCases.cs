using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ScrollFieldNumberConfig(string Locale = "en-US", int Index = 7, bool Reverse = false);

[ParitySuite("scroll-field-number")]
public sealed class ScrollFieldNumberCases
{
    public static IEnumerable<CaseData<ScrollFieldNumberConfig>> Configurations()
    {
        foreach (var locale in new[] { "en-US", "ar-EG", "fa-IR" })
        foreach (var reverse in new[] { false, true })
        {
            var id = $"dynamic.{locale.ToLowerInvariant()}.{(reverse ? "reverse" : "select")}";
            yield return new(id, id, new(locale, Reverse: reverse));
        }
    }
    [ParityScenario("dynamic", Data = nameof(Configurations))]
    public async Task Dynamic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ScrollFieldNumberConfig>();
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(config.Locale);
            await using var host = new AvaloniaHost(writer, 200, 120, new ManualTimeProvider());
            var number = new ScrollFieldNumber { Index = config.Index, SelectedContentColor = Colors.Black,
                UnselectedContentColor = Colors.Blue, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            host.Mount(new Grid { Children = { number } }); host.Frame(0);
            void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("number", "number", null, [
                Measurement.Text("content", number.Text!, category: "text", definition: "scroll-field-number.content"),
                Measurement.Numeric("fontSize", number.FontSize, "sp", category: "text", definition: "scroll-field-number.fontSize"),
                Measurement.Numeric("weight", (int)number.FontWeight, "weight", category: "text", definition: "scroll-field-number.weight")])]));
            Sample("initial");
            host.AdvanceTo(16000); number.IsSelected = true;
            for (long time = 16000; time <= 640000; time += 16000)
            {
                if (time == 80000 && config.Reverse) { host.AdvanceTo(time); number.IsSelected = false; }
                host.Frame(time);
                if (time <= 160000 || time % 80000 == 0) Sample($"time-{time}");
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
