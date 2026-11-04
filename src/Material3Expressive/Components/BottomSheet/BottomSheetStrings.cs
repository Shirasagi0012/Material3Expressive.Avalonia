// Bottom sheet semantics from Compose Multiplatform Core material3/l10n/*.kt,
// a9e47e8aa04c95cfe572ab4297c770f3cbfb234e. Copyright The Android Open Source Project.
// Licensed under Apache-2.0 (LICENSE.txt). JSON preserves the upstream locale mapping.
using System.Globalization;
using System.Text.Json;

namespace Material3Expressive.Components;

internal static class BottomSheetStrings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = Load();
    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        using var stream = typeof(BottomSheetStrings).Assembly.GetManifestResourceStream("Material3Expressive.Components.BottomSheet.BottomSheetStrings.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
    }
    internal static string Get(string key)
    {
        for (var culture = CultureInfo.CurrentUICulture; culture.Name.Length > 0; culture = culture.Parent)
            if (Translations.TryGetValue(culture.Name.ToLowerInvariant(), out var values)) return values[key];
        return Translations["en"][key];
    }
}
