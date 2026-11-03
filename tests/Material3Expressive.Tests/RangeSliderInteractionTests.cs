using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class RangeSliderInteractionTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void PressDragRelease_IsPairedAndAssignedToTheSelectedEnd(bool start)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f,
            StartThumbInteractionSource = start ? source : new(), EndThumbInteractionSource = start ? new() : source };
        var window = DialogTests.Show(new StackPanel { Children = { slider } });
        var presenter = start ? slider.StartPresenter! : slider.EndPresenter!;
        var point = presenter.TranslatePoint(new Point(presenter.Bounds.Width / 2, presenter.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Flush(window);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        Assert.Equal(InteractionState.Pressed, start ? slider.StartInteraction : slider.EndInteraction);
        var destination = point + new Vector(start ? 24 : -24, 0);
        window.MouseMove(destination); Flush(window);
        var cancel = Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>());
        var drag = Assert.Single(events.OfType<MaterialInteractionEvent.DragStart>());
        Assert.Same(press, cancel.Start);
        Assert.True(events.IndexOf(cancel) < events.IndexOf(drag));
        Assert.Equal(InteractionState.Dragged, start ? slider.StartInteraction : slider.EndInteraction);
        window.MouseUp(destination, MouseButton.Left); Flush(window);
        Assert.Same(drag, Assert.Single(events.OfType<MaterialInteractionEvent.DragStop>()).Start);
        Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("disable")]
    [InlineData("detach")]
    [InlineData("state")]
    public void ActiveDragIsCancelledOnLifecycleChange(string change)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f, StartThumbInteractionSource = source };
        var root = new StackPanel { Children = { slider } }; var window = DialogTests.Show(root);
        var point = slider.StartPresenter!.TranslatePoint(new Point(2, 22), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(16, 0)); Flush(window);
        var drag = Assert.Single(events.OfType<MaterialInteractionEvent.DragStart>());
        switch (change)
        {
            case "disable": slider.IsEnabled = false; break;
            case "detach": root.Children.Remove(slider); break;
            case "state": slider.State = new(.1f, .9f); break;
        }
        Flush(window);
        Assert.Same(drag, Assert.Single(events.OfType<MaterialInteractionEvent.DragCancel>()).Start);
        Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction);
        Assert.False(slider.ActualState.IsDragging);
        window.MouseUp(point, MouseButton.Left); window.Close();
    }

    [AvaloniaFact]
    public void ExternalEventsKeepIdentityAndFocusEndDoesNotClearAnExternalPress()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var slider = new RangeSlider { Width = 280, StartThumbInteractionSource = source };
        var other = new Button { Content = "Other" };
        var window = DialogTests.Show(new StackPanel { Children = { slider, other } });
        var first = new MaterialInteractionEvent.Press(default); var second = new MaterialInteractionEvent.Press(default);
        source.Emit(first); source.Emit(second); source.Emit(new MaterialInteractionEvent.Release(new(default)));
        source.Emit(new MaterialInteractionEvent.Release(first));
        slider.StartPresenter!.Focus(NavigationMethod.Tab); Flush(window);
        Assert.Equal(InteractionState.Focused | InteractionState.Pressed, slider.StartInteraction);
        other.Focus(); Flush(window);
        Assert.Equal(InteractionState.Pressed, slider.StartInteraction);
        Assert.Same(Assert.Single(events.OfType<MaterialInteractionEvent.Focus>()), Assert.Single(events.OfType<MaterialInteractionEvent.Unfocus>()).Start);
        source.Emit(new MaterialInteractionEvent.CancelPress(second));
        Assert.Equal(InteractionState.None, slider.StartInteraction);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReentrantSourceReplacementDoesNotRestartTheCancelledGesture(bool duringDrag)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var replacement = new MaterialInteractionSource();
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f, StartThumbInteractionSource = source };
        source.Interaction += interaction =>
        {
            if (duringDrag ? interaction is MaterialInteractionEvent.CancelPress : interaction is MaterialInteractionEvent.Press)
                slider.StartThumbInteractionSource = replacement;
        };
        var window = DialogTests.Show(new StackPanel { Children = { slider } });
        var point = slider.StartPresenter!.TranslatePoint(new Point(2, 22), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(16, 0)); Flush(window);
        // Like ThumbContent, changing the source keeps events already collected. A callback
        // that replaces the source before our Cancel collector runs leaves its prior Press.
        Assert.Equal(duringDrag ? InteractionState.Pressed : InteractionState.None, slider.StartInteraction | slider.EndInteraction);
        Assert.False(slider.ActualState.IsDragging);
        window.MouseUp(point, MouseButton.Left); window.Close();
    }

    [AvaloniaFact]
    public void SourceReplacementKeepsTheThumbsRememberedInteractionsUntilDetached()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var thumb = new SliderThumb { Width = 4, Height = 44, InteractionSource = source };
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f,
            StartThumbInteractionSource = source, StartThumb = thumb };
        var root = new StackPanel { Children = { slider } }; var window = DialogTests.Show(root);
        var point = thumb.TranslatePoint(new Point(2, 22), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(16, 0)); Flush(window);
        var drag = Assert.Single(events.OfType<MaterialInteractionEvent.DragStart>());
        var replacement = new MaterialInteractionSource();
        slider.StartThumbInteractionSource = replacement; thumb.InteractionSource = replacement;
        Assert.Empty(events.OfType<MaterialInteractionEvent.DragCancel>());
        Assert.False(slider.ActualState.IsDragging);
        Assert.Equal(InteractionState.Dragged, thumb.Interaction);
        replacement.Emit(new MaterialInteractionEvent.DragStop(drag));
        Assert.Equal(InteractionState.None, thumb.Interaction);
        window.MouseUp(point, MouseButton.Left); window.Close();
    }

    [AvaloniaFact]
    public void CustomThumbSourceDrivesLabelAndShrinkingWithoutChangingLayoutSize()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var thumb = new SliderThumb { Width = 4, Height = 44, InteractionSource = source };
        var label = new TooltipLabel { InteractionSource = source, Content = thumb,
            Label = new PlainTooltip { Content = new Border { Width = 32, Height = 8 } } };
        var slider = new RangeSlider { Width = 280, RangeStart = .25f, RangeEnd = .75f,
            StartThumbInteractionSource = source, StartThumb = label };
        var window = DialogTests.Show(new StackPanel { Margin = new Thickness(40), Children = { slider } });
        var point = thumb.TranslatePoint(new Point(2, 22), window)!.Value;
        window.MouseMove(point); Flush(window); Assert.True(label.IsPresented);
        window.MouseDown(point, MouseButton.Left); Flush(window);
        Assert.Equal(InteractionState.Pressed, thumb.Interaction);
        Assert.Equal(new Size(4, 44), label.Bounds.Size);
        window.MouseUp(point, MouseButton.Left); window.MouseMove(new Point(2, 2)); Flush(window);
        Assert.False(label.IsPresented); Assert.Equal(InteractionState.None, thumb.Interaction);
        window.Close();
    }

    [AvaloniaFact]
    public void ThumbHoverRespectsOcclusion_AndDetachDoesNotReplayExternalFeedback()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var thumb = new SliderThumb { Width = 4, Height = 44, InteractionSource = source };
        var cover = new Border { Width = 40, Height = 60, Background = Avalonia.Media.Brushes.Red };
        var root = new Grid { Children = { thumb, cover } };
        var window = DialogTests.Show(root);
        var point = thumb.TranslatePoint(new Point(2, 22), window)!.Value;
        window.MouseMove(point); Flush(window);
        Assert.Empty(events.OfType<MaterialInteractionEvent.HoverEnter>());
        cover.IsVisible = false; window.MouseMove(point + new Vector(0, 1)); Flush(window);
        var hover = Assert.Single(events.OfType<MaterialInteractionEvent.HoverEnter>());
        source.Emit(new MaterialInteractionEvent.Press(default));
        root.Children.Remove(thumb); Flush(window);
        Assert.Same(hover, Assert.Single(events.OfType<MaterialInteractionEvent.HoverExit>()).Start);
        Assert.Equal(InteractionState.None, thumb.Interaction);
        root.Children.Insert(0, thumb); Flush(window);
        Assert.Equal(InteractionState.None, thumb.Interaction);
        window.MouseMove(new Point(1, 1)); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ValueCallbackReplacingStateOrSourceDoesNotReviveCancelledDrag(bool replaceState)
    {
        using var clock = new MotionTestClockScope();
        var state = new RangeSliderState(.25f, .75f);
        var slider = new RangeSlider { Width = 280, State = state, StartThumbInteractionSource = new() };
        state.OnValueChange = _ =>
        {
            if (replaceState) slider.State = new(.1f, .9f);
            else slider.StartThumbInteractionSource = new();
        };
        var window = DialogTests.Show(new StackPanel { Children = { slider } });
        var point = slider.StartPresenter!.TranslatePoint(new Point(2, 22), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(16, 0)); Flush(window);
        Assert.False(state.IsDragging); Assert.False(slider.ActualState.IsDragging);
        window.MouseUp(point, MouseButton.Left); Flush(window);
        Assert.False(slider.ActualState.IsDragging);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ReplacingOneSourceDoesNotDisposeTheOtherThumbsFocus(bool start)
    {
        using var clock = new MotionTestClockScope();
        var startSource = new MaterialInteractionSource(); var endSource = new MaterialInteractionSource();
        var events = new List<MaterialInteractionEvent>();
        (start ? startSource : endSource).Interaction += events.Add;
        var slider = new RangeSlider { Width = 280, StartThumbInteractionSource = startSource, EndThumbInteractionSource = endSource };
        var window = DialogTests.Show(slider);
        (start ? slider.StartPresenter : slider.EndPresenter)!.Focus(NavigationMethod.Tab); Flush(window);
        var focus = Assert.Single(events.OfType<MaterialInteractionEvent.Focus>());
        if (start) slider.EndThumbInteractionSource = new(); else slider.StartThumbInteractionSource = new();
        Assert.Empty(events.OfType<MaterialInteractionEvent.Unfocus>());
        Assert.Equal(InteractionState.Focused, start ? slider.StartInteraction : slider.EndInteraction);
        if (start) slider.StartThumbInteractionSource = new(); else slider.EndThumbInteractionSource = new();
        Assert.Same(focus, Assert.Single(events.OfType<MaterialInteractionEvent.Unfocus>()).Start);
        Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction);
        window.Close();
    }

    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
