using Avalonia;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

[ParitySuite("chip-shape")]
public sealed class ChipShapeCases
{
    private static readonly string[] Sets = ["cut", "round-percent", "mixed", "generic"];
    public static IEnumerable<CaseData<ChipConfig>> StaticCases()
    {
        foreach (var kind in new[] { "filter", "elevated-filter", "input" })
        foreach (var shape in Sets)
        foreach (var rtl in new[] { false, true })
        foreach (var selected in new[] { false, true })
            yield return new($"static.{kind}.{shape}.{rtl.ToString().ToLowerInvariant()}.{selected.ToString().ToLowerInvariant()}", "State outline and logical corners",
                new(kind, Morph: true, Selected: selected, Rtl: rtl, Width: 160, ShapeSet: shape));
    }
    public static IEnumerable<CaseData<ChipConfig>> DynamicCases()
    {
        foreach (var kind in new[] { "filter", "elevated-filter", "input" })
        foreach (var shape in Sets)
        foreach (var action in new[] { "reverse-selection", "source-press", "resize", "shapes", "source-replace" })
            yield return new($"dynamic.{kind}.{shape}.{action}", "Shape interruption, keys and source lifetime",
                new(kind, Morph: true, Width: 160, ShapeSet: shape, Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new ChipCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicCases))]
    public Task Behavior(CaptureWriter writer) => new ChipCases().Behavior(writer);

    internal static ChipShapes Shapes(string set) => set switch
    {
        "cut" => new(new CutCornerShape { TopStart = new(12, RelativeUnit.Absolute), TopEnd = new(4, RelativeUnit.Absolute),
            BottomEnd = new(8, RelativeUnit.Absolute), BottomStart = new(2, RelativeUnit.Absolute) },
            new CutCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)), new CutCornerShape(4)),
        "round-percent" => new(new RoundedCornerShape { TopStart = new(.2, RelativeUnit.Relative), TopEnd = new(.1, RelativeUnit.Relative),
            BottomEnd = new(.4, RelativeUnit.Relative), BottomStart = new(.3, RelativeUnit.Relative) },
            new RoundedCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)), new RoundedCornerShape(new RelativeScalar(.1, RelativeUnit.Relative))),
        "mixed" => new(new CutCornerShape(12), new RoundedCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)), new CutCornerShape(4)),
        "generic" => new(MaterialShapeCases.CreateShape("curve"), new CutCornerShape(12), new RoundedCornerShape(4)),
        _ => throw new ArgumentOutOfRangeException(nameof(set))
    };
}
