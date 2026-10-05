using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class TextFieldConfigurationTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void StateDefaults_PreserveOrdinaryStylesLocalValuesAndBindings(bool outlined, bool secure)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined, secure);
        var style = new Style
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Red),
                new Setter(TextBox.ForegroundProperty, Brushes.Green),
                new Setter(TextBox.BorderBrushProperty, Brushes.Blue),
                new Setter(TextBox.PlaceholderForegroundProperty, Brushes.Purple),
                new Setter(TextBox.CaretBrushProperty, Brushes.Orange),
            },
        };
        void CheckStyle()
        {
            Flush(window);
            Assert.Equal(Colors.Red, ColorOf(box.Background));
            Assert.Equal(Colors.Green, ColorOf(box.Foreground));
            Assert.Equal(Colors.Blue, ColorOf(box.BorderBrush));
            Assert.Equal(Colors.Purple, ColorOf(box.PlaceholderForeground));
            Assert.Equal(Colors.Orange, ColorOf(box.CaretBrush));
        }
        try
        {
            box.Styles.Add(style); box.ApplyStyling(); CheckStyle();
            box.Focus(); CheckStyle();
            MaterialTextField.SetIsError(box, true); CheckStyle();
            box.IsEnabled = false; CheckStyle();
            box.Background = Brushes.Yellow; Flush(window);
            Assert.Equal(Colors.Yellow, ColorOf(box.Background));
            box.ClearValue(TextBox.BackgroundProperty); CheckStyle();
            var source = new Border { Tag = Brushes.Cyan };
            using (box.Bind(TextBox.BorderBrushProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                Flush(window); Assert.Equal(Colors.Cyan, ColorOf(box.BorderBrush));
                source.Tag = Brushes.Black;
                Flush(window); Assert.Equal(Colors.Black, ColorOf(box.BorderBrush));
            }
            CheckStyle();
            box.Styles.Remove(style); Flush(window);
            Assert.Equal(ColorOf(TextFieldThemeDefaults.GetBorderBrush(box)), ColorOf(box.BorderBrush));
            Assert.InRange(ColorOf(box.BorderBrush).A, outlined ? (byte)30 : (byte)96, outlined ? (byte)31 : (byte)98);
            box.IsEnabled = true; Flush(window);
            Assert.Equal(ColorOf(TextFieldThemeDefaults.GetCaretBrush(box)), ColorOf(box.CaretBrush));
            Assert.Equal(MaterialSecureTextField.GetIsSecure(box), secure);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void Padding_ThemeSelectsCurrentLabelCondition_AndClearRestoresCurrentDefault(bool outlined, bool secure)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined, secure);
        try
        {
            Assert.Equal(new Thickness(16), box.Padding);
            MaterialTextField.SetLabel(box, "Label"); Flush(window);
            Assert.Equal(outlined ? new Thickness(16) : new Thickness(16, 8), box.Padding);
            box.Resources[Comp.TextField.ContentPadding] = new Thickness(20, 12);
            box.Resources[Comp.TextField.WithLabelContentPadding] = new Thickness(24, 10);
            Flush(window);
            Assert.Equal(outlined ? new Thickness(20, 12) : new Thickness(24, 10), box.Padding);
            MaterialTextField.SetLabelPosition(box, TextFieldLabelPosition.Inside); Flush(window);
            Assert.Equal(new Thickness(24, 10), box.Padding);
            var style = new Style { Setters = { new Setter(TextBox.PaddingProperty, new Thickness(3)) } };
            box.Styles.Add(style); box.ApplyStyling(); Flush(window);
            Assert.Equal(new Thickness(3), box.Padding);
            MaterialTextField.SetLabelPosition(box, TextFieldLabelPosition.Above); Flush(window);
            Assert.Equal(new Thickness(3), box.Padding);
            box.Padding = new Thickness(7); MaterialTextField.SetLabel(box, null); Flush(window);
            Assert.Equal(new Thickness(7), box.Padding);
            box.ClearValue(TextBox.PaddingProperty); Flush(window);
            Assert.Equal(new Thickness(3), box.Padding);
            box.Styles.Remove(style); Flush(window);
            Assert.Equal(new Thickness(20, 12), box.Padding);
            var source = new Border { Tag = new Thickness(9) };
            using (box.Bind(TextBox.PaddingProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                MaterialTextField.SetLabelPosition(box, TextFieldLabelPosition.Inside);
                MaterialTextField.SetLabel(box, "Label"); Flush(window);
                Assert.Equal(new Thickness(9), box.Padding);
                source.Tag = new Thickness(11); Flush(window);
                Assert.Equal(new Thickness(11), box.Padding);
            }
            Flush(window); Assert.Equal(new Thickness(24, 10), box.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void SoftMinimums_FollowTokensWithoutReplacingNativeConstraints(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        try
        {
            Assert.Equal(0, box.MinWidth); Assert.Equal(0, box.MinHeight);
            Assert.Equal(280, box.Bounds.Width); Assert.Equal(56, box.Bounds.Height);
            box.Resources[Comp.TextField.MinimumWidth] = 320d;
            box.Resources[Comp.TextField.MinimumHeight] = 80d; Flush(window);
            Assert.Equal(320, box.Bounds.Width); Assert.Equal(80, box.Bounds.Height);
            box.MinWidth = 200; box.MinHeight = 64; Flush(window);
            Assert.Equal(200, box.Bounds.Width); Assert.Equal(64, box.Bounds.Height);
            box.ClearValue(TextBox.MinWidthProperty); box.ClearValue(TextBox.MinHeightProperty); Flush(window);
            Assert.Equal(320, box.Bounds.Width); Assert.Equal(80, box.Bounds.Height);
            window.Width = 120; Flush(window);
            Assert.True(box.Bounds.Width <= 120, "The token minimum remains constrained by available space.");
            Assert.Equal(0, box.MinWidth);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MotionResourceReferences_StayLiveAcrossThemeAndMountChanges()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        try
        {
            var layout = Part(box);
            var first = (StackPanel)window.Content!;
            first.Resources[Sys.Motion.Spring.SpatialFast] = new SpringSpec(0.7, 150);
            first.Resources[Sys.Motion.Spring.EffectsFast] = new SpringSpec(1, 200);
            first.Resources[Sys.Motion.Spring.EffectsSlow] = new SpringSpec(1, 90);
            Flush(window);
            Assert.Equal(new SpringSpec(0.7, 150), layout.SpatialAnimation);
            Assert.Equal(new SpringSpec(1, 200), layout.FastEffectsAnimation);
            Assert.Equal(new SpringSpec(1, 90), layout.SlowEffectsAnimation);
            var second = new StackPanel();
            second.Resources[Comp.TextField.SpatialAnimation] = new SpringSpec(0.9, 350);
            second.Resources[Comp.TextField.MinimumWidth] = 310d;
            second.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
                { [Comp.TextField.FastEffectsAnimation] = new SpringSpec(1, 300) };
            second.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
                { [Comp.TextField.FastEffectsAnimation] = new SpringSpec(1, 600) };
            first.Children.Remove(box); first.Children.Add(second); second.Children.Add(box); Flush(window);
            layout = Part(box);
            Assert.Equal(new SpringSpec(0.9, 350), layout.SpatialAnimation);
            Assert.Equal(new SpringSpec(1, 300), layout.FastEffectsAnimation);
            Assert.Equal(310, layout.DefaultMinimumWidth);
            window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
            Assert.Equal(new SpringSpec(1, 600), layout.FastEffectsAnimation);
            Assert.Equal(310, box.Bounds.Width);
            MaterialTextField.SetLabel(box, "Label"); box.Focus(); Flush(window);
            for (var i = 0; i < 100; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.Equal(1, layout.LabelProgress);
            Assert.Equal(new SpringSpec(0.9, 350), layout.SpatialAnimation);
            Assert.Null(MotionTestClockScope.Error(box));
        }
        finally { window.Close(); }
    }

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    private static TextFieldLayout Part(TextBox box) => box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static Window Show(out TextBox box, bool outlined = false, bool secure = false)
    {
        var window = new MotionTestWindow { Width = 420, Height = 320, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new MaterialTheme());
        var key = "Material" + (outlined ? "Outlined" : "Filled") + (secure ? "Secure" : "") + "TextFieldTheme";
        box = new TextBox { Theme = (ControlTheme)window.FindResource(key)!, HorizontalAlignment = HorizontalAlignment.Left };
        window.Content = new StackPanel { Children = { new Button { Content = "Focus target" }, box } };
        window.Show(); Flush(window); return window;
    }
}
