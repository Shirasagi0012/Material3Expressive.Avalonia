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

[ParitySuite("time-picker-dialog")]
public sealed class TimePickerDialogCases
{
    public static IEnumerable<CaseData<PickerDialogConfig>> StaticConfigurations()
    {
        foreach (var dismiss in new[] { false, true })
        foreach (var toggle in new[] { false, true })
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
        foreach (var height in new[] { 199, 200, 201 })
            yield return Data($"static.landscape.boundary.{height}", new(ContentWidth: 400, ContentHeight: height));
        foreach (var height in new[] { 383, 384, 385 })
            yield return Data($"static.landscape.host.{height}", new(ContentWidth: 400, ContentHeight: 200, HostHeight: height));
        yield return Data("static.landscape.rtl", new(ContentWidth: 400, ContentHeight: 200, Rtl: true));
    }
    public static IEnumerable<CaseData<PickerDialogConfig>> DynamicConfigurations()
    {
        yield return Data("dynamic.toggle", new(Action: "toggle"));
        foreach (var action in new[] { "theme", "slots", "resize", "mode", "outside", "escape", "confirm", "dismiss", "transition", "interrupt", "immediate" })
            yield return Data($"dynamic.{action}", new(Action: action));
        foreach (var action in new[] { "outside", "escape" })
            yield return Data($"dynamic.{action}.disabled", new(Action: action, AllowDismiss: false));
        yield return Data("dynamic.transition.rtl", new(Action: "transition", Rtl: true));
    }
    static CaseData<PickerDialogConfig> Data(string id, PickerDialogConfig config) => new(id.ToLowerInvariant(), id.Replace('.', ' '), config with { Date = false });
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => PickerDialogScene.Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => PickerDialogScene.Capture(writer, true);
}
