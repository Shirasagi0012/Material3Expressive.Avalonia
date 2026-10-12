using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

// Do not measure the native editor: these checks exercise real template/resource/property
// paths without asking the platform to resolve a glyph typeface.
public class TextFieldTypographyTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void CompleteStyles_PreserveBaselineAndClampedInterpolation(bool outlined, bool secure)
    {
        using var fixture = new Fixture(outlined, secure);
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        Assert.Equal(16, expanded.FontSize);
        Assert.Equal(12, floating.FontSize);
        var middle = TextFieldLayout.InterpolateLabelStyle(expanded, floating, .5f)!;
        Assert.Equal(14, middle.FontSize);
        Assert.Equal(20, middle.LineHeight);
        Assert.Equal(.45, middle.LetterSpacing!.Value, 10);
        Assert.Equal(FontWeight.Normal, middle.FontWeight);
        Assert.Equal(16, TextFieldLayout.InterpolateLabelStyle(expanded, floating, -.5f)!.FontSize);
        Assert.Equal(12, TextFieldLayout.InterpolateLabelStyle(expanded, floating, 1.5f)!.FontSize);
        Assert.False(expanded.IsVisible); Assert.False(floating.IsVisible);
        Assert.Null(expanded.Content); Assert.Null(floating.Content);
        Assert.Equal(default, expanded.DesiredSize); Assert.Equal(default, floating.DesiredSize);
        Assert.NotNull(TopLevel.GetTopLevel(fixture.Layout));
    }

    [AvaloniaTheory]
    [InlineData(0f)] [InlineData(.49f)] [InlineData(.5f)] [InlineData(1f)]
    public void DiscreteTypography_ReachesBothEffectiveEndpoints_AndNativeOverridesClearToCurrentProgress(float progress)
    {
        using var fixture = new Fixture();
        fixture.Box.Resources[Sys.TypeScale.BodyLarge.Style] = new TextStyle
        {
            FontSize = 16, LineHeight = 24, FontStyle = FontStyle.Normal,
            FontStretch = FontStretch.Condensed, TextAlignment = TextAlignment.Left,
        };
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle
        {
            FontSize = 12, LineHeight = 16, FontStyle = FontStyle.Italic,
            FontStretch = FontStretch.Expanded, TextAlignment = TextAlignment.Right,
        };
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        var label = fixture.Endpoint("Label");
        var expectedStyle = progress < .5f ? FontStyle.Normal : FontStyle.Italic;
        var expectedStretch = progress < .5f ? FontStretch.Condensed : FontStretch.Expanded;
        var expectedAlignment = progress < .5f ? TextAlignment.Left : TextAlignment.Right;
        var output = TextFieldLayout.InterpolateLabelStyle(expanded, floating, progress)!;
        Assert.Equal(expectedStyle, output.FontStyle);
        Assert.Equal(expectedStretch, output.FontStretch);
        Assert.Equal(expectedAlignment, output.TextAlignment);
        MaterialText.SetStyle(label, output);
        Assert.Equal(expectedStyle, label.FontStyle);
        Assert.Equal(expectedStretch, label.FontStretch);
        Assert.Equal(expectedAlignment, label.GetValue(TextBlock.TextAlignmentProperty));
        label.FontStyle = FontStyle.Oblique;
        label.FontStretch = FontStretch.UltraCondensed;
        label.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        MaterialText.SetStyle(label, TextFieldLayout.InterpolateLabelStyle(expanded, floating, progress));
        Assert.Equal(FontStyle.Oblique, label.FontStyle);
        Assert.Equal(FontStretch.UltraCondensed, label.FontStretch);
        Assert.Equal(TextAlignment.Center, label.GetValue(TextBlock.TextAlignmentProperty));
        Assert.Equal(FontStyle.Normal, expanded.FontStyle);
        Assert.Equal(FontStyle.Italic, floating.FontStyle);
        Assert.Equal(FontStretch.Condensed, expanded.FontStretch);
        Assert.Equal(FontStretch.Expanded, floating.FontStretch);
        label.ClearValue(ContentPresenter.FontStyleProperty);
        label.ClearValue(ContentPresenter.FontStretchProperty);
        label.ClearValue(TextBlock.TextAlignmentProperty);
        Assert.Equal(expectedStyle, label.FontStyle);
        Assert.Equal(expectedStretch, label.FontStretch);
        Assert.Equal(expectedAlignment, label.GetValue(TextBlock.TextAlignmentProperty));
    }

    [AvaloniaFact]
    public void SparseRole_UsesOwnerEffectiveContext_WithoutInventingSmallMetrics()
    {
        using var fixture = new Fixture();
        fixture.Box.FontSize = 20;
        fixture.Box.LineHeight = 31;
        fixture.Box.LetterSpacing = 2;
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { FontWeight = FontWeight.Bold };
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        Assert.Equal(16, expanded.FontSize); // The complete large role explicitly supplies 16.
        Assert.Equal(20, floating.FontSize);
        Assert.Equal(31, floating.GetValue(TextBlock.LineHeightProperty));
        Assert.Equal(2, floating.GetValue(TextElement.LetterSpacingProperty));
        Assert.Equal(FontWeight.Bold, floating.FontWeight);
        var middle = TextFieldLayout.InterpolateLabelStyle(expanded, floating, .5f)!;
        Assert.Equal(18, middle.FontSize);
        Assert.Equal(27.5, middle.LineHeight);
        Assert.Equal(1.25, middle.LetterSpacing);
        fixture.Box.ClearValue(TextBox.FontSizeProperty);
        Assert.Equal(16, floating.FontSize);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { FontSize = 13 };
        Assert.Equal(13, floating.FontSize);
        Assert.Equal(31, floating.GetValue(TextBlock.LineHeightProperty));
        Assert.Equal(FontWeight.Normal, floating.FontWeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { LineHeight = 19 };
        Assert.Equal(16, floating.FontSize);
        Assert.Equal(19, floating.GetValue(TextBlock.LineHeightProperty));
    }

    [AvaloniaFact]
    public void EndpointInputs_DoNotReadLabelOutput_AndNativeOverridesRestoreCurrentInterpolation()
    {
        using var fixture = new Fixture();
        fixture.Box.Resources[Sys.TypeScale.BodyLarge.Style] = new TextStyle { FontWeight = FontWeight.Normal };
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { FontSize = 12 };
        fixture.Box.FontSize = 20;
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        var label = fixture.Endpoint("Label");
        var native = new Style { Setters = { new Setter(ContentPresenter.FontSizeProperty, 26d) } };
        label.Styles.Add(native); label.ApplyStyling();
        for (var i = 0; i < 5; i++)
        {
            var output = TextFieldLayout.InterpolateLabelStyle(expanded, floating, .5f)!;
            Assert.Equal(16, output.FontSize);
            MaterialText.SetStyle(label, output);
            Assert.Equal(26, label.FontSize);
            Assert.Equal(20, expanded.FontSize);
        }
        label.FontSize = 28;
        MaterialText.SetStyle(label, TextFieldLayout.InterpolateLabelStyle(expanded, floating, .75f));
        Assert.Equal(28, label.FontSize);
        label.ClearValue(ContentPresenter.FontSizeProperty);
        Assert.Equal(26, label.FontSize);
        label.Styles.Remove(native);
        Assert.Equal(14, label.FontSize);
        var source = new Border { Tag = 35d };
        using (label.Bind(ContentPresenter.FontSizeProperty, new Binding(nameof(Control.Tag)) { Source = source }))
        {
            MaterialText.SetStyle(label, TextFieldLayout.InterpolateLabelStyle(expanded, floating, .5f));
            Assert.Equal(35, label.FontSize);
            source.Tag = 36d; Assert.Equal(36, label.FontSize);
        }
        Assert.Equal(16, label.FontSize);
    }

    [AvaloniaFact]
    public void SparseEndpoints_TrackNativeInheritanceAndRestoreAggregateDefaults()
    {
        using var fixture = new Fixture();
        fixture.Host.SetValue(TextElement.FontSizeProperty, 23d);
        MaterialText.SetStyle(fixture.Box, TextStyle.Empty);
        fixture.Layout.LargeTextStyle = TextStyle.Empty;
        fixture.Layout.SmallTextStyle = new TextStyle { FontWeight = FontWeight.Bold };
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        Assert.Equal(23, fixture.Box.FontSize);
        Assert.Equal(23, expanded.FontSize); Assert.Equal(23, floating.FontSize);
        fixture.Host.SetValue(TextElement.FontSizeProperty, 31d);
        Assert.Equal(31, expanded.FontSize); Assert.Equal(31, floating.FontSize);
        fixture.Host.ClearValue(TextElement.FontSizeProperty);
        Assert.Equal(fixture.Box.FontSize, floating.FontSize);
        fixture.Box.ClearValue(MaterialText.StyleProperty); // restores the theme BodyLarge role
        Assert.Equal(16, expanded.FontSize); Assert.Equal(16, floating.FontSize); // a sparse endpoint inherits unspecified fields
    }

    [AvaloniaFact]
    public void ResourcesFontScaleAndTheme_ReevaluateBothIndependentEndpoints()
    {
        using var fixture = new Fixture();
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        fixture.Box.Resources[Sys.TypeScale.FontScale] = 1.5d;
        Assert.Equal(24, expanded.FontSize); Assert.Equal(18, floating.FontSize);
        fixture.Box.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
            { [Sys.TypeScale.BodySmall.Style] = new TextStyle { FontWeight = FontWeight.Bold } };
        fixture.Box.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
            { [Sys.TypeScale.BodySmall.Style] = new TextStyle { FontSize = 11, LineHeight = 18 } };
        Assert.Equal(24, floating.FontSize);
        fixture.Window.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.Equal(11, floating.FontSize);
        Assert.Equal(18, floating.GetValue(TextBlock.LineHeightProperty));
        Assert.Equal(24, expanded.FontSize);
        fixture.Box.Resources.ThemeDictionaries.Clear();
        Assert.Equal(18, floating.FontSize);
        fixture.Box.Resources.Remove(Sys.TypeScale.FontScale);
        Assert.Equal(16, expanded.FontSize); Assert.Equal(12, floating.FontSize);
    }

    [AvaloniaFact]
    public void Reparenting_ReconnectsEndpointEnvironment_AndDetachStopsOutputUpdates()
    {
        using var fixture = new Fixture();
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { FontWeight = FontWeight.Bold };
        var first = fixture.Host;
        var second = new StackPanel();
        second.Resources[Sys.TypeScale.BodyLarge.Style] = new TextStyle { FontSize = 27, LineHeight = 35 };
        first.Children.Remove(fixture.Box);
        first.Children.Add(second); second.Children.Add(fixture.Box);
        Assert.Equal(27, fixture.Endpoint("FloatingLabelStyle").FontSize);
        var label = fixture.Endpoint("Label");
        second.Children.Remove(fixture.Box);
        var detachedOutput = MaterialText.GetStyle(label);
        second.Resources[Sys.TypeScale.BodyLarge.Style] = new TextStyle { FontSize = 40 };
        Assert.Same(detachedOutput, MaterialText.GetStyle(label));
        first.Children.Add(fixture.Box);
        Assert.Equal(16, fixture.Endpoint("FloatingLabelStyle").FontSize);
        Assert.Equal(16, fixture.Endpoint("ExpandedLabelStyle").FontSize);
    }

    [AvaloniaTheory]
    [InlineData(24, 16, 0, 24)] [InlineData(24, 16, .49f, 20.08)]
    [InlineData(24, 16, .5f, 20)] [InlineData(24, 16, 1, 16)]
    [InlineData(24, double.NaN, 0, 24)] [InlineData(24, double.NaN, .49f, 24)]
    [InlineData(24, double.NaN, .5f, double.NaN)] [InlineData(24, double.NaN, 1, double.NaN)]
    [InlineData(double.NaN, 16, 0, double.NaN)] [InlineData(double.NaN, 16, .49f, double.NaN)]
    [InlineData(double.NaN, 16, .5f, 16)] [InlineData(double.NaN, 16, 1, 16)]
    [InlineData(double.NaN, double.NaN, 0, double.NaN)] [InlineData(double.NaN, double.NaN, .49f, double.NaN)]
    [InlineData(double.NaN, double.NaN, .5f, double.NaN)] [InlineData(double.NaN, double.NaN, 1, double.NaN)]
    public void AutomaticLineHeight_PreservesDiscreteEndpoints_WhileFiniteHeightsInterpolate(
        double from, double to, float progress, double expected)
    {
        var expanded = new ContentPresenter(); var floating = new ContentPresenter();
        expanded.SetValue(TextBlock.LineHeightProperty, from);
        floating.SetValue(TextBlock.LineHeightProperty, to);
        var actual = TextFieldLayout.InterpolateLabelStyle(expanded, floating, progress)!.LineHeight!.Value;
        if (double.IsNaN(expected)) Assert.True(double.IsNaN(actual));
        else Assert.Equal(expected, actual, 5);
    }

    [AvaloniaFact]
    public void MinimizedGeometry_UsesEffectiveLineHeight_AndOnlyAutomaticHeightUsesTokenFallback()
    {
        using var fixture = new Fixture();
        fixture.Box.LineHeight = 30;
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { FontWeight = FontWeight.Bold };
        Assert.Equal(30, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { LineHeight = double.NaN };
        var floating = fixture.Endpoint("FloatingLabelStyle");
        Assert.True(double.IsNaN(floating.GetValue(TextBlock.LineHeightProperty)));
        Assert.Equal(16, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.LineHeight] = 22d;
        Assert.Equal(22, fixture.Layout.MinimizedLabelLineHeight);
        Assert.True(double.IsNaN(floating.GetValue(TextBlock.LineHeightProperty)));
        fixture.Box.Resources[Sys.TypeScale.BodySmall.LineHeight] = double.PositiveInfinity;
        Assert.Equal(0, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.LineHeight] = double.NaN;
        Assert.Equal(0, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.LineHeight] = -1d;
        Assert.Equal(0, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.LineHeight] = new Foundation.Tokens.TokenRef(
            new Foundation.Tokens.MaterialTokenKey<double>("test.text-field.missing-line-height"));
        Assert.Equal(0, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources.Remove(Sys.TypeScale.BodySmall.LineHeight);
        Assert.Equal(16, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.FontScale] = 1.5d;
        Assert.Equal(24, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { LineHeight = 19 };
        Assert.Equal(19, fixture.Layout.MinimizedLabelLineHeight);
        fixture.Box.Resources.Remove(Sys.TypeScale.BodySmall.Style);
        Assert.Equal(24, fixture.Layout.MinimizedLabelLineHeight);
    }

    [AvaloniaFact]
    public void InfiniteLineHeight_IsInvalidConfiguration_AndRecoversWithoutChangingAutomaticSemantics()
    {
        using var fixture = new Fixture();
        var expanded = fixture.Endpoint("ExpandedLabelStyle");
        var floating = fixture.Endpoint("FloatingLabelStyle");
        var label = fixture.Endpoint("Label");
        var validOutput = MaterialText.GetStyle(label);
        floating.SetValue(TextBlock.LineHeightProperty, double.PositiveInfinity);
        Assert.Null(TextFieldLayout.InterpolateLabelStyle(expanded, floating, 0));
        Assert.Null(TextFieldLayout.InterpolateLabelStyle(expanded, floating, .5f));
        Assert.Equal(0, fixture.Layout.MinimizedLabelLineHeight);
        Assert.Equal(validOutput! with { Foreground = null },
            MaterialText.GetStyle(label)! with { Foreground = null });
        floating.ClearValue(TextBlock.LineHeightProperty);
        Assert.Equal(16, fixture.Layout.MinimizedLabelLineHeight);
        Assert.Equal(24, MaterialText.GetStyle(label)!.LineHeight);
    }

    [AvaloniaFact]
    public void InvalidMetrics_HoldTypographyButKeepChildTextColorLiveAcrossFocusErrorAndRecovery()
    {
        using var fixture = new Fixture();
        var text = new TextBlock { Text = "Label" };
        MaterialTextField.SetLabel(fixture.Box, text);
        var label = fixture.Endpoint("Label");
        label.UpdateChild(); text.ApplyStyling();
        var floating = fixture.Endpoint("FloatingLabelStyle");
        var originalColor = Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color;
        var originalStyle = MaterialText.GetStyle(label)! with { Foreground = null };
        floating.SetValue(TextBlock.LineHeightProperty, double.PositiveInfinity);
        // Exercise the real theme's focus selector without focusing/measuring the native editor.
        ((IPseudoClasses)fixture.Box.Classes).Add(":focus");
        fixture.Advance(100);
        var focusedColor = Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color;
        Assert.NotEqual(originalColor, focusedColor);
        Assert.Equal(fixture.Layout.PaintedLabelColor, focusedColor);
        Assert.Equal(originalStyle, MaterialText.GetStyle(label)! with { Foreground = null });
        MaterialTextField.SetIsError(fixture.Box, true);
        fixture.Advance(100);
        var errorColor = Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color;
        Assert.NotEqual(focusedColor, errorColor);
        Assert.Equal(fixture.Layout.PaintedLabelColor, errorColor);
        Assert.Equal(originalStyle, MaterialText.GetStyle(label)! with { Foreground = null });
        floating.ClearValue(TextBlock.LineHeightProperty);
        Assert.Equal(errorColor, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
        MaterialTextField.SetIsError(fixture.Box, false);
        ((IPseudoClasses)fixture.Box.Classes).Remove(":focus");
        fixture.Advance(100);
        Assert.Equal(originalColor, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
        Assert.Equal(24, text.LineHeight);
        Assert.Null(fixture.Window.MotionRoot.LastError);
    }

    [AvaloniaTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void MissingMotion_WaitsForAllConfiguration_AndResumesCurrentLabelTarget(int missing)
    {
        using var fixture = new Fixture();
        var key = missing switch
        {
            0 => Comp.TextField.SpatialAnimation,
            1 => Comp.TextField.FastEffectsAnimation,
            _ => Comp.TextField.SlowEffectsAnimation,
        };
        fixture.Box.Resources[key] = new Foundation.Tokens.TokenRef(
            new Foundation.Tokens.MaterialTokenKey<Foundation.Motion.SpringSpec>("test.text-field.missing-motion"));
        MaterialTextField.SetAlwaysMinimizeLabel(fixture.Box, true);
        fixture.Advance(4);
        Assert.Equal(0, fixture.Layout.LabelProgress);
        Assert.Null(fixture.Window.MotionRoot.LastError);
        fixture.Box.Resources.Remove(key);
        fixture.Advance(100);
        Assert.Equal(1, fixture.Layout.LabelProgress);
        Assert.Equal(12, fixture.Endpoint("Label").FontSize);
        Assert.Null(fixture.Window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void InFlightTypography_UsesCurrentEndpointsWithoutRetargetingCapturedMotion()
    {
        using var fixture = new Fixture();
        var comparison = new TextBox { Theme = fixture.Box.Theme };
        MaterialTextField.SetLabel(comparison, new Border { Width = 24, Height = 12 });
        fixture.Host.Children.Add(comparison); comparison.ApplyStyling(); comparison.ApplyTemplate();
        var comparisonLayout = comparison.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        MaterialTextField.SetAlwaysMinimizeLabel(fixture.Box, true);
        MaterialTextField.SetAlwaysMinimizeLabel(comparison, true);
        fixture.Advance(4);
        Assert.InRange(fixture.Layout.LabelProgress, .001f, .999f);
        var label = fixture.Endpoint("Label");
        fixture.Box.Resources[Comp.TextField.SpatialAnimation] = new Foundation.Motion.SpringSpec(1, 20);
        fixture.Box.Resources[Sys.TypeScale.BodySmall.Style] = new TextStyle { FontSize = 10 };
        for (var i = 0; i < 8; i++)
        {
            fixture.Advance(1);
            Assert.Equal(comparisonLayout.LabelProgress, fixture.Layout.LabelProgress);
            var p = Math.Clamp(fixture.Layout.LabelProgress, 0, 1);
            Assert.Equal(16 - 6 * p, label.FontSize, 5);
            label.FontSize = 28;
            Assert.Equal(28, label.FontSize);
            label.ClearValue(ContentPresenter.FontSizeProperty);
            Assert.Equal(16 - 6 * p, label.FontSize, 5);
        }
        fixture.Advance(100);
        Assert.Equal(1, fixture.Layout.LabelProgress);
        Assert.Equal(10, label.FontSize);
        Assert.Null(fixture.Window.MotionRoot.LastError);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly MotionTestClockScope _clock = new();
        internal MotionTestWindow Window { get; } = new() { RequestedThemeVariant = ThemeVariant.Light };
        internal StackPanel Host { get; } = new();
        internal TextBox Box { get; }
        internal TextFieldLayout Layout => Box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        internal ContentPresenter Endpoint(string name) => (ContentPresenter)Layout.Children.Single(x => x.Name == name);

        internal Fixture(bool outlined = false, bool secure = false)
        {
            Window.Styles.Add(new MaterialTheme());
            var key = "Material" + (outlined ? "Outlined" : "Filled") + (secure ? "Secure" : "") + "TextFieldTheme";
            Box = new TextBox { Theme = (ControlTheme)Window.FindResource(key)! };
            MaterialTextField.SetLabel(Box, new Border { Width = 24, Height = 12 });
            Host.Children.Add(Box); Window.Content = Host;
            Window.ApplyTemplate(); Window.Presenter?.UpdateChild();
            Box.ApplyStyling(); Box.ApplyTemplate();
            foreach (var control in Box.GetVisualDescendants().OfType<Control>()) control.ApplyStyling();
        }
        internal void Advance(int count)
        {
            for (var i = 0; i < count; i++) _clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        }
        public void Dispose() { Window.Close(); _clock.Dispose(); }
    }
}
