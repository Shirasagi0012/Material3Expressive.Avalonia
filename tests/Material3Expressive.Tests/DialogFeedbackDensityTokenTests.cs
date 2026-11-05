using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class DialogFeedbackDensityTokenTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Alert_ActionRowSpacingTracksInheritedMinimumAndPreservesTokenOverrides(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Border { Width = 180, Height = 40 };
        var dismiss = new Border { Width = 180, Height = 40 };
        var dialog = new AlertDialog { ConfirmButton = confirm, DismissButton = dismiss, IsOpen = true, AnimateTransition = false };
        var parent = new Grid { Children = { dialog } };
        var window = DialogTests.Show(parent);
        try
        {
            window.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            void Check(double minimum, double spacing)
            {
                MaterialInteractiveSize.SetMinimum(parent, minimum); Flush(window);
                var panel = window.GetVisualDescendants().OfType<AlertDialogButtons>().Single();
                Assert.Equal(spacing, panel.CrossAxisSpacing);
                Assert.Equal(40 + spacing, dismiss.TranslatePoint(default, panel)!.Value.Y - confirm.TranslatePoint(default, panel)!.Value.Y);
            }
            Check(48, 0); Check(44, 4); Check(40, 8); Check(0, 8); Check(64, 0);
            window.Resources[Comp.Dialog.ActionVerticalSpacing] = 13d;
            Check(40, 13); Check(64, 13);
            window.Resources[Comp.Dialog.ActionVerticalSpacing] = 0d;
            Check(40, 0);
            window.Resources.Remove(Comp.Dialog.ActionVerticalSpacing);
            Check(40, 8);
            MaterialInteractiveSize.SetMinimum(dialog, 44); Flush(window);
            Assert.Equal(4, window.GetVisualDescendants().OfType<AlertDialogButtons>().Single().CrossAxisSpacing);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Alert_SlotSpacingChangesMeasuredHeightWithoutAddingAbsentSlots()
    {
        using var clock = new MotionTestClockScope();
        var dialog = new AlertDialog { Icon = new Border { Width = 10, Height = 10 },
            Title = new Border { Width = 10, Height = 10 }, Content = new Border { Width = 40, Height = 40 },
            ConfirmButton = new Border { Width = 40, Height = 40 }, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        try
        {
            window.Resources[Comp.Dialog.ContentPadding] = new Thickness(10);
            window.Resources[Comp.Dialog.IconBottomSpacing] = 7d;
            window.Resources[Comp.Dialog.HeadlineBottomSpacing] = 9d;
            window.Resources[Comp.Dialog.SupportingTextBottomSpacing] = 11d;
            Flush(window);
            var container = window.GetVisualDescendants().OfType<MaterialSurface>().Single(c => c.Name == "PART_Container");
            Assert.Equal(147, container.Bounds.Height);
            window.Resources[Comp.Dialog.SupportingTextBottomSpacing] = 21d;
            Flush(window);
            Assert.Equal(157, container.Bounds.Height);
            dialog.Icon = null;
            Flush(window);
            Assert.Equal(140, container.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Date_ActionPaddingAndWrappingUpdateTogether(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Border { Width = 160, Height = 40 };
        var dismiss = new Border { Width = 160, Height = 40 };
        var dialog = new DatePickerDialog { Content = new Border { Height = 40 }, ConfirmButton = confirm,
            DismissButton = dismiss, IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        try
        {
            window.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            window.Resources[Comp.DatePickerDialog.ActionEndPadding] = 20d;
            window.Resources[Comp.DatePickerDialog.ActionBottomPadding] = 18d;
            window.Resources[Comp.DatePickerDialog.ActionHorizontalSpacing] = 30d;
            window.Resources[Comp.DatePickerDialog.ActionVerticalSpacing] = 22d;
            Flush(window);
            var container = window.GetVisualDescendants().OfType<MaterialSurface>().Single(c => c.Name == "PART_Container");
            var first = confirm.TranslatePoint(default, container)!.Value;
            var second = dismiss.TranslatePoint(default, container)!.Value;
            Assert.Equal(62, second.Y - first.Y);
            var actions = window.GetVisualDescendants().OfType<AlertDialogButtons>().Single();
            Assert.Equal(rtl ? 20 : 180, actions.Bounds.X);
            Assert.Equal(160, container.Bounds.Height);
            window.Resources[Comp.DatePickerDialog.ActionHorizontalSpacing] = 10d;
            Flush(window);
            first = confirm.TranslatePoint(default, container)!.Value;
            second = dismiss.TranslatePoint(default, container)!.Value;
            Assert.Equal(first.Y, second.Y);
            Assert.Equal(170, Math.Abs(first.X - second.X));
            Assert.Equal(98, container.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Snackbar_WidthCapKeepsBranchOrderAndUpdatesLive(bool newLine)
    {
        using var clock = new MotionTestClockScope();
        var message = new Border { Width = 40, Height = 10 };
        var snackbar = new Snackbar { Content = message, Action = new Border { Width = 50, Height = 40 }, ActionOnNewLine = newLine };
        var window = DialogTests.Show(snackbar);
        try
        {
            window.Width = 900;
            window.Resources[Comp.Snackbar.MaxWidth] = 200d;
            window.Resources[Comp.Snackbar.ContentStartPadding] = 20d;
            window.Resources[Comp.Snackbar.ContentEndPadding] = 10d;
            window.Resources[Comp.Snackbar.FirstBaselineOffset] = 35d;
            window.Resources[Comp.Snackbar.NewLineLastBaselineOffset] = 15d;
            window.Resources[Comp.Snackbar.NewLineBottomPadding] = 5d;
            Flush(window);
            Assert.Equal(newLine ? 200 : 230, snackbar.Bounds.Width);
            Assert.Equal(20, message.TranslatePoint(default, snackbar)!.Value.X);
            if (newLine)
            {
                Assert.Equal(35, message.TranslatePoint(default, snackbar)!.Value.Y);
                Assert.Equal(95, snackbar.Bounds.Height);
            }
            window.Resources[Comp.Snackbar.MaxWidth] = 180d;
            Flush(window);
            Assert.Equal(newLine ? 180 : 210, snackbar.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Snackbar_DataMarginIsSharedByTemplateAndMinimumConstraint()
    {
        using var clock = new MotionTestClockScope();
        var snackbar = new Snackbar { Data = new Data(), MinWidth = 300, MaxWidth = 300 };
        var window = DialogTests.Show(snackbar);
        try
        {
            window.Resources[Comp.Snackbar.DataMargin] = new Thickness(20, 10, 30, 15);
            Flush(window);
            var layout = snackbar.GetVisualDescendants().OfType<SnackbarLayout>().Single();
            var inset = snackbar.GetVisualDescendants().OfType<Border>().Single(c => c.Name == "PART_DataInset");
            Assert.Equal(new Thickness(20, 10, 30, 15), inset.Margin);
            Assert.Equal(inset.Margin, layout.DataMargin);
            Assert.Equal(250, layout.Bounds.Width);
            window.Resources[Comp.Snackbar.DataMargin] = new Thickness(8);
            Flush(window);
            Assert.Equal(new Thickness(8), inset.Margin);
            Assert.Equal(284, layout.Bounds.Width);
        }
        finally { window.Close(); }
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }

    private sealed class Data : ISnackbarData, ISnackbarVisuals
    {
        public ISnackbarVisuals Visuals => this;
        public string Message => "Message";
        public string? ActionLabel => null;
        public bool WithDismissAction => false;
        public SnackbarDuration Duration => SnackbarDuration.Indefinite;
        public void PerformAction() { }
        public void Dismiss() { }
    }
}
