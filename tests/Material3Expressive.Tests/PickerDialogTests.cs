using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class PickerDialogTests
{
    [AvaloniaFact]
    public void Date_ReservesActions_AndCollapsesWithContent()
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Height = 700 };
        var confirm = new Border { Width = 80, Height = 40 };
        var dialog = new DatePickerDialog { Content = content, ConfirmButton = confirm, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } }, 640);
        Assert.False(dialog.UsePlatformDefaultWidth);
        Assert.Equal(new Size(360, 568), Part(window, "PART_Container").Bounds.Size);
        Assert.Equal(520, content.GetVisualAncestors().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First().Bounds.Height);
        Assert.Equal(0, dialog.TonalElevation);
        content.Height = 100; Flush(window);
        Assert.Equal(148, Part(window, "PART_Container").Bounds.Height);
        window.Height = 120; Flush(window);
        Assert.Equal(40, confirm.Bounds.Height);
        Assert.Equal(72, content.GetVisualAncestors().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First().Bounds.Height);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Date_CoreButtonWrapping_UsesTwelveDipGap(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Border { Width = 220, Height = 40 };
        var dismiss = new Border { Width = 220, Height = 40 };
        var dialog = new DatePickerDialog { Content = new Border { Height = 40 }, ConfirmButton = confirm,
            DismissButton = dismiss, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        window.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; Flush(window);
        var container = Part(window, "PART_Container");
        var first = confirm.TranslatePoint(default, container)!.Value;
        var second = dismiss.TranslatePoint(default, container)!.Value;
        Assert.Equal(52, second.Y - first.Y);
        Assert.Equal(rtl ? 6 : 134, first.X);
    }

    [AvaloniaTheory]
    [InlineData(199, 320)]
    [InlineData(200, 500)]
    public void Time_ChoosesLayoutFromMeasuredContent(double height, double expectedHeight)
    {
        using var clock = new MotionTestClockScope();
        var dialog = new TimePickerDialog { Content = new Border { Width = 400, Height = height },
            Title = new Border { Width = 80, Height = 25 }, ConfirmButton = new Border { Width = 80, Height = 48 },
            IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } }, 500);
        Assert.False(dialog.UsePlatformDefaultWidth);
        Assert.Equal(expectedHeight, Part(window, "PART_Container").Bounds.Height);
        Assert.Equal(448, Part(window, "PART_Container").Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActionsAndDismissalRemainCallerOwned_AndEmptyTemplatesOccupySlots(bool date)
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Button { Content = "Confirm" };
        var dismiss = new Button { Content = "Cancel" };
        var empty = new FuncDataTemplate<object>((_, _) => new Border());
        Dialog dialog = date
            ? new DatePickerDialog { ConfirmButton = confirm, DismissButton = dismiss, DismissButtonTemplate = null }
            : new TimePickerDialog { ConfirmButton = confirm, DismissButton = dismiss, Title = "Time", ModeToggleButtonTemplate = empty };
        dialog.Content = new TextBox { Text = "Caller editor", Width = 256, Height = 100 };
        dialog.IsOpen = true; dialog.AnimateTransition = false;
        var count = 0; confirm.Click += (_, _) => count++; dismiss.Click += (_, _) => count++;
        var requests = 0; dialog.DismissRequested += (_, _) => requests++;
        var window = DialogTests.Show(new Grid { Children = { dialog } }, 640);
        if (!date) Assert.True(Part(window, "PART_ModeToggle").IsVisible);
        foreach (var button in new[] { confirm, dismiss })
        {
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
        }
        Assert.Equal(2, count); Assert.True(dialog.IsOpen); Assert.Equal(0, requests);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"); Flush(window);
        Assert.Equal(1, requests); Assert.True(dialog.IsOpen);
        dialog.IsOpen = false; Flush(window); Assert.False(dialog.IsPresented);
        dialog.IsOpen = true; Flush(window); Assert.True(dialog.IsPresented);
        window.Content = null; Flush(window); Assert.False(dialog.IsPresented);
    }

    [AvaloniaFact]
    public void Date_TokenOverridesRemainLive_AndContentInheritsSurfaceColor()
    {
        using var clock = new MotionTestClockScope();
        var content = new TextBlock { Text = "Editor" };
        var confirm = new TextBlock { Text = "Confirm" };
        var dialog = new DatePickerDialog { Content = content, ConfirmButton = confirm, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        window.Resources[Comp.DatePickerModal.ContainerWidth] = 380d;
        window.Resources[Sys.Color.OnSurface] = Colors.Red;
        window.Resources[Comp.Dialog.ActionLabelTextColor] = Colors.Blue;
        Flush(window);
        Assert.Equal(380, Part(window, "PART_Container").Bounds.Width);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)content.Foreground!).Color);
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)confirm.Foreground!).Color);
        Assert.Equal(14, confirm.FontSize);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TemplatesAndContentReplacement_RetainDataContextAndSurfaceEnvironment(bool date)
    {
        using var clock = new MotionTestClockScope();
        TextBlock? built = null;
        Dialog dialog = date ? new DatePickerDialog() : new TimePickerDialog();
        dialog.ContentTemplate = new FuncDataTemplate<string>((value, _) => built = new TextBlock { Text = value });
        dialog.Content = "first"; dialog.DataContext = "owner";
        dialog.IsOpen = true; dialog.AnimateTransition = false;
        var emptyAction = new FuncDataTemplate<object>((_, _) => new Border { Width = 80, Height = 40 });
        if (dialog is DatePickerDialog d) { d.ConfirmButtonTemplate = emptyAction; d.DismissButtonTemplate = emptyAction; }
        if (dialog is TimePickerDialog t) { t.ConfirmButtonTemplate = emptyAction; t.TitleTemplate = emptyAction; t.ModeToggleButtonTemplate = emptyAction; }
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        Assert.Equal("first", built!.Text); Assert.Equal("first", built.DataContext);
        Assert.True(Part(window, date ? "PART_Dismiss" : "PART_ModeToggle").IsVisible);
        dialog.Content = "second"; Flush(window);
        Assert.Equal("second", built!.Text); Assert.Equal("second", built.DataContext);
        dialog.ContentTemplate = null;
        var direct = new TextBlock { Text = "direct" };
        dialog.Content = direct; Flush(window);
        Assert.Equal("owner", direct.DataContext);
        window.Resources[Sys.Color.OnSurface] = Colors.Green; Flush(window);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)direct.Foreground!).Color);
    }

    [AvaloniaFact]
    public void Time_ExplicitWidthConstrainsColumn_WithoutStretchingEditor()
    {
        using var clock = new MotionTestClockScope();
        var editor = new NaturalEditor();
        var dialog = new TimePickerDialog { Content = editor, Width = 500, Title = new Border { Width = 80, Height = 24 },
            ConfirmButton = new Border { Width = 80, Height = 40 }, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } }, 640);
        Assert.Equal(new Size(500, 640), Part(window, "PART_Container").Bounds.Size);
        Assert.Equal(new Size(256, 300), editor.Bounds.Size);
        dialog.Width = double.NaN; Flush(window);
        Assert.Equal(new Size(304, 412), Part(window, "PART_Container").Bounds.Size);
        Assert.Equal(new Size(256, 300), editor.Bounds.Size);
    }

    sealed class NaturalEditor : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(256, 300).Constrain(availableSize);
    }

    static Control Part(Window window, string name) => window.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name);
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

