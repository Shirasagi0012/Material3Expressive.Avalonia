using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Tokens;
using Xunit;
using TypographyApi = Material3Expressive.Foundation.Typography.Typography;

namespace Material3Expressive.Tests;

public class TypographyTests
{
    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 600, Height = 500 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content;
        window.Show();
        return window;
    }

    private static void Flush() => Dispatcher.UIThread.RunJobs();

    [Fact]
    public void Merge_PreservesUnspecified_AndCopiesExplicitDefaults()
    {
        var original = new TextStyle { FontSize = 22, FontWeight = FontWeight.Bold, LetterSpacing = 2, LineHeight = 28 };
        var merged = original.Merge(new TextStyle { FontWeight = FontWeight.Normal, LetterSpacing = 0, LineHeight = double.NaN });
        Assert.Equal(22, merged.FontSize);
        Assert.Equal(FontWeight.Normal, merged.FontWeight);
        Assert.Equal(0, merged.LetterSpacing);
        Assert.True(double.IsNaN(merged.LineHeight!.Value));
        Assert.Equal(FontWeight.Bold, original.FontWeight);
        Assert.Same(original, original.Merge(null));
    }

    [AvaloniaFact]
    public void CompiledXaml_RolesResourcesDerivationAndNestedDefaults()
    {
        var panel = (StackPanel)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/Typography.axaml"));
        var window = Show(panel);
        try
        {
            var bound = panel.FindControl<TextBlock>("Bound")!;
            Assert.Equal(26, bound.FontSize);
            Assert.Equal(FontStyle.Italic, bound.FontStyle);
            Assert.Equal(FontStretch.Condensed, bound.FontStretch);
            Assert.Equal(TextAlignment.Center, bound.TextAlignment);
            Assert.Equal(Colors.Green, Assert.IsAssignableFrom<ISolidColorBrush>(bound.Foreground).Color);
            panel.FindControl<TextBlock>("Sizing")!.FontSize = 29;
            Assert.Equal(29, bound.FontSize);
            Assert.Equal(14, panel.FindControl<TextBlock>("Styled")!.FontSize);
            Assert.Equal(FontWeight.Bold, panel.FindControl<TextBlock>("Styled")!.FontWeight);
            Assert.Equal(22, panel.FindControl<TextBlock>("Role")!.FontSize);
            Assert.Equal(FontWeight.Bold, panel.FindControl<TextBlock>("Derived")!.FontWeight);
            Assert.Equal(22, panel.FindControl<TextBlock>("Derived")!.FontSize);
            Assert.Equal(25, panel.FindControl<TextBlock>("Resource")!.FontSize);
            Assert.Equal(31, panel.FindControl<TextBlock>("Resource")!.LineHeight);
            var colored = panel.FindControl<TextBlock>("Colored")!;
            Assert.Equal(25, colored.FontSize);
            panel.Resources["Custom"] = new TextStyle { FontSize = 28 };
            panel.Resources[MaterialTokens.Sys.Color.Primary] = Colors.Magenta;
            Flush();
            Assert.Equal(28, colored.FontSize);
            Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(colored.Foreground).Color);
            Assert.Equal(16, panel.FindControl<TextBlock>("Inherited")!.FontSize);
            Assert.Equal(FontWeight.Bold, panel.FindControl<TextBlock>("Inherited")!.FontWeight);

            panel.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.Size] = 27d;
            Flush();
            Assert.Equal(27, panel.FindControl<TextBlock>("Role")!.FontSize);
            Assert.Equal(27, panel.FindControl<TextBlock>("Derived")!.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PartialStylesMerge_OverAncestors_AndNullRestoresEnvironment()
    {
        var text = new TextBlock { Text = "Example" };
        var inner = new StackPanel { Children = { text } };
        var outer = new StackPanel { Children = { inner } };
        MaterialText.SetStyle(outer, new TextStyle { FontSize = 30, FontWeight = FontWeight.Bold });
        MaterialText.SetStyle(inner, new TextStyle { LetterSpacing = 2 });
        var window = Show(outer);
        try
        {
            Assert.Equal(30, text.FontSize);
            Assert.Equal(FontWeight.Bold, text.FontWeight);
            Assert.Equal(2, text.LetterSpacing);
            MaterialText.SetStyle(text, new TextStyle { FontSize = 11 });
            Assert.Equal(11, text.FontSize);
            Assert.Equal(FontWeight.Bold, text.FontWeight); // unspecified fields keep inheriting
            Assert.Equal(2, text.LetterSpacing);
            MaterialText.SetStyle(text, null);
            Assert.Equal(30, text.FontSize);
            MaterialText.SetStyle(outer, new TextStyle { FontSize = 32, FontWeight = FontWeight.Medium });
            Assert.Equal(32, text.FontSize);
            Assert.Equal(FontWeight.Medium, text.FontWeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeLocalBindingAndClassSetter_OverrideAndRestore()
    {
        var text = new TextBlock { Text = "Example" };
        MaterialText.SetStyle(text, new TextStyle { FontSize = 22, FontWeight = FontWeight.Normal });
        var window = Show(text);
        try
        {
            var style = new Style(s => s.OfType<TextBlock>().Class("warning"))
            {
                Setters = { new Setter(TextBlock.FontSizeProperty, 28d) },
            };
            window.Styles.Add(style);
            text.Classes.Add("warning");
            window.UpdateLayout();
            Assert.Equal(28, text.FontSize);
            var value = new TextBlock { FontSize = 35 };
            using var binding = text.Bind(TextBlock.FontSizeProperty, new Binding(nameof(TextBlock.FontSize)) { Source = value });
            MaterialText.SetStyle(text, new TextStyle { FontSize = 24 });
            Assert.Equal(35, text.FontSize);
            value.FontSize = 36;
            Assert.Equal(36, text.FontSize);
            binding.Dispose();
            Assert.Equal(28, text.FontSize);
            text.Classes.Remove("warning");
            Assert.Equal(24, text.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Reparenting_ChangesEnvironment_AndDetachedScopeDoesNotObserveOldParent()
    {
        var text = new TextBlock { Text = "Moves" };
        var scope = new StackPanel { Children = { text } };
        MaterialText.SetStyle(scope, new TextStyle { FontWeight = FontWeight.Bold });
        var left = new StackPanel { Children = { scope } };
        var right = new StackPanel();
        MaterialText.SetStyle(left, new TextStyle { FontSize = 20 });
        MaterialText.SetStyle(right, new TextStyle { FontSize = 28 });
        var window = Show(new StackPanel { Children = { left, right } });
        try
        {
            Assert.Equal(20, text.FontSize);
            left.Children.Remove(scope);
            right.Children.Add(scope);
            window.UpdateLayout();
            Assert.Equal(28, text.FontSize);
            MaterialText.SetStyle(left, new TextStyle { FontSize = 40 });
            Assert.Equal(28, text.FontSize);
            MaterialText.SetStyle(right, new TextStyle { FontSize = 29 });
            Assert.Equal(29, text.FontSize);
            Assert.Equal(FontWeight.Bold, text.FontWeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RoleBinding_TracksScalarOverrideWholeStyleAndFontScale()
    {
        var text = new TextBlock();
        text.Bind(MaterialText.StyleProperty, TypographyApi.CreateBinding(TypographyRole.TitleLarge));
        var window = Show(text);
        try
        {
            Assert.Equal(22, text.FontSize);
            window.Resources[MaterialTokens.Sys.TypeScale.FontScale] = 1.5d;
            Flush();
            Assert.Equal(33, text.FontSize);
            Assert.Equal(42, text.LineHeight);
            text.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.LetterSpacing] =
                TextUnit.Em(0.1, MaterialTokens.Sys.TypeScale.TitleLarge.Size);
            Flush();
            Assert.Equal(3.3, text.LetterSpacing, 10);

            var custom = new TextStyle { FontSize = 19, LineHeight = 25 };
            text.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.Style] = custom;
            Flush();
            Assert.Equal(19, text.FontSize);
            window.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.Size] = 70d;
            Flush();
            Assert.Equal(19, text.FontSize);
            Assert.Same(custom, TypographyApi.GetStyle(text, TypographyRole.TitleLarge));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void UnspecifiedFields_WithdrawTheirSetters_AndRestoreNativeInheritance()
    {
        var text = new TextBlock { Text = "Color" };
        var panel = new StackPanel { Children = { text } };
        panel.SetValue(TextElement.ForegroundProperty, Brushes.Red);
        var window = Show(panel);
        try
        {
            Assert.Same(Brushes.Red, text.Foreground);
            MaterialText.SetStyle(text, new TextStyle { Foreground = Brushes.Blue, FontWeight = FontWeight.Bold });
            Assert.Same(Brushes.Blue, text.Foreground);
            MaterialText.SetStyle(text, TextStyle.Empty);
            Assert.Same(Brushes.Red, text.Foreground);
            Assert.Equal(FontWeight.Normal, text.FontWeight);
            panel.SetValue(TextElement.ForegroundProperty, Brushes.Green);
            Assert.Same(Brushes.Green, text.Foreground);
            MaterialText.SetStyle(panel, new TextStyle { FontSize = 30 });
            Assert.Equal(30, text.FontSize); // Empty specifies nothing, so the ancestor value inherits.
            MaterialText.SetStyle(text, null);
            Assert.Equal(30, text.FontSize);
            MaterialText.SetStyle(panel, null);
            Assert.Equal(16, text.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeStyleOnTheSameControl_WinsAcrossAggregateUpdatesAndRemoval()
    {
        var text = new TextBlock();
        var native = new Style { Setters = { new Setter(TextBlock.FontSizeProperty, 31d) } };
        text.Styles.Add(native);
        MaterialText.SetStyle(text, new TextStyle { FontSize = 22 });
        var window = Show(text);
        try
        {
            Assert.Equal(31, text.FontSize);
            MaterialText.SetStyle(text, new TextStyle { FontSize = 24 });
            Assert.Equal(31, text.FontSize);
            text.Styles.Remove(native);
            Assert.Equal(24, text.FontSize);
            MaterialText.SetStyle(text, TextStyle.Empty);
            Assert.NotEqual(24, text.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DerivedStyle_AcceptsAnIntegerBindingForADipField()
    {
        var text = new TextBlock();
        var extension = new TextStyleExtension { FontSize = new Binding { Source = 23 } };
        text.Bind(MaterialText.StyleProperty, extension.ProvideValue(new StubServiceProvider()));
        var window = Show(text);
        try { Assert.Equal(23, text.FontSize); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Button_ProvidesStyleToExplicitAndTemplatedContent_AndHonorsNativeOverrides()
    {
        var label = new TextBlock { Text = "Explicit" };
        var button = new Button { Content = new StackPanel { Children = { label } } };
        var window = Show(button);
        try
        {
            Assert.Equal(14, label.FontSize);
            Assert.Equal(20, label.LineHeight);
            button.Classes.Add("large");
            window.UpdateLayout();
            Assert.Equal(24, label.FontSize);
            button.FontSize = 31;
            Assert.Equal(31, label.FontSize);
            button.ClearValue(Button.FontSizeProperty);
            Assert.Equal(24, label.FontSize);
            button.Resources[MaterialTokens.Sys.TypeScale.HeadlineSmall.Style] =
                new TextStyle { FontSize = 27, LineHeight = 35, FontWeight = FontWeight.Bold };
            Flush();
            Assert.Equal(27, label.FontSize);
            Assert.Equal(FontWeight.Bold, label.FontWeight);
            button.Content = "Template";
            button.ContentTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Text = "Template" });
            window.UpdateLayout();
            var generated = button.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(27, generated.FontSize);
            Assert.Equal(35, generated.LineHeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RailLabel_KeepsItsRoleForStringsAndExplicitText()
    {
        var item = new NavigationRailItem { Label = "Home" };
        var window = Show(item);
        try
        {
            Assert.Equal(12, item.GetVisualDescendants().OfType<TextBlock>().Single().FontSize);
            var label = new TextBlock { Text = "Home" };
            item.Label = label;
            window.UpdateLayout();
            Assert.Equal(12, label.FontSize);
            item.Resources[MaterialTokens.Sys.TypeScale.LabelMedium.Style] = new TextStyle { FontSize = 15, LineHeight = 21 };
            Flush();
            Assert.Equal(15, label.FontSize);
            Assert.Equal(21, label.LineHeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TokenUnits_RejectCyclesAndInvalidScale_AndAllRolesResolve()
    {
        var text = new TextBlock();
        var window = Show(text);
        try
        {
            foreach (var role in Enum.GetValues<TypographyRole>())
            {
                var style = TypographyApi.GetStyle(text, role);
                Assert.True(style.FontSize > 0);
                Assert.True(style.LineHeight >= style.FontSize);
                Assert.NotNull(style.FontFamily);
            }
            window.Resources[MaterialTokens.Sys.TypeScale.FontScale] = 0d;
            Assert.False(TokenResolver.TryResolve(text, MaterialTokens.Sys.TypeScale.BodyLarge.Style, null, out _));
            window.Resources[MaterialTokens.Sys.TypeScale.FontScale] = 1d;
            text.Resources[MaterialTokens.Sys.TypeScale.BodyLarge.Size] =
                TextUnit.Em(1, MaterialTokens.Sys.TypeScale.BodyLarge.Size);
            Assert.False(TokenResolver.TryResolve(text, MaterialTokens.Sys.TypeScale.BodyLarge.Style, null, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => TextUnit.Sp(double.PositiveInfinity));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RoleBinding_TracksThemeVariantAndResourceRemoval()
    {
        var text = new TextBlock();
        text.Bind(MaterialText.StyleProperty, TypographyApi.CreateBinding(TypographyRole.TitleLarge));
        text.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            [MaterialTokens.Sys.TypeScale.TitleLarge.Style] = new TextStyle { FontSize = 40, Foreground = Brushes.White },
        };
        var window = Show(text);
        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            Flush();
            Assert.Equal(22, text.FontSize);
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Flush();
            Assert.Equal(40, text.FontSize);
            Assert.Same(Brushes.White, text.Foreground);
            text.Resources.ThemeDictionaries.Clear();
            Flush();
            Assert.Equal(22, text.FontSize);
            Assert.NotSame(Brushes.White, text.Foreground);
        }
        finally { window.Close(); }
    }
}
