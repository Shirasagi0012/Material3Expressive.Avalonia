// Date/time subset of Compose Multiplatform Core material3/l10n/*.kt,
// a9e47e8aa04c95cfe572ab4297c770f3cbfb234e. Copyright The Android Open Source Project.
// Licensed under Apache-2.0 (LICENSE.txt). JSON preserves upstream translations;
// positional printf placeholders are converted to .NET composite-format placeholders.
using System.Globalization;
using System.Text.Json;

namespace Material3Expressive.Components;

internal static class DateTimeStrings
{
    private static readonly Dictionary<string,Dictionary<string,string>> Translations = Load();
    private static Dictionary<string,Dictionary<string,string>> Load()
    {
        using var stream=typeof(DateTimeStrings).Assembly.GetManifestResourceStream("Material3Expressive.Components.DateTimePicker.DateTimeStrings.json")!;
        return JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(stream)!;
    }
    internal static string Get(string key,CultureInfo? culture=null,params object[] args)
    {
        culture ??= CultureInfo.CurrentUICulture;
        // Fixed Core localeTagChain uses language + region, then language, then English.
        // Scripts do not replace the region (zh-Hant-HK must still resolve zh-HK).
        var language=culture.TwoLetterISOLanguageName.ToLowerInvariant();
        language=language switch {"he"=>"iw","id"=>"in",_=>language};
        var region=!culture.IsNeutralCulture && culture.Name.Length>0
            ? new RegionInfo(culture.Name).TwoLetterISORegionName.ToLowerInvariant() : null;
        string? text=null;
        if(region is not null && Translations.TryGetValue($"{language}-{region}",out var regional))
            regional.TryGetValue(key,out text);
        if(text is null && Translations.TryGetValue(language,out var neutral))neutral.TryGetValue(key,out text);
        text ??= Translations["en"][key];
        return args.Length==0?text:string.Format(culture,text,args);
    }
}
