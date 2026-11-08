using Material3Expressive.Components;

namespace Material3Expressive.Parity;

[ParitySuite("carousel-shape")]
public sealed class CarouselShapeCases
{
    public static IEnumerable<CaseData<CarouselConfig>> ClassicConfigurations()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var shape in new[] { "asymmetric", "percent", "rounded", "round-percent", "offset", "curve", "hole" })
            yield return new($"classic.{variant.ToLowerInvariant()}.{shape}.{(rtl ? "rtl" : "ltr")}",
                "Mask outline at changing keyline sizes", new(variant, rtl, Shape: shape, Border: 2.25));
        // All classic variants share rememberMaskShape, but have different moving masks.
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var action in new[] { "animate", "replace-shape" })
            yield return new($"classic.{variant.ToLowerInvariant()}.{action}", "Shape lifetime and moving border cache",
                new(variant, Shape: "asymmetric", Border: 2.25, Action: action));
        yield return new("classic.thick", "Stroke clamp uses full item size", new(Shape: "percent", Border: 80));
    }

    public static IEnumerable<CaseData<MultiAspectCarouselConfig>> MultiConfigurations()
    {
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var shape in new[] { "asymmetric", "percent", "rounded", "round-percent", "offset", "curve", "hole" })
            yield return new($"multi.{(vertical ? "vertical" : "horizontal")}.{shape}.{(rtl ? "rtl" : "ltr")}",
                "Multi aspect generic mask and border", new(vertical, rtl, Shape: shape, Border: 2.25));
        foreach (var vertical in new[] { false, true })
        foreach (var action in new[] { "scroll", "resize", "replace-shape" })
            yield return new($"multi.{(vertical ? "vertical" : "horizontal")}.{action}", "Independent clip and remembered border",
                new(vertical, Shape: "asymmetric", Border: 2.25, Action: action));
        yield return new("multi.thick", "Stroke clamp uses full item size", new(Shape: "percent", Border: 80));
    }

    [ParityScenario("classic", Data = nameof(ClassicConfigurations))]
    public Task Classic(CaptureWriter writer) => CarouselCases.Capture(writer);
    [ParityScenario("multi", Data = nameof(MultiConfigurations))]
    public Task Multi(CaptureWriter writer) => MultiAspectCarouselCases.Capture(writer);
}
