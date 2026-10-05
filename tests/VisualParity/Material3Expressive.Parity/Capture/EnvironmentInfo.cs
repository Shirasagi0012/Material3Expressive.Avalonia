using Avalonia;
using Avalonia.Platform;
using System.Security.Cryptography;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public static class EnvironmentInfo
{
    public static Dictionary<string, string> Read(string root)
    {
        var environment = new Dictionary<string, string>
        {
            ["platform"] = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : "macos",
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            ["density"] = AvaloniaHost.RenderScale.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["fontScale"] = "1",
            ["colorSpace"] = "sRGB",
            ["fontSha256"] = ParityJson.Hash(Path.Combine(root, "modules/MaterialTypography.Roboto/Assets/Roboto-Regular.ttf")),
            ["fontMediumSha256"] = ParityJson.Hash(Path.Combine(root, "modules/MaterialTypography.Roboto/Assets/Roboto-Medium.ttf")),
            ["fontBoldSha256"] = ParityJson.Hash(Path.Combine(root, "modules/MaterialTypography.Roboto/Assets/Roboto-Bold.ttf")),
            ["symbolSha256"] = SymbolHash(),
            ["avalonia"] = typeof(Application).Assembly.GetName().Version!.ToString(),
            ["skia"] = typeof(SkiaSharp.SKBitmap).Assembly.GetName().Version!.ToString(),
            ["composeAnimation"] = typeof(VisualAnimationScope).Assembly.GetName().Version!.ToString(),
            ["composeAnimationDllSha256"] = ParityJson.Hash(typeof(VisualAnimationScope).Assembly.Location),
            ["composeAnimationRuntimeDllSha256"] = ParityJson.Hash(typeof(Animatable<double>).Assembly.Location),
            ["composeAnimationTestSupportDllSha256"] = ParityJson.Hash(typeof(ManualAnimationFrameClock).Assembly.Location),
            ["materialDllSha256"] = ParityJson.Hash(typeof(MaterialSurface).Assembly.Location),
            ["runnerDllSha256"] = ParityJson.Hash(typeof(CaptureWriter).Assembly.Location),
            ["applicationBinaries"] = string.Join("\n",
                Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll", SearchOption.AllDirectories)
                    .Order().Select(x => Path.GetRelativePath(AppContext.BaseDirectory, x).Replace('\\', '/') + ":" + ParityJson.Hash(x))),
            ["dependencyLockSha256"] =
                ParityJson.Hash(Path.Combine(root, "tests/VisualParity/Material3Expressive.Parity/packages.lock.json")),
            ["dotnet"] = Environment.Version.ToString()
        };
        return environment;
    }

    private static string SymbolHash()
    {
        using var font = new StandardAssetLoader().Open(new Uri($"avares://{typeof(MaterialSymbols.Avalonia.RoundedSymbols).Assembly.GetName().Name}/Assets/MaterialSymbolsRounded.ttf"));
        return Convert.ToHexStringLower(SHA256.HashData(font));
    }
}
