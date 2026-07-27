using System.Text.Json;
using Avalonia.Media;
using Material3Expressive.VisualTesting.Storage;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialColorUtilities.DynamicColors;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class ColorContractTests
{
    [Fact]
    public void SharedComposeContractMatchesThePinnedMaterialColorUtilitiesScheme()
    {
        var contractPath = Path.Combine(
            RepositoryRoot.Find(),
            "visual-references",
            "color-contracts",
            "tonal-spot-spec2021-phone-seed-6750a4.json");
        using var document = JsonDocument.Parse(File.ReadAllText(contractPath));
        var root = document.RootElement;
        var scheme = new TonalSpotScheme(Color.Parse(root.GetProperty("seed").GetString()!))
        {
            SpecVersion = ColorSpec.SpecVersion.Spec2021,
            Platform = DynamicScheme.Platform.Phone
        };
        var resolved = new ResolvedColorScheme(scheme);
        var roles = root.GetProperty("roles");

        Assert.Equal("Spec2021", root.GetProperty("specVersion").GetString());
        Assert.Equal("Phone", root.GetProperty("platform").GetString());
        foreach (var token in Enum.GetValues<SysColorToken>().Where(item => item < SysColorToken.Custom))
        {
            var declared = roles.GetProperty(token.ToString());
            Assert.Equal(
                declared.GetProperty("light").GetString(),
                ToHex(resolved.GetColor(token, isDark: false)));
            Assert.Equal(
                declared.GetProperty("dark").GetString(),
                ToHex(resolved.GetColor(token, isDark: true)));
        }
    }

    [Fact]
    public void TonalSpotStillDefaultsToTheDeclaredSpecAndPlatform()
    {
        var scheme = new TonalSpotScheme(Color.Parse("#6750A4"));

        Assert.Equal(ColorSpec.SpecVersion.Spec2021, scheme.SpecVersion);
        Assert.Equal(DynamicScheme.Platform.Phone, scheme.Platform);
    }

    private static string ToHex(Color color) =>
        $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
