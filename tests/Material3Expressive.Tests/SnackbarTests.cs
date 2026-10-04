using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class SnackbarTests
{
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(80)]
    public void LegacyUnspecifiedBaseline_KeepsSingleLineHeight_EvenForTallContent(double height)
    {
        using var clock = new MotionTestClockScope();
        var marker = new Marker(40, height);
        var snackbar = new Snackbar { Content = marker };
        var window = DialogTests.Show(snackbar);
        try
        {
            Assert.Equal(48, snackbar.Bounds.Height);
            Assert.Equal(16, marker.TranslatePoint(default, snackbar)!.Value.X);
            Assert.Equal((48 - height - 12) / 2 + 6, marker.TranslatePoint(default, snackbar)!.Value.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false, 624, 48, 19, 0)]
    [InlineData(false, true, 616, 48, 19, 0)]
    [InlineData(true, false, 600, 84, 30, 42)]
    [InlineData(true, true, 600, 84, 30, 42)]
    public void LegacySlots_PreserveCapPaddingAndTopAlignedNewLineRow(bool newLine, bool dismiss, double width, double height, double contentY, double actionY)
    {
        using var clock = new MotionTestClockScope();
        var content = new Marker(40, 10); var action = new Marker(120, 40); var close = new Marker(24, 24);
        var snackbar = new Snackbar { Content = content, Action = action, DismissAction = dismiss ? close : null, ActionOnNewLine = newLine };
        var window = DialogTests.Show(snackbar); window.Width = 900; Flush(window);
        try
        {
            Assert.Equal(new Size(width, height), snackbar.Bounds.Size);
            Assert.Equal(new Point(16, contentY), content.TranslatePoint(default, snackbar));
            Assert.Equal(new Point(width - (dismiss ? 24 : 8) - 120, actionY), action.TranslatePoint(default, snackbar));
            if (dismiss) Assert.Equal(new Point(width - 24, newLine ? actionY : 12), close.TranslatePoint(default, snackbar));
            snackbar.FlowDirection = FlowDirection.RightToLeft; Flush(window);
            Assert.Equal(width - 16 - 40, content.TranslatePoint(default, snackbar)!.Value.X);
            Assert.Equal(dismiss ? 24 : 8, action.TranslatePoint(default, snackbar)!.Value.X);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeText_AutomaticBaselines_ReactToHardAndSoftWrapping()
    {
        using var clock = new MotionTestClockScope();
        var text = new TextBlock { Text = "First\nSecond", TextWrapping = TextWrapping.Wrap };
        var snackbar = new Snackbar { Content = text };
        var window = DialogTests.Show(snackbar);
        try
        {
            Assert.Equal(14, text.FontSize); Assert.Equal(20, text.LineHeight); Assert.Equal(.2, text.LetterSpacing);
            Assert.Equal(68, snackbar.Bounds.Height);
            Assert.Equal(30, text.TranslatePoint(default, snackbar)!.Value.Y + text.TextLayout.TextLines[0].Baseline, 0);
            text.Text = "Saved"; Flush(window); Assert.Equal(48, snackbar.Bounds.Height);
            snackbar.Content = "A longer message that wraps onto several lines at a narrow width.";
            snackbar.Width = 160; Flush(window);
            var label = Part<ButtonContentPresenter>(snackbar, "PART_ContentPresenter").Child as TextBlock;
            Assert.NotNull(label); Assert.True(label.TextLayout.TextLines.Count > 1);
            Assert.True(snackbar.Bounds.Height >= 68);
            Assert.Equal(30, label.TranslatePoint(default, snackbar)!.Value.Y + label.TextLayout.TextLines[0].Baseline, 0);
            var action = new Marker(24, 24);
            snackbar.Action = action; snackbar.ActionOnNewLine = true; Flush(window);
            Assert.Equal(30, label.TranslatePoint(default, snackbar)!.Value.Y + label.TextLayout.TextLines[0].Baseline, 0);
            var lastBaseline = label.TranslatePoint(default, snackbar)!.Value.Y
                + label.TextLayout.TextLines.Take(label.TextLayout.TextLines.Count - 1).Sum(line => line.Height)
                + label.TextLayout.TextLines[^1].Baseline;
            Assert.Equal(lastBaseline + 12, action.TranslatePoint(default, snackbar)!.Value.Y, 0);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContentScopesAndTokens_UpdateWithoutReplacingCallerOverrides()
    {
        using var clock = new MotionTestClockScope();
        var content = new TextBlock { Text = "Message" }; var action = new TextBlock { Text = "Action" }; var dismiss = new TextBlock { Text = "Close" };
        var snackbar = new Snackbar { Content = content, Action = action, DismissAction = dismiss };
        var window = DialogTests.Show(snackbar);
        try
        {
            Assert.Equal(14, content.FontSize); Assert.Equal(FontWeight.Normal, content.FontWeight);
            Assert.Equal(14, action.FontSize); Assert.Equal(FontWeight.Medium, action.FontWeight);
            Assert.Equal(14, dismiss.FontSize); Assert.Equal(FontWeight.Normal, dismiss.FontWeight);
            snackbar.Foreground = Brushes.Red; snackbar.ActionForeground = Brushes.Blue; snackbar.DismissActionForeground = Brushes.Green;
            Assert.Same(Brushes.Red, content.Foreground); Assert.Same(Brushes.Blue, action.Foreground); Assert.Same(Brushes.Green, dismiss.Foreground);
            window.Resources[Sys.TypeScale.BodyMedium.Size] = 18d; Flush(window); Assert.Equal(18, content.FontSize);
            action.FontSize = 22; window.Resources[Sys.TypeScale.LabelLarge.Size] = 16d; Flush(window); Assert.Equal(22, action.FontSize);
            window.Resources[Comp.Snackbar.ContainerColor] = Colors.Yellow;
            var surface = Part<MaterialSurface>(snackbar, "PART_Container");
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)surface.Background!).Color);
            Assert.Equal(6, surface.ShadowElevation); Assert.Equal(0, surface.TonalElevation);
            snackbar.CornerRadius = new(12); Assert.Equal(new CornerRadius(12), surface.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultHostData_RealButtonClick_CompletesCorrectResult(bool dismiss)
    {
        using var clock = new MotionTestClockScope();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var host = new SnackbarHost();
        var window = DialogTests.Show(host, fluent: true);
        try
        {
            var request = host.State.ShowSnackbarAsync("Saved", "Undo", true, cancellationToken: cancellation.Token);
            Advance(clock, window, 30);
            var snackbar = host.GetVisualDescendants().OfType<Snackbar>().Single();
            Assert.Same(host.State.CurrentSnackbarData, snackbar.Data);
            var surface = Part<MaterialSurface>(snackbar, "PART_Container");
            Assert.Equal(new Point(12, 12), surface.TranslatePoint(default, snackbar));
            Assert.Equal(72, snackbar.Bounds.Height);
            Assert.Equal("Saved", Part<ButtonContentPresenter>(snackbar, "PART_ContentPresenter").Content);
            var message = (TextBlock)Part<ButtonContentPresenter>(snackbar, "PART_ContentPresenter").Child!;
            var actionButton = Part<Button>(snackbar, "PART_DataAction");
            var actionLabel = (TextBlock)Part<ButtonContentPresenter>(actionButton, "PART_ContentPresenter").Child!;
            Assert.Equal(message.TranslatePoint(default, snackbar)!.Value.Y + message.TextLayout.TextLines[0].Baseline,
                actionLabel.TranslatePoint(default, snackbar)!.Value.Y + actionLabel.TextLayout.TextLines[0].Baseline, 0);
            var close = Part<IconButton>(snackbar, "PART_DataDismiss");
            Assert.Equal("Dismiss", AutomationProperties.GetName(close));
            var tooltip = Part<TooltipBox>(snackbar, "PART_DismissTooltip");
            snackbar.DismissActionText = "关闭";
            Assert.Equal("关闭", ((PlainTooltip)tooltip.Tooltip!).Content);
            Assert.Equal("关闭", AutomationProperties.GetName(close));
            var button = dismiss ? close : Part<Button>(snackbar, "PART_DataAction");
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal(dismiss ? SnackbarResult.Dismissed : SnackbarResult.ActionPerformed, await request.WaitAsync(TimeSpan.FromSeconds(2), cancellation.Token));
            Advance(clock, window, 30);
            Assert.Empty(host.GetVisualDescendants().OfType<Snackbar>());
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { cancellation.Cancel(); window.Close(); }
    }

    [AvaloniaFact]
    public void DataAndTemplates_CanBeReplaced_AndStandaloneSlotsRemainCallerOwned()
    {
        using var clock = new MotionTestClockScope();
        var first = new Data("one", "Undo", true); var second = new Data("two", null, false);
        var snackbar = new Snackbar { Data = first };
        var window = DialogTests.Show(snackbar);
        try
        {
            Assert.True(Part<Grid>(snackbar, "PART_Action").IsVisible); Assert.True(Part<Grid>(snackbar, "PART_Dismiss").IsVisible);
            snackbar.Data = second; Flush(window);
            Assert.Equal("two", Part<ButtonContentPresenter>(snackbar, "PART_ContentPresenter").Content);
            Assert.False(Part<Grid>(snackbar, "PART_Action").IsVisible); Assert.False(Part<Grid>(snackbar, "PART_Dismiss").IsVisible);
            snackbar.ContentTemplate = new FuncDataTemplate<object>((_, _) => new Marker(36, 10));
            snackbar.ActionTemplate = new FuncDataTemplate<object>((_, _) => new Marker(48, 20)); Flush(window);
            Assert.IsType<Marker>(Part<ButtonContentPresenter>(snackbar, "PART_ContentPresenter").Child);
            Assert.True(Part<Grid>(snackbar, "PART_Action").IsVisible);
            snackbar.Data = null; Flush(window); Assert.Equal(48, snackbar.Bounds.Height);
            Assert.Equal(0, first.Actions); Assert.Equal(0, first.Dismissals);
        }
        finally { window.Close(); }
    }

    private static T Part<T>(Control owner, string name) where T : Control => owner.GetVisualDescendants().OfType<T>().Single(c => c.Name == name && ReferenceEquals(c.TemplatedParent, owner));
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private static void Advance(MotionTestClockScope clock, Window window, int count)
    {
        for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
    private sealed class Marker(double width, double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(width, height);
    }
    private sealed class Data(string message, string? action, bool dismiss) : ISnackbarData, ISnackbarVisuals
    {
        public ISnackbarVisuals Visuals => this;
        public string Message => message; public string? ActionLabel => action; public bool WithDismissAction => dismiss;
        public SnackbarDuration Duration => SnackbarDuration.Indefinite;
        public int Actions, Dismissals;
        public void PerformAction() => Actions++;
        public void Dismiss() => Dismissals++;
    }
}
