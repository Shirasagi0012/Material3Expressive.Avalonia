using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class LegacySliderTrackTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeTemplateKeepsNativeInputAndUsesUninsetTickPositions(bool vertical)
    {
        using var clock = new MotionTestClockScope();
        var slider = new Slider { Width = vertical ? 48 : 280, Height = vertical ? 280 : 48,
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal,
            Minimum = 0, Maximum = 1, Value = .25, TickFrequency = .25, SmallChange = .25 };
        var window = Show(slider, nativeTheme: true);
        try
        {
            var painter = slider.GetVisualDescendants().OfType<LegacySliderTrack>().Single();
            var track = slider.GetVisualDescendants().OfType<Track>().Single();
            Assert.Equal(new SliderRange(0, .25f), painter.ActiveRange);
            Assert.Equal(new float[] { 0, .25f, .5f, .75f, 1 }, painter.TickFractions);
            Assert.Equal(vertical ? 207 : 69, vertical ? track.Thumb!.Bounds.Y : track.Thumb!.Bounds.X);
            var changes = 0; slider.ValueChanged += (_, _) => changes++;
            slider.Focus(); window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(.5, slider.Value); Assert.Equal(1, changes);
            Assert.Equal(new SliderRange(0, .5f), painter.ActiveRange);
            slider.Value = .75; window.UpdateLayout();
            Assert.Equal(2, changes);
            Assert.Equal(vertical ? 69 : 207, vertical ? track.Thumb!.Bounds.Y : track.Thumb!.Bounds.X);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeTicksRemainObservableAndExplicitRangeOverridesOwner()
    {
        using var clock = new MotionTestClockScope();
        var ticks = new AvaloniaList<double> { 0, 30, 100 };
        var slider = new Slider { Width = 280, Value = 30, Ticks = ticks };
        var window = Show(slider, nativeTheme: true);
        try
        {
            var painter = slider.GetVisualDescendants().OfType<LegacySliderTrack>().Single();
            Assert.Equal(new float[] { 0, .3f, 1 }, painter.TickFractions);
            ticks.Add(60); Assert.Equal(new float[] { 0, .3f, .6f, 1 }, painter.TickFractions);
            slider.Ticks = new AvaloniaList<double> { 25, 75 };
            Assert.Equal(new float[] { .25f, .75f }, painter.TickFractions);
            painter.State = new RangeSliderState(.2f, .8f);
            slider.Value = 90;
            Assert.Equal(new SliderRange(.2f, .8f), painter.ActiveRange);
            painter.State = null; Assert.Equal(new SliderRange(0, .9f), painter.ActiveRange);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RangeSlotUsesActualStateAcrossReplacementAndRemount(bool template)
    {
        using var clock = new MotionTestClockScope();
        var first = new RangeSliderState(.25f, .75f, 3);
        var slider = new RangeSlider { Width = 280, State = first };
        if (template) slider.TrackTemplate = new FuncDataTemplate<RangeSliderState>((state, _) => new LegacySliderTrack { State = state });
        else slider.Track = new LegacySliderTrack();
        var window = Show(slider);
        try
        {
            var painter = slider.GetVisualDescendants().OfType<LegacySliderTrack>().Single();
            Assert.Equal(new SliderRange(.25f, .75f), painter.ActiveRange);
            var start = slider.GetVisualDescendants().OfType<SliderThumbPresenter>().Single(x => x.Name == "PART_StartThumb");
            Assert.Equal(69, start.Bounds.X);
            first.ActiveRangeStart = .5f; window.UpdateLayout();
            Assert.Equal(new SliderRange(.5f, .75f), painter.ActiveRange);
            slider.State = new RangeSliderState(.1f, .9f); window.UpdateLayout();
            painter = slider.GetVisualDescendants().OfType<LegacySliderTrack>().Single();
            Assert.Equal(new SliderRange(.1f, .9f), painter.ActiveRange);
            window.Content = null; window.UpdateLayout();
            first.ActiveRangeStart = 0; slider.State.ActiveRangeStart = .2f;
            window.Content = slider; window.UpdateLayout();
            Assert.Equal(new SliderRange(.2f, .9f), painter.ActiveRange);
            Assert.Equal(.9f, slider.State.ActiveRangeEnd);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StandaloneTrackUsesSharedTokensAndDoesNotMutateState()
    {
        using var clock = new MotionTestClockScope();
        var state = new RangeSliderState(.3f, .7f);
        var painter = new LegacySliderTrack { State = state, Width = 120 };
        var window = Show(painter);
        try
        {
            Assert.Equal(16, painter.TrackHeight); Assert.Equal(4, painter.TickSize);
            window.Resources[MaterialTokens.Comp.Slider.InactiveTrackHeight] = 10d;
            window.UpdateLayout(); Assert.Equal(10, painter.Bounds.Height);
            window.MouseDown(new Point(80, 5), MouseButton.Left); window.MouseUp(new Point(80, 5), MouseButton.Left);
            Assert.Equal(.3f, state.ActiveRangeStart); Assert.Equal(.7f, state.ActiveRangeEnd);
            Assert.False(painter.Focusable);
            painter.IsEnabled = false; Assert.Equal(new SliderRange(.3f, .7f), painter.ActiveRange);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RangeConfigurationRearrangesThumbsWithoutRequiringASizeChange(bool legacy)
    {
        using var clock = new MotionTestClockScope();
        var slider = new RangeSlider { Width = 280, State = new RangeSliderState(.25f, 1) };
        if (legacy) slider.Track = new LegacySliderTrack();
        var window = Show(slider);
        try
        {
            var end = slider.GetVisualDescendants().OfType<SliderThumbPresenter>().Single(x => x.Name == "PART_EndThumb");
            Assert.Equal(276, end.Bounds.X);
            slider.FlowDirection = FlowDirection.RightToLeft; window.UpdateLayout();
            Assert.Equal(0, end.Bounds.X);
            slider.State = new RangeSliderState(.25f, .75f); window.UpdateLayout();
            Assert.Equal(69, end.Bounds.X);
            Assert.Equal(.75f, slider.State.ActiveRangeEnd);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control content, bool nativeTheme = false)
    {
        content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 320, Height = 320 };
        window.Styles.Add(new MaterialTheme()); window.Content = content; window.Show();
        if (nativeTheme) content.Theme = (ControlTheme)content.FindResource("MaterialLegacySliderTheme")!;
        window.UpdateLayout(); return window;
    }
}
