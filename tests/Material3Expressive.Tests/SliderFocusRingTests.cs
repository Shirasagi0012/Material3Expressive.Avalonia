using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class SliderFocusRingTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeFocusPreservesCoreGeometryAndAnimatesOutsideThumb(bool vertical)
    {
        using var clock = new MotionTestClockScope();
        var slider = new Slider { Minimum = 0, Maximum = 1, Value = .25,
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal };
        if (vertical) slider.Height = 280; else slider.Width = 280;
        MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.InsetRing);
        var window = DialogTests.Show(new StackPanel { Children = { slider } });
        try
        {
            var thumb = Assert.Single(slider.GetVisualDescendants().OfType<SliderThumb>());
            var presenter = Assert.Single(slider.GetVisualDescendants().OfType<SliderThumbPresenter>());
            var ring = Assert.Single(slider.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            var core = thumb.Bounds; var size = slider.Bounds.Size;
            slider.Focus(NavigationMethod.Tab);
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.Equal(core, thumb.Bounds); Assert.Equal(size, slider.Bounds.Size);
            Assert.Equal(new Rect(-4, -4, core.Width + 8, core.Height + 8), ring.Bounds);
            Assert.All(ring.GetVisualAncestors().TakeWhile(v => v != slider), visual => Assert.False(visual.ClipToBounds, visual.GetType().Name));
            Assert.Equal(4, presenter.FocusPadding); Assert.InRange(ring.Progress, .99f, 1.01f);
            window.FocusManager!.Focus(null);
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.Equal(0, presenter.FocusPadding); Assert.Equal(0, ring.Progress);
            Assert.Equal(.25, slider.Value);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RangeSourcesRemainIndependentAndChangingContentKeepsIndication()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f, StartThumbInteractionSource = source };
        MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.InsetRing);
        var window = DialogTests.Show(new StackPanel { Children = { slider } });
        void Frames() { for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            var start = slider.StartPresenter!; var end = slider.EndPresenter!;
            var geometry = (slider.Bounds.Size, slider.ActualState.StartThumbWidth, slider.ActualState.EndThumbWidth);
            start.Focus(NavigationMethod.Tab); Frames();
            Assert.InRange(start.Ring.Progress, .99f, 1.01f); Assert.Equal(0, end.Ring.Progress);
            Assert.Equal(geometry, (slider.Bounds.Size, slider.ActualState.StartThumbWidth, slider.ActualState.EndThumbWidth));
            end.Focus(NavigationMethod.Tab); Frames();
            Assert.Equal(0, start.Ring.Progress); Assert.InRange(end.Ring.Progress, .99f, 1.01f);
            slider.StartThumb = new Border { Width = 12, Height = 32 }; window.UpdateLayout();
            Assert.Single(start.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            slider.StartThumbInteractionSource = new();
            source.Emit(new MaterialInteractionEvent.Focus()); Frames();
            Assert.Equal(0, start.Ring.Progress);
            MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.Opacity); Frames();
            Assert.Equal(0, end.Ring.Progress); Assert.Equal(0, end.FocusPadding);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void NativeCustomThumbKeepsItsTemplateAndSlotFocusRing()
    {
        using var clock = new MotionTestClockScope();
        var slider = new Slider { Width = 280, Value = 35 };
        MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.InsetRing);
        var window = DialogTests.Show(new StackPanel { Children = { slider } });
        try
        {
            var native = Assert.Single(slider.GetVisualDescendants().OfType<Thumb>());
            var custom = new Border { Width = 20, Height = 32, Background = Brushes.Blue };
            native.Template = new FuncControlTemplate<Thumb>((_, _) => custom);
            window.UpdateLayout();
            var slot = Assert.Single(slider.GetVisualDescendants().OfType<SliderThumbPresenter>());
            slider.Focus(NavigationMethod.Tab);
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.Equal(new Size(20, 32), native.Bounds.Size);
            Assert.Equal(new Size(28, 40), slot.Ring.Bounds.Size);
            Assert.InRange(slot.Ring.Progress, .99f, 1.01f);
            Assert.Same(custom, Assert.Single(native.GetVisualChildren()));
            Assert.True(slot.ZIndex > native.ZIndex);
            slider.IsEnabled = false;
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.Equal(0, slot.Ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExternalRangeFocusPairsKeepPaddingUntilLastFocusEnds()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var slider = new RangeSlider { Width = 280, StartThumbInteractionSource = source };
        MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.InsetRing);
        var panel = new StackPanel { Children = { slider } }; var window = DialogTests.Show(panel);
        try
        {
            var first = new MaterialInteractionEvent.Focus(); var second = new MaterialInteractionEvent.Focus();
            source.Emit(first); source.Emit(second); window.UpdateLayout();
            var presenter = slider.StartPresenter!;
            Assert.Equal(4, presenter.FocusPadding);
            source.Emit(new MaterialInteractionEvent.Unfocus(first)); window.UpdateLayout();
            Assert.Equal(4, presenter.FocusPadding);
            source.Emit(new MaterialInteractionEvent.Unfocus(second)); window.UpdateLayout();
            Assert.Equal(0, presenter.FocusPadding);
            panel.Children.Remove(slider);
            source.Emit(new MaterialInteractionEvent.Focus());
            panel.Children.Add(slider); window.UpdateLayout();
            Assert.Equal(0, presenter.Ring.Progress);
            Assert.Equal(0, presenter.FocusPadding);
        }
        finally { window.Close(); }
    }

}
