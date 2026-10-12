using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class SnackbarConfigurationIntegrationTests
{
    [AvaloniaFact]
    public void DataButtons_FollowDisabledDefaults_AndOrdinaryForegroundStyles()
    {
        using var clock = new MotionTestClockScope();
        var snackbar = new Snackbar { Content = new Border { Width = 20, Height = 12 },
            DataActionForeground = Brushes.Red, DismissActionForeground = Brushes.Blue };
        snackbar.Resources[Comp.TextButton.DisabledContentColor] = Colors.Magenta;
        snackbar.Resources[Comp.StandardIconButton.DisabledContentColor] = Colors.Cyan;
        var window = DialogTests.Show(new Grid { Children = { snackbar } });
        try
        {
            var action = snackbar.GetVisualDescendants().OfType<Button>().Single(c => c.Name == "PART_DataAction");
            // Exercise the real data overload without requiring label glyphs.
            action.ContentTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 16, Height = 8 });
            snackbar.Data = new Data(); Flush(window);
            var dismiss = snackbar.GetVisualDescendants().OfType<IconButton>().Single(c => c.Name == "PART_DataDismiss");
            Assert.Equal(Colors.Red, ColorOf(action.Foreground)); Assert.Equal(Colors.Blue, ColorOf(dismiss.Foreground));
            snackbar.IsEnabled = false; Flush(window);
            Assert.Equal(Colors.Magenta, ColorOf(action.Foreground)); Assert.Equal(Colors.Cyan, ColorOf(dismiss.Foreground));
            var actionStyle = new Style { Setters = { new Setter(Button.ForegroundProperty, Brushes.Green) } };
            var dismissStyle = new Style { Setters = { new Setter(Button.ForegroundProperty, Brushes.Orange) } };
            action.Styles.Add(actionStyle); action.ApplyStyling(); dismiss.Styles.Add(dismissStyle); dismiss.ApplyStyling(); Flush(window);
            Assert.Equal(Colors.Green, ColorOf(action.Foreground)); Assert.Equal(Colors.Orange, ColorOf(dismiss.Foreground));
            snackbar.IsEnabled = true; Flush(window);
            Assert.Equal(Colors.Green, ColorOf(action.Foreground)); Assert.Equal(Colors.Orange, ColorOf(dismiss.Foreground));
            action.Styles.Remove(actionStyle); dismiss.Styles.Remove(dismissStyle); Flush(window);
            Assert.Equal(Colors.Red, ColorOf(action.Foreground)); Assert.Equal(Colors.Blue, ColorOf(dismiss.Foreground));
            action.Foreground = Brushes.Yellow; snackbar.IsEnabled = false; Flush(window);
            Assert.Equal(Colors.Yellow, ColorOf(action.Foreground));
            action.ClearValue(Button.ForegroundProperty); Flush(window); Assert.Equal(Colors.Magenta, ColorOf(action.Foreground));
            snackbar.IsEnabled = true; snackbar.DataActionForeground = Brushes.Purple; Flush(window);
            Assert.Equal(Colors.Purple, ColorOf(action.Foreground));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DerivedActionTheme_KeepsSizeClassCandidatesAndUserOverrides()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = new Border { Width = 16, Height = 8 } };
        var mediumText = new TextStyle { FontSize = 30 };
        button.Resources[Comp.ButtonMedium.ContentPadding] = new Thickness(21, 11);
        button.Resources[Comp.ButtonMedium.LabelTextStyle] = mediumText;
        var window = DialogTests.Show(new Grid { Children = { button } });
        try
        {
            button.Theme = (ControlTheme)window.FindResource("MaterialSnackbarActionButtonTheme")!; Flush(window);
            Assert.Equal(new Thickness(12, 8), button.Padding);
            button.Classes.Add("medium"); Flush(window);
            Assert.Equal(new Thickness(21, 11), button.Padding); Assert.Equal(mediumText, MaterialText.GetStyle(button));
            var userText = new TextStyle { FontSize = 17 };
            var style = new Style { Setters = { new Setter(Button.PaddingProperty, new Thickness(5)),
                new Setter(MaterialText.StyleProperty, userText) } };
            button.Styles.Add(style); button.ApplyStyling(); Flush(window);
            Assert.Equal(new Thickness(5), button.Padding); Assert.Equal(userText, MaterialText.GetStyle(button));
            button.Classes.Remove("medium"); Flush(window);
            Assert.Equal(new Thickness(5), button.Padding); Assert.Equal(userText, MaterialText.GetStyle(button));
            button.Styles.Remove(style); Flush(window); Assert.Equal(new Thickness(12, 8), button.Padding);
            button.Resources[Comp.Snackbar.ActionContentPadding] = new Thickness(14, 9); Flush(window);
            Assert.Equal(new Thickness(14, 9), button.Padding);
        }
        finally { window.Close(); }
    }

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private sealed class Data : ISnackbarData, ISnackbarVisuals
    {
        public ISnackbarVisuals Visuals => this;
        public string Message => "Unused message";
        public string ActionLabel => "Action";
        public bool WithDismissAction => true;
        public SnackbarDuration Duration => SnackbarDuration.Indefinite;
        public void PerformAction() { }
        public void Dismiss() { }
    }
}
