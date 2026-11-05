using System.Globalization;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class DateTimeLocalizationTests
{
    [Theory]
    [InlineData("zh-CN", "小时值必须介于 1 到 12 间")]
    [InlineData("zh-Hans-CN", "小时值必须介于 1 到 12 间")]
    [InlineData("zh-TW", "小時值須介於 1 至 12 之間")]
    [InlineData("zh-Hant-HK", "小時必須介乎 1 至 12")]
    [InlineData("pt-PT", "A hora tem de ser 1-12")]
    [InlineData("fr-CA", "Heure entre 1 et 12")]
    [InlineData("as-IN", "ঘণ্টা ১–১২ৰ ভিতৰত হ’বই লাগিব")]
    [InlineData("id-ID", "Jam harus antara 1—12")]
    [InlineData("is-IS", "Klst. þarf að vera 1–12")]
    public void RegionalTranslationsUseCoreLanguageAndRegionChain(string culture, string expected) =>
        Assert.Equal(expected, DateTimeStrings.Get("TimePickerHourError", CultureInfo.GetCultureInfo(culture)));

    [Theory]
    [InlineData("zh", "Hour must be 1–12")]
    [InlineData("zh-Hant", "Hour must be 1–12")]
    [InlineData("de-AT", "Zulässige Werte: 1–12")]
    public void MissingRegionFallsBackToLanguageThenEnglish(string culture, string expected) =>
        Assert.Equal(expected, DateTimeStrings.Get("TimePickerHourError", CultureInfo.GetCultureInfo(culture)));
}
