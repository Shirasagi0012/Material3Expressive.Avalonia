using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using TimePicker = Material3Expressive.Components.TimePicker;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public sealed class TimeSideControlTests
{
    [AvaloniaTheory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public void SideColumnUsesFixedTargetsAndCenteredVisuals(bool scroll, bool clock24, bool rtl)
    {
        var picker = Create(scroll, clock24, rtl);
        var toggle = new Border { Width = 24, Height = 24 };
        picker.SetValue(TimePicker.ToggleProperty, toggle);
        var window = Show(picker);
        try
        {
            var side = picker.GetVisualDescendants().OfType<TimeSideControlLayout>().Single();
            Assert.Equal(new Size(48, 140), side.Bounds.Size);
            Assert.Equal(clock24 ? 1 : 3, side.Children.Count);
            Assert.Equal(clock24 ? 46 : 1, side.Children[0].Bounds.Y);
            Assert.All(side.Children, child => Assert.Equal(new Size(48, 48), child.Bounds.Size));
            foreach (var button in side.Children.OfType<TimePeriodButton>())
            {
                var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
                Assert.Equal(new Size(40, 40), surface.Bounds.Size);
                Assert.Equal(new Point(4, 4), surface.TranslatePoint(default, button));
            }
            Assert.Equal(new Size(24, 24), toggle.Bounds.Size);
            Assert.Equal(new Point(12, clock24 ? 58 : 109), toggle.TranslatePoint(default, side));
            var field = picker.GetVisualDescendants().OfType<Control>()
                .Single(c => c.Name == (scroll ? "PART_HourScroll" : "PART_HourInput"));
            Assert.Equal(field.TranslatePoint(default, picker)!.Value.Y, side.TranslatePoint(default, picker)!.Value.Y);
            Assert.Equal(!rtl, side.TranslatePoint(default, window)!.Value.X > field.TranslatePoint(default, window)!.Value.X);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TemplateOnlyToggleParticipatesInLayout_AndCanBeRemoved(bool scroll)
    {
        var picker = Create(scroll, false, false);
        picker.SetValue(TimePicker.ToggleTemplateProperty, new FuncDataTemplate<object?>((_, _) => new Border { Width = 24, Height = 24 }));
        var window = Show(picker);
        try
        {
            Assert.Single(picker.GetVisualDescendants().OfType<TimeSideControlLayout>());
            picker.Resources[scroll ? Comp.TimeScroll.TimeFieldWithToggleContainerHeight : Comp.TimeInput.VibrantTimeFieldWithToggleContainerHeight] = 151d;
            Flush(window);
            var side = picker.GetVisualDescendants().OfType<TimeSideControlLayout>().Single();
            Assert.Equal(151, side.Bounds.Height);
            Assert.Equal(6, side.Children[0].Bounds.Y); // (151 - 3*48)/2 uses integer division, then +3.
            picker.SetValue(TimePicker.Is24HourProperty, true); Flush(window);
            side = picker.GetVisualDescendants().OfType<TimeSideControlLayout>().Single();
            Assert.Single(side.Children);
            Assert.Equal(51, side.Children[0].Bounds.Y);
            picker.ClearValue(TimePicker.ToggleTemplateProperty); Flush(window);
            Assert.Empty(picker.GetVisualDescendants().OfType<TimeSideControlLayout>());
            Assert.Equal(new TimeOnly(2, 30), picker.GetValue(TimePicker.SelectedTimeProperty));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MouseSelectsVisiblePeriod_AndRepeatedSelectionPreservesValue()
    {
        var picker = Create(false, false, false);
        picker.SetValue(TimePicker.ToggleProperty, new Border { Width = 24, Height = 24 });
        var window = Show(picker);
        try
        {
            var pm = picker.GetVisualDescendants().OfType<TimePeriodButton>().Last();
            var point = pm.TranslatePoint(new Point(1, 24), window)!.Value; // Outside the 40dp visible surface.
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal(new TimeOnly(2, 30), picker.GetValue(TimePicker.SelectedTimeProperty));
            point = pm.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal(new TimeOnly(14, 30), picker.GetValue(TimePicker.SelectedTimeProperty));
            Assert.True(pm.IsChecked);
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal(new TimeOnly(14, 30), picker.GetValue(TimePicker.SelectedTimeProperty));
            Assert.True(pm.IsChecked);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1, 2)]
    [InlineData(24, 14)]
    public void TouchUsesTheExistingVisualBoundary_NotTheLayoutReservation(double x, int expectedHour)
    {
        var picker = Create(false, false, false);
        picker.SetValue(TimePicker.ToggleProperty, new Border { Width = 24, Height = 24 });
        var window = Show(picker);
        using var touch = new TouchDevice();
        try
        {
            using (window.CaptureRenderedFrame()) { }
            var pm = picker.GetVisualDescendants().OfType<TimePeriodButton>().Last();
            var point = pm.TranslatePoint(new Point(x, 24), window)!.Value;
            var impl = window.PlatformImpl!;
            var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
            impl.Input!(new RawTouchEventArgs(touch, 100, root, RawPointerEventType.TouchBegin, point, RawInputModifiers.None, 1));
            impl.Input!(new RawTouchEventArgs(touch, 200, root, RawPointerEventType.TouchEnd, point, RawInputModifiers.None, 1));
            Flush(window);
            // DECISIONS.md: minimum interactive size reserves layout only. The paired
            // touch-edge case retains the mismatch with Core's expanded touch hit test.
            Assert.Equal(new TimeOnly(expectedHour, 30), picker.GetValue(TimePicker.SelectedTimeProperty));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void VibrantContainedPeriodsUseTheirOwnSpacing_AndSurviveToggleRemoval(bool scroll)
    {
        var picker = Create(scroll, false, false);
        var window = Show(picker);
        try
        {
            StackPanel Periods() => (StackPanel)picker.GetVisualDescendants().OfType<TimePeriodButton>().First().Parent!;
            Assert.Equal(8, Periods().Spacing);
            Assert.All(Periods().Children, x => Assert.Equal(56, x.Height));
            var key = scroll ? Comp.TimeScroll.PeriodSelectorSpacing : Comp.TimeInput.VibrantPeriodSelectorSpacing;
            picker.Resources[key] = 6d; Flush(window);
            Assert.Equal(6, Periods().Spacing);
            Assert.All(Periods().Children, x => Assert.Equal(57, x.Height));
            picker.SetValue(TimePicker.ToggleProperty, new Border()); Flush(window);
            Assert.Single(picker.GetVisualDescendants().OfType<TimeSideControlLayout>());
            picker.SetValue(TimePicker.ToggleProperty, null); Flush(window);
            Assert.Equal(6, Periods().Spacing);
            Assert.All(Periods().Children, x => Assert.Equal(57, x.Height));
        }
        finally { window.Close(); }
    }

    private static TemplatedControl Create(bool scroll, bool clock24, bool rtl)
    {
        TemplatedControl picker = scroll ? new TimeScroll() : new TimeInput();
        picker.SetValue(TimePicker.VariantProperty, TimePickerVariant.Vibrant);
        picker.SetValue(TimePicker.SelectedTimeProperty, new TimeOnly(2, 30));
        picker.SetValue(TimePicker.Is24HourProperty, clock24);
        picker.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        return picker;
    }
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 800, Height = 800 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top;
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
