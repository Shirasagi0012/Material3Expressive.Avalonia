// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:
// 1216-1225 and 1291-1388 apply ProvideTextStyleFromToken to direct ListItem slots.
// Avalonia maps that ambient Compose decorator to a leased template-priority TextStyle on a
// caller-supplied direct TextBlock; alpha22 visual reference can exercise the legacy static
// Click rows, while alpha25's line-aware height behavior remains parity unavailable.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialListItemTypographyTests
{
    [AvaloniaFact]
    public void DirectTextBlockSlots_ReceiveTheirComposeTypographyRolesAndMetrics()
    {
        var leading = new TextBlock { Text = "Leading" };
        var trailing = new TextBlock { Text = "On" };
        var overline = new TextBlock { Text = "Storage" };
        var headline = new TextBlock { Text = "Cloud backup" };
        var supporting = new TextBlock { Text = "Back up photos and videos" };
        var item = new MaterialListItem
        {
            Width = 240,
            LeadingContent = leading,
            TrailingContent = trailing,
            OverlineContent = overline,
            Content = headline,
            SupportingContent = supporting
        };

        ThemeTestHelper.ShowWindow(item);
        Dispatcher.UIThread.RunJobs();

        AssertSlotTypography(leading, TypographyRole.TitleMedium, fontSize: 16, lineHeight: 24, letterSpacing: 0.2);
        AssertSlotTypography(trailing, TypographyRole.LabelSmall, fontSize: 11, lineHeight: 16, letterSpacing: 0.5);
        AssertSlotTypography(overline, TypographyRole.LabelSmall, fontSize: 11, lineHeight: 16, letterSpacing: 0.5);
        AssertSlotTypography(headline, TypographyRole.BodyLarge, fontSize: 16, lineHeight: 24, letterSpacing: 0.5);
        AssertSlotTypography(supporting, TypographyRole.BodyMedium, fontSize: 14, lineHeight: 20, letterSpacing: 0.2);
    }

    [AvaloniaFact]
    public void StringSlots_KeepTheirOwnTypographyAfterTheTemplateAndDispatcherSettle()
    {
        const string headlineText = "Headline string";
        const string supportingText = "Supporting string";
        const string overlineText = "OVERLINE";
        const string trailingText = "Today";
        var item = new MaterialListItem
        {
            Width = 320,
            Content = headlineText,
            SupportingContent = supportingText,
            OverlineContent = overlineText,
            TrailingContent = trailingText
        };

        ThemeTestHelper.ShowWindow(item);
        Dispatcher.UIThread.RunJobs();

        var textByContent = item.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(textBlock => textBlock.Text is not null)
            .ToDictionary(textBlock => textBlock.Text!, StringComparer.Ordinal);

        AssertSlotTypography(textByContent[headlineText], TypographyRole.BodyLarge, fontSize: 16, lineHeight: 24, letterSpacing: 0.5);
        AssertSlotTypography(textByContent[supportingText], TypographyRole.BodyMedium, fontSize: 14, lineHeight: 20, letterSpacing: 0.2);
        AssertSlotTypography(textByContent[overlineText], TypographyRole.LabelSmall, fontSize: 11, lineHeight: 16, letterSpacing: 0.5);
        AssertSlotTypography(textByContent[trailingText], TypographyRole.LabelSmall, fontSize: 11, lineHeight: 16, letterSpacing: 0.5);
    }

    [AvaloniaFact]
    public void DirectTextBlockSlots_PreserveExplicitLocalTypographyAndLocalBinding()
    {
        var local = new TextBlock
        {
            Text = "Local",
            FontFamily = new FontFamily("Courier New"),
            FontWeight = FontWeight.Bold,
            FontSize = 31,
            LineHeight = 37,
            LetterSpacing = 1.75
        };
        TypographyScheme.SetTextStyle(local, TypographyRole.DisplaySmall);

        var localRoleOnly = new TextBlock { Text = "Local role" };
        TypographyScheme.SetTextStyle(localRoleOnly, TypographyRole.LabelLarge);

        var roles = new TestObservable<TypographyRole>(TypographyRole.TitleSmall);
        var bound = new TextBlock { Text = "Bound" };
        using var binding = bound.Bind(TypographyScheme.TextStyleProperty, roles);

        var item = new MaterialListItem
        {
            Width = 240,
            Content = local,
            LeadingContent = localRoleOnly,
            SupportingContent = bound
        };

        ThemeTestHelper.ShowWindow(item);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TypographyRole.DisplaySmall, TypographyScheme.GetTextStyle(local));
        Assert.Equal(new FontFamily("Courier New"), local.FontFamily);
        Assert.Equal(FontWeight.Bold, local.FontWeight);
        Assert.Equal(31, local.FontSize);
        Assert.Equal(37, local.LineHeight);
        Assert.Equal(1.75, local.LetterSpacing);
        AssertSlotTypography(localRoleOnly, TypographyRole.LabelLarge, fontSize: 14, lineHeight: 20, letterSpacing: 0.1);
        AssertSlotTypography(bound, TypographyRole.TitleSmall, fontSize: 14, lineHeight: 20, letterSpacing: 0.1);
        Assert.Equal(TypographyRole.TitleSmall, TypographyScheme.GetTextStyle(bound));

        roles.Publish(TypographyRole.HeadlineSmall);
        Dispatcher.UIThread.RunJobs();

        AssertSlotTypography(bound, TypographyRole.HeadlineSmall, fontSize: 24, lineHeight: 32, letterSpacing: 0);
    }

    [AvaloniaFact]
    public void DirectTextBlockSlot_TracksAnExplicitTextStyleBindingAddedAfterAttachment()
    {
        var supporting = new TextBlock { Text = "Initial supporting copy" };
        var item = new MaterialListItem
        {
            Width = 240,
            Content = "Headline",
            SupportingContent = supporting
        };

        ThemeTestHelper.ShowWindow(item);
        Dispatcher.UIThread.RunJobs();
        AssertSlotTypography(supporting, TypographyRole.BodyMedium, fontSize: 14, lineHeight: 20, letterSpacing: 0.2);

        var roles = new TestObservable<TypographyRole>(TypographyRole.HeadlineSmall);
        var binding = supporting.Bind(TypographyScheme.TextStyleProperty, roles);
        Dispatcher.UIThread.RunJobs();

        AssertSlotTypography(supporting, TypographyRole.HeadlineSmall, fontSize: 24, lineHeight: 32, letterSpacing: 0);

        roles.Publish(TypographyRole.LabelLarge);
        Dispatcher.UIThread.RunJobs();

        AssertSlotTypography(supporting, TypographyRole.LabelLarge, fontSize: 14, lineHeight: 20, letterSpacing: 0.1);

        binding.Dispose();
        Dispatcher.UIThread.RunJobs();

        AssertSlotTypography(supporting, TypographyRole.BodyMedium, fontSize: 14, lineHeight: 20, letterSpacing: 0.2);
    }

    [AvaloniaFact]
    public void DirectTextBlockSlotTypography_IsRemovedWhenTheItemLeavesTheVisualTree()
    {
        var trailing = new TextBlock { Text = "On" };
        var item = new MaterialListItem { Width = 240, TrailingContent = trailing };
        var host = new ContentControl { Content = item };

        ThemeTestHelper.ShowWindow(host);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TypographyRole.LabelSmall, TypographyScheme.GetTextStyle(trailing));
        Assert.True(trailing.GetBaseValue(TypographyScheme.TextStyleProperty).HasValue);

        var replacement = new TextBlock { Text = "Off" };
        item.TrailingContent = replacement;

        Assert.False(trailing.GetBaseValue(TypographyScheme.TextStyleProperty).HasValue);
        Assert.False(trailing.GetDiagnostic(TextBlock.LetterSpacingProperty).IsOverriddenCurrentValue);
        Assert.Equal(TypographyRole.LabelSmall, TypographyScheme.GetTextStyle(replacement));
        Assert.True(replacement.GetBaseValue(TypographyScheme.TextStyleProperty).HasValue);

        host.Content = new Border();

        Assert.False(replacement.GetBaseValue(TypographyScheme.TextStyleProperty).HasValue);
        Assert.False(replacement.GetDiagnostic(TextBlock.LetterSpacingProperty).IsOverriddenCurrentValue);
    }

    private static void AssertSlotTypography(
        TextBlock textBlock,
        TypographyRole role,
        double fontSize,
        double lineHeight,
        double letterSpacing)
    {
        Assert.Equal(role, TypographyScheme.GetTextStyle(textBlock));
        Assert.Equal(fontSize, textBlock.FontSize);
        Assert.Equal(lineHeight, textBlock.LineHeight);
        Assert.Equal(letterSpacing, textBlock.LetterSpacing);
    }

    private sealed class TestObservable<T>(T initialValue) : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = new();
        private T _value = initialValue;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            observer.OnNext(_value);
            return new Subscription(_observers, observer);
        }

        public void Publish(T value)
        {
            _value = value;
            foreach (var observer in _observers.ToArray())
                observer.OnNext(value);
        }

        private sealed class Subscription(List<IObserver<T>> observers, IObserver<T> observer) : IDisposable
        {
            public void Dispose() => observers.Remove(observer);
        }
    }
}
