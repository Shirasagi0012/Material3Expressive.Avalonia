using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class AlertDialogTests
{
    [AvaloniaTheory]
    [InlineData(300, 320)]
    [InlineData(480, 440)]
    [InlineData(600, 580)]
    public void FoundationPlatformWidth_TracksClientShortSide(double height, double expected)
    {
        using var clock = new MotionTestClockScope();
        var dialog = new Dialog { AnimateTransition = false, IsOpen = true, Content = new Border { Width = 700, Height = 40 } };
        var window = DialogTests.Show(new Grid { Children = { dialog } }, height);
        window.Width = 800; Flush(window);
        Assert.Equal(expected, Part(window, "PART_Surface").Bounds.Width);
        dialog.UsePlatformDefaultWidth = false; Flush(window);
        Assert.Equal(700, Part(window, "PART_Surface").Bounds.Width);
        dialog.UsePlatformDefaultWidth = true; window.Height = 300; Flush(window);
        Assert.Equal(320, Part(window, "PART_Surface").Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData(double.NaN, 20, 280)]
    [InlineData(double.NaN, 700, 320)]
    [InlineData(100, 20, 100)]
    [InlineData(400, 20, 320)]
    public void BasicDefaults_AndExplicitWidth(double width, double contentWidth, double expected)
    {
        using var clock = new MotionTestClockScope();
        var dialog = new BasicAlertDialog { Width = width, AnimateTransition = false, IsOpen = true,
            Content = new Border { Width = contentWidth, Height = 40 } };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.Equal(expected, Part(window, "PART_Surface").Bounds.Width);
        Assert.Equal(40, Part(window, "PART_Surface").Bounds.Height);
        Assert.Equal(default, dialog.DesiredSize);
    }

    [AvaloniaFact]
    public void BasicContent_PropagatesMinimumWidth_AndMaximumCanBeOverridden()
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Background = Brushes.Red, Height = 40 };
        var dialog = new BasicAlertDialog { Content = content, IsOpen = true, AnimateTransition = false, UsePlatformDefaultWidth = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.Equal(280, content.Bounds.Width);
        content.Width = 700; window.Width = 800; window.Height = 650; Flush(window);
        Assert.Equal(560, Part(window, "PART_Surface").Bounds.Width);
        dialog.MaxWidth = 650; Flush(window);
        Assert.Equal(650, Part(window, "PART_Surface").Bounds.Width);
    }

    [AvaloniaFact]
    public void Slots_ApplyTypographyAndColors_AndTrackReplacement()
    {
        using var clock = new MotionTestClockScope();
        var icon = new TextBlock { Text = "I" };
        var title = new TextBlock { Text = "Title" };
        var text = new TextBlock { Text = "Body" };
        var confirm = new TextBlock { Text = "Confirm" };
        var dialog = new AlertDialog { Icon = icon, Title = title, Content = text, ConfirmButton = confirm, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.Equal(24, title.FontSize); Assert.Equal(14, text.FontSize); Assert.Equal(14, confirm.FontSize);
        Assert.Equal(BrushColor(dialog.IconForeground), BrushColor(icon.Foreground));
        Assert.Equal(BrushColor(dialog.TitleForeground), BrushColor(title.Foreground));
        Assert.Equal(BrushColor(dialog.TextForeground), BrushColor(text.Foreground));
        window.Resources[Comp.Dialog.HeadlineColor] = Colors.Blue; Flush(window);
        Assert.Equal(Colors.Blue, BrushColor(title.Foreground));
        dialog.Icon = null; Flush(window);
        Assert.False(Part(window, "PART_Icon").IsVisible);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Left, Part(window, "PART_Title").HorizontalAlignment);
        var replacement = new TextBlock { Text = "New", FontSize = 18 };
        dialog.Content = replacement; Flush(window);
        Assert.Equal(18, replacement.FontSize);
    }

    [AvaloniaFact]
    public void OptionalEmptyTemplate_IsPresent_AndButtonsNeverCloseImplicitly()
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Button { Content = "Accept" }; var dismiss = new Button { Content = "Cancel" };
        var dialog = new AlertDialog { ConfirmButton = confirm, DismissButton = dismiss, TitleTemplate = new FuncDataTemplate<object>((_, _) => new Border()),
            IsOpen = true, AnimateTransition = false };
        var clicks = 0; confirm.Click += (_, _) => clicks++; dismiss.Click += (_, _) => clicks++;
        var requests = 0; dialog.DismissRequested += (_, _) => requests++;
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.True(Part(window, "PART_Title").IsVisible);
        foreach (var button in new[] { confirm, dismiss })
        {
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
        }
        Assert.Equal(2, clicks); Assert.Equal(0, requests); Assert.True(dialog.IsOpen);
        dialog.IsOpen = false; Flush(window); Assert.False(dialog.IsPresented);
    }

    [AvaloniaFact]
    public void BodyPresenter_PreservesDataContextAndTemplates_AcrossReplacement()
    {
        using var clock = new MotionTestClockScope();
        var body = new TextBlock();
        body.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Label"));
        var dialog = new AlertDialog { Content = body, DataContext = new { Label = "First" }, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.Equal("First", body.Text); Assert.Equal(14, body.FontSize);
        dialog.DataContext = new { Label = "Second" }; Flush(window);
        Assert.Equal("Second", body.Text);
        TextBlock? templated = null;
        dialog.ContentTemplate = new FuncDataTemplate<string>((data, _) => templated = new TextBlock { Text = data });
        dialog.Content = "Template body"; Flush(window);
        Assert.NotNull(templated); Assert.Equal("Template body", templated.Text); Assert.Equal(14, templated.FontSize);
        dialog.ContentTemplate = null; dialog.Content = body; Flush(window);
        Assert.Equal("Second", body.Text); Assert.Equal(14, body.FontSize);
    }

    [AvaloniaFact]
    public void RealXaml_SlotsAndTemplates_CompileAndOpen()
    {
        using var clock = new MotionTestClockScope();
        var dialog = (AlertDialog)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/AlertDialog.axaml"));
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.True(dialog.IsPresented); Assert.True(Part(window, "PART_Icon").IsVisible);
        Assert.IsType<Button>(dialog.ConfirmButton);
    }

    private static Control Part(Window window, string name) => window.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name);
    private static Color BrushColor(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
