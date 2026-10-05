using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ContentColorForBindingPriorityTests
{
    [AvaloniaFact]
    public void CompiledTemplate_UsesTemplatePriority_AndRestoresLiveColorAfterUserOverrides()
    {
        var host = LoadFixture();
        var window = Show(host);
        var owner = host.FindControl<ContentControl>("Templated")!;
        var part = owner.GetVisualDescendants().OfType<Border>().Single();
        window.Styles.Add(new Style(s => s.OfType<ContentControl>().Template().OfType<Border>())
        {
            Setters = { new Setter(Border.BorderBrushProperty, Brushes.Blue) },
        });
        window.Styles.Add(new Style(s => s.OfType<ContentControl>().Template().OfType<Border>().Class("user"))
        {
            Setters =
            {
                new Setter(Border.BorderBrushProperty, Brushes.Orange),
                new Setter(Control.TagProperty, "user"),
            },
        });
        try
        {
            Flush(window);
            AssertColor(part, Colors.Green, BindingPriority.Template);
            part.Classes.Add("user");
            Flush(window);
            Assert.Equal("user", part.Tag);
            AssertColor(part, Colors.Orange, BindingPriority.StyleTrigger);

            part.BorderBrush = Brushes.Cyan;
            host.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Purple;
            Flush(window);
            AssertColor(part, Colors.Cyan, BindingPriority.LocalValue);
            part.ClearValue(Border.BorderBrushProperty);
            Flush(window);
            AssertColor(part, Colors.Orange, BindingPriority.StyleTrigger);
            part.Classes.Remove("user");
            Flush(window);
            AssertColor(part, Colors.Purple, BindingPriority.Template);

            var input = new Border { BorderBrush = Brushes.Red };
            using (part.Bind(Border.BorderBrushProperty,
                       new Binding(nameof(Border.BorderBrush)) { Source = input }))
            {
                host.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Yellow;
                Flush(window);
                AssertColor(part, Colors.Red, BindingPriority.LocalValue);
                input.BorderBrush = Brushes.Cyan;
                Flush(window);
                AssertColor(part, Colors.Cyan, BindingPriority.LocalValue);
            }
            Flush(window);
            AssertColor(part, Colors.Yellow, BindingPriority.Template);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CompiledSetter_KeepsStylePriority_AndRestoresLiveColorAfterUserOverrides()
    {
        var host = LoadFixture();
        var window = Show(host);
        var border = host.FindControl<Border>("Styled")!;
        var userStyle = new Style { Setters = { new Setter(Border.BorderBrushProperty, Brushes.Blue) } };
        try
        {
            AssertColor(border, Colors.Green, BindingPriority.Style);
            border.Styles.Add(userStyle);
            border.ApplyStyling();
            Flush(window);
            AssertColor(border, Colors.Blue, BindingPriority.Style);
            host.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Yellow;
            Flush(window);
            AssertColor(border, Colors.Blue, BindingPriority.Style);

            border.BorderBrush = Brushes.Cyan;
            Flush(window);
            AssertColor(border, Colors.Cyan, BindingPriority.LocalValue);
            border.ClearValue(Border.BorderBrushProperty);
            Flush(window);
            AssertColor(border, Colors.Blue, BindingPriority.Style);
            border.Styles.Remove(userStyle);
            Flush(window);
            AssertColor(border, Colors.Yellow, BindingPriority.Style);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CompiledDirectBinding_KeepsLocalPriority_UntilExplicitlyRemoved()
    {
        var host = LoadFixture();
        var window = Show(host);
        var border = host.FindControl<Border>("Direct")!;
        window.Styles.Add(new Style(s => s.OfType<Border>().Class("user"))
        {
            Setters =
            {
                new Setter(Border.BorderBrushProperty, Brushes.Orange),
                new Setter(Control.TagProperty, "user"),
            },
        });
        try
        {
            AssertColor(border, Colors.Green, BindingPriority.LocalValue);
            border.Classes.Add("user");
            Flush(window);
            Assert.Equal("user", border.Tag);
            AssertColor(border, Colors.Green, BindingPriority.LocalValue);
            host.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Yellow;
            Flush(window);
            AssertColor(border, Colors.Yellow, BindingPriority.LocalValue);

            var binding = BindingOperations.GetBindingExpressionBase(border, Border.BorderBrushProperty);
            Assert.NotNull(binding);
            binding.Dispose();
            Flush(window);
            AssertColor(border, Colors.Orange, BindingPriority.StyleTrigger);
            border.BorderBrush = Brushes.Cyan;
            Flush(window);
            AssertColor(border, Colors.Cyan, BindingPriority.LocalValue);
            border.ClearValue(Border.BorderBrushProperty);
            Flush(window);
            AssertColor(border, Colors.Orange, BindingPriority.StyleTrigger);
            border.Classes.Remove("user");
            Flush(window);
            AssertColor(border, Colors.Yellow, BindingPriority.Style);
        }
        finally { window.Close(); }
    }

    private static void AssertColor(Border border, Color expected, BindingPriority priority)
    {
        Assert.Equal(expected, Assert.IsAssignableFrom<ISolidColorBrush>(border.BorderBrush).Color);
        Assert.Equal(priority, border.GetDiagnostic(Border.BorderBrushProperty).Priority);
    }

    private static StackPanel LoadFixture()
    {
        var host = (StackPanel)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3Expressive.Tests/Fixtures/ContentColorForBindingPriority.axaml"));
        host.Resources[MaterialTokens.Sys.Color.Primary] = Colors.Red;
        host.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Green;
        return host;
    }

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 300, Height = 200, Content = content };
        window.Show();
        Flush(window);
        return window;
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
