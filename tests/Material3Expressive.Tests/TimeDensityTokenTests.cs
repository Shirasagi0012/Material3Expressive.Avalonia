using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Tests;

public class TimeDensityTokenTests
{
    [AvaloniaFact]
    public void InputDimensionsFollowAliasesWithoutReplacingFocusedInvalidDraft()
    {
        var input = new TimeInput { Is24Hour = false };
        var intermediate = new MaterialTokenKey<double>("test.time.height-alias");
        var height = new MaterialTokenKey<double>("test.time.height");
        input.Resources[Comp.TimeInput.TimeFieldContainerHeight] = new TokenRef(intermediate);
        input.Resources[intermediate] = new TokenRef(height);
        input.Resources[height] = 88d;
        input.Resources[Comp.TimeInput.PeriodSelectorSpacing] = 8d;
        var window = Show(input);
        try
        {
            var hour = input.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "PART_HourInput");
            var separator = input.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Classes.Contains("time-separator"));
            var period = input.GetVisualDescendants().OfType<StackPanel>().Single(x => x.Children.Count == 2 && x.Children.All(c => c is Button));
            Assert.Equal(88d, hour.Height);
            Assert.Equal(88d, period.Height);
            Assert.All(period.Children, x => Assert.Equal(40d, x.Height));
            hour.Focus();
            hour.Text = "99";
            Flush(window);
            Assert.False(input.IsInputValid);
            hour.SelectionStart = 0;
            hour.SelectionEnd = 1;
            var caret = hour.CaretIndex;
            input.Resources[height] = 95d;
            input.Resources[Comp.TimeInput.PeriodSelectorSpacing] = 5d;
            Flush(window);
            Assert.Same(hour, input.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "PART_HourInput"));
            Assert.Equal("99", hour.Text);
            Assert.True(hour.IsFocused);
            Assert.Equal(caret, hour.CaretIndex);
            Assert.Equal(0, hour.SelectionStart);
            Assert.Equal(1, hour.SelectionEnd);
            Assert.Equal(95d, hour.Height);
            Assert.Equal(95d, period.Height);
            Assert.All(period.Children, x => Assert.Equal(45d, x.Height));
            Assert.Equal(95d, separator.Height);
            Assert.Equal(15d, separator.Padding.Top);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClockDimensionsUpdateLabelMeasureAndPreserveNormalizedRadii()
    {
        var picker = new TimePicker { Is24Hour = true, Layout = TimePickerLayout.Vertical };
        picker.Resources[Comp.TimePicker.ClockDialContainerSize] = 320d;
        picker.Resources[Comp.TimePicker.ClockDialLabelContainerSize] = 40d;
        var window = Show(picker);
        try
        {
            var face = picker.GetVisualDescendants().OfType<ClockFace>().Single();
            var labels = face.GetVisualChildren().OfType<Button>().ToArray();
            Assert.Equal(320d, face.Width);
            Assert.Equal(new Size(40, 40), labels[0].Bounds.Size);
            Assert.Equal(LayoutHelper.RoundLayoutValue(160d - 101 * 320d / 256 - 20,
                LayoutHelper.GetLayoutScale(labels[0])), labels[0].Bounds.Y, 6);
            picker.Resources[Comp.TimePicker.ClockDialContainerSize] = 288d;
            picker.Resources[Comp.TimePicker.ClockDialLabelContainerSize] = 36d;
            Flush(window);
            Assert.Same(face, picker.GetVisualDescendants().OfType<ClockFace>().Single());
            Assert.Equal(new Size(36, 36), labels[0].Bounds.Size);
            Assert.Equal(LayoutHelper.RoundLayoutValue(144d - 101 * 288d / 256 - 18,
                LayoutHelper.GetLayoutScale(labels[0])), labels[0].Bounds.Y, 6);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ScrollItemTokenIsLocalToTimeScrollAndUpdatesExistingPanels()
    {
        var scroll = new TimeScroll { Is24Hour = true };
        scroll.Resources[Comp.TimeScroll.ItemHeight] = 32d;
        var window = Show(scroll);
        try
        {
            var panels = scroll.GetVisualDescendants().OfType<DateTimePickerPanel>().ToArray();
            Assert.Equal(2, panels.Length);
            Assert.All(panels, x => Assert.Equal(32d, x.ItemHeight));
            scroll.Resources[Comp.TimeScroll.ItemHeight] = 48d;
            Flush(window);
            Assert.All(panels, x => Assert.Equal(48d, x.ItemHeight));
            Assert.Equal(panels, scroll.GetVisualDescendants().OfType<DateTimePickerPanel>().ToArray());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DialogPaddingAndActionsSpacingUpdateBothMeasurementAndPlacement()
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 300, Height = 100 };
        var dialog = new TimePickerDialog {
            Content = content, Title = new Border { Width = 80, Height = 20 },
            ConfirmButton = new Border { Width = 60, Height = 40 },
            DismissButton = new Border { Width = 60, Height = 40 },
            IsOpen = true, AnimateTransition = false
        };
        dialog.Resources[Comp.TimePickerDialog.HorizontalPadding] = 30d;
        dialog.Resources[Comp.TimePickerDialog.VerticalPadding] = 18d;
        dialog.Resources[Comp.TimePickerDialog.ActionsSpacing] = 12d;
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        try
        {
            var layout = window.GetVisualDescendants().OfType<TimePickerDialogLayout>().Single();
            var actions = window.GetVisualDescendants().OfType<TimePickerDialogActions>().Single();
            Assert.Equal(new Size(360, 196), layout.DesiredSize);
            Assert.Equal(new Point(30, 38), layout.Children[1].Bounds.Position);
            Assert.Equal(12d, actions.Spacing);
            dialog.Resources[Comp.TimePickerDialog.HorizontalPadding] = 20d;
            dialog.Resources[Comp.TimePickerDialog.VerticalPadding] = 26d;
            dialog.Resources[Comp.TimePickerDialog.ActionsSpacing] = 4d;
            Flush(window);
            Assert.Equal(new Size(340, 212), layout.DesiredSize);
            Assert.Equal(new Point(20, 46), layout.Children[1].Bounds.Position);
            Assert.Equal(4d, actions.Spacing);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 800, Height = 800 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        window.Content = control;
        window.Show();
        Flush(window);
        return window;
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
