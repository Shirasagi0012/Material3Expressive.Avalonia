using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.VisualTesting.Cases;
using Material3Expressive.VisualTesting.Compose;
using Material3Expressive.VisualTesting.Storage;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class ComposeReferenceContractTests
{
    [AvaloniaFact]
    public void EveryPlannedComposeCaseHasTheSameAvaloniaContract()
    {
        var paths = new RepositoryPaths();
        var manifest = new ComposeReferenceStore(paths).ReadManifest();

        foreach (var referenceCase in manifest.Cases)
        {
            var avaloniaCase = VisualCaseCatalog.All.SingleOrDefault(
                item => string.Equals(item.Id, referenceCase.Id, StringComparison.Ordinal));
            Assert.NotNull(avaloniaCase);
            Assert.Equal(
                new PixelSize(referenceCase.Width, referenceCase.Height),
                avaloniaCase.Viewport);
            Assert.Equal(referenceCase.Theme == "dark", avaloniaCase.Theme.IsDark);
            Assert.Equal(1, avaloniaCase.RenderScaling);
            Assert.Equal(manifest.Environment.Locale, avaloniaCase.Culture);
            Assert.Equal(FlowDirection.LeftToRight, avaloniaCase.FlowDirection);
            Assert.Equal(manifest.ColorContract.Variant, avaloniaCase.Theme.SchemeVariant);
            Assert.Equal(
                manifest.ColorContract.SpecVersion,
                avaloniaCase.Theme.SpecVersion.ToString());
            Assert.Equal(
                manifest.ColorContract.Platform,
                avaloniaCase.Theme.Platform.ToString());
            Assert.Equal(
                manifest.ColorContract.Seed,
                $"#{avaloniaCase.Theme.SeedColor.R:X2}" +
                $"{avaloniaCase.Theme.SeedColor.G:X2}" +
                $"{avaloniaCase.Theme.SeedColor.B:X2}");
        }
    }
}
