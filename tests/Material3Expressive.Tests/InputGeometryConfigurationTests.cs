using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class InputGeometryConfigurationTests
{
    [AvaloniaFact]
    public void SliderVerticalDefault_DoesNotOverrideOrdinaryStyle_AndRestoresAfterClear()
    {
        var slider = new Slider { Orientation = Orientation.Vertical, Height = 100 };
        var style = new Style { Setters = { new Setter(SliderAppearance.EnableCornerShrinkingProperty, false) } };
        slider.Styles.Add(style);
        using var clock = new MotionTestClockScope(); var window = Show(slider);
        try
        {
            Assert.False(SliderAppearance.GetEnableCornerShrinking(slider));
            slider.Orientation = Orientation.Horizontal; Flush(window); Assert.False(SliderAppearance.GetEnableCornerShrinking(slider));
            slider.Orientation = Orientation.Vertical; Flush(window); Assert.False(SliderAppearance.GetEnableCornerShrinking(slider));
            SliderAppearance.SetEnableCornerShrinking(slider, true); Flush(window); Assert.True(SliderAppearance.GetEnableCornerShrinking(slider));
            slider.ClearValue(SliderAppearance.EnableCornerShrinkingProperty); Flush(window); Assert.False(SliderAppearance.GetEnableCornerShrinking(slider));
            slider.Styles.Remove(style); Flush(window); Assert.True(SliderAppearance.GetEnableCornerShrinking(slider));
            slider.Resources[Comp.Slider.VerticalEnableCornerShrinking] = false; Flush(window); Assert.False(SliderAppearance.GetEnableCornerShrinking(slider));
            slider.Resources[Comp.Slider.TrackInsideCornerSize] = 5d; Flush(window);
            Assert.Equal(5, SliderAppearance.GetTrackInsideCornerSize(slider));
            Assert.Equal(5, slider.GetVisualDescendants().OfType<SliderTrack>().Single().TrackInsideCornerSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BadgeContentDefaults_PreserveOrdinaryStylesAndLocalValues()
    {
        var badge = new Badge();
        var textStyle = new TextStyle { FontSize = 19 };
        var style = new Style { Setters = { new Setter(Badge.PaddingProperty, new Thickness(7)),
            new Setter(Badge.CornerRadiusProperty, new CornerRadius(3)), new Setter(MaterialText.StyleProperty, textStyle) } };
        badge.Styles.Add(style);
        using var clock = new MotionTestClockScope(); var window = Show(badge);
        try
        {
            badge.Content = new Border { Width = 10, Height = 8 }; Flush(window);
            Assert.Equal(new Thickness(7), badge.Padding); Assert.Equal(new CornerRadius(3), badge.CornerRadius);
            Assert.Same(textStyle, MaterialText.GetStyle(badge));
            badge.Content = null; badge.ContentTemplate = new FuncDataTemplate<object>((_, _) => new Border()); Flush(window);
            Assert.Equal(new Thickness(7), badge.Padding); Assert.Equal(new CornerRadius(3), badge.CornerRadius);
            badge.Padding = new Thickness(9); Flush(window); badge.ClearValue(Badge.PaddingProperty); Flush(window);
            Assert.Equal(new Thickness(7), badge.Padding);
            badge.Styles.Remove(style); Flush(window); Assert.Equal(new Thickness(4, 0), badge.Padding);
            badge.Resources[Comp.Badge.LargeContentPadding] = new Thickness(6, 1); Flush(window);
            Assert.Equal(new Thickness(6, 1), badge.Padding);
            badge.ContentTemplate = null; Flush(window); Assert.Equal(default, badge.Padding); Assert.Null(MaterialText.GetStyle(badge));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PlainTooltipSoftGeometry_TracksTokens_WithoutChangingExplicitConstraints()
    {
        var tooltip = new PlainTooltip { Content = new Border { Width = 8, Height = 8 } };
        using var clock = new MotionTestClockScope(); var window = Show(tooltip);
        try
        {
            Assert.Equal(new Size(40, 24), tooltip.Bounds.Size);
            tooltip.Resources[Comp.PlainTooltip.MinWidth] = 60d; tooltip.Resources[Comp.PlainTooltip.MinHeight] = 32d;
            tooltip.Resources[Comp.PlainTooltip.MaxWidth] = 120d; tooltip.Resources[Comp.PlainTooltip.ContentPadding] = new Thickness(10, 5); Flush(window);
            Assert.Equal(new Size(60, 32), tooltip.Bounds.Size); Assert.Equal(120, tooltip.TooltipMaxWidth);
            Assert.Equal(new Thickness(10, 5), tooltip.Padding);
            tooltip.Width = 20; tooltip.Height = 12; Flush(window); Assert.Equal(new Size(20, 12), tooltip.Bounds.Size);
            tooltip.ClearValue(Control.WidthProperty); tooltip.ClearValue(Control.HeightProperty); Flush(window);
            Assert.Equal(new Size(60, 32), tooltip.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BasicAlertDialogSoftWidth_IsAReactiveTemplateInput()
    {
        using var clock = new MotionTestClockScope();
        var dialog = new BasicAlertDialog { Content = new Border { Width = 20, Height = 20 }, IsOpen = true,
            AnimateTransition = false, UsePlatformDefaultWidth = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        try
        {
            var layout = window.GetVisualDescendants().OfType<BasicAlertDialogLayout>().Single();
            Assert.Equal(280, layout.Bounds.Width);
            dialog.Resources[Comp.Dialog.MinWidth] = 180d; Flush(window); Assert.Equal(180, layout.DefaultMinWidth); Assert.Equal(180, layout.Bounds.Width);
            dialog.Width = 100; Flush(window); Assert.Equal(100, layout.Bounds.Width);
            dialog.ClearValue(Control.WidthProperty); Flush(window); Assert.Equal(180, layout.Bounds.Width);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 500, Height = 400 };
        window.Styles.Add(new MaterialTheme()); window.Content = new StackPanel { Children = { content } };
        window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
