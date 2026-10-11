using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record PickerDialogConfig(bool Date = false, bool Dismiss = true, bool Toggle = true,
    bool Dark = false, bool Rtl = false, bool PlatformWidth = false, double? Width = null, double? Height = null,
    int HostWidth = 640, int HostHeight = 640, double ContentWidth = 256, double ContentHeight = 300,
    double ButtonWidth = 80, string Colors = "default", string Action = "none", bool AllowDismiss = true);

[ParitySuite("date-picker-dialog")]
public sealed class DatePickerDialogCases
{
    public static IEnumerable<CaseData<PickerDialogConfig>> StaticConfigurations()
    {
        foreach (var dismiss in new[] { false, true })
        foreach (var toggle in new[] { false })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.slots.{dismiss}.{toggle}.{dark}", new(Dismiss: dismiss, Toggle: toggle, Dark: dark));
        foreach (var rtl in new[] { false, true })
        {
            if (rtl) yield return Data("static.row.true", new(Rtl: true)); // LTR is in the slot matrix.
            yield return Data($"static.large-actions.{rtl}", new(Rtl: rtl, ButtonWidth: 210));
            yield return Data($"static.short-host.{rtl}", new(Rtl: rtl, HostHeight: 180, ContentHeight: 700));
            yield return Data($"static.narrow-host.{rtl}", new(Rtl: rtl, HostWidth: 240));
        }
        yield return Data("static.height.explicit", new(Height: 500));
        yield return Data("static.width.explicit-small", new(Width: 180));
        yield return Data("static.width.explicit-large", new(Width: 500));
        yield return Data("static.platform-width", new(PlatformWidth: true, HostHeight: 420));
        yield return Data("static.input", new(ContentHeight: 100));
        yield return Data("static.tall-content", new(ContentHeight: 800));
        foreach (var color in new[] { "custom", "surface", "transparent", "primary" })
            yield return Data($"static.colors.{color}", new(Colors: color));
    }
    public static IEnumerable<CaseData<PickerDialogConfig>> DynamicConfigurations()
    {
        foreach (var action in new[] { "theme", "slots", "resize", "mode", "outside", "escape", "confirm", "dismiss", "transition", "interrupt", "immediate" })
            yield return Data($"dynamic.{action}", new(Action: action));
        foreach (var action in new[] { "outside", "escape" })
            yield return Data($"dynamic.{action}.disabled", new(Action: action, AllowDismiss: false));
        yield return Data("dynamic.transition.rtl", new(Action: "transition", Rtl: true));
    }
    static CaseData<PickerDialogConfig> Data(string id, PickerDialogConfig config) => new(id.ToLowerInvariant(), id.Replace('.', ' '), config with { Date = true });
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => PickerDialogScene.Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => PickerDialogScene.Capture(writer, true);
}
