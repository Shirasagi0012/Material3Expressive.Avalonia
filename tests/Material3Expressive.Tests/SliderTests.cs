using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using Xunit;
using MSlider = Material3Expressive.Components.Slider;

namespace Material3Expressive.Tests;

public sealed class SliderTests
{
    [AvaloniaFact]
    public void Tokens_AreCompleteAndKeepRoleReferences() => TokenScan.AssertComplete(new SliderTokenSource(), typeof(MaterialTokens.Comp.Slider));

    [AvaloniaFact]
    public void State_PreservesConstructorAndSetterSnappingDistinction()
    {
        var state = new SliderState(0.36f, 3);
        Assert.Equal(0.36f, state.Value);
        state.Value = 0.36f; Assert.Equal(0.25f, state.Value);
        state.Value = 0.375f; Assert.Equal(0.25f, state.Value); // first-tick tie
        state.ShouldAutoSnap = false; state.Value = 1.2f;
        Assert.Equal(1.2f, state.Value); Assert.Equal(1, state.CoercedValueAsFraction);
        state.ShouldAutoSnap = true; state.Value = -1; Assert.Equal(0, state.Value);
    }
    [AvaloniaFact]
    public void RangeState_SortsInitialValuesAndConstrainsEachThumb()
    {
        var state = new RangeSliderState(0.8f, 0.2f, 3);
        Assert.Equal(0.2f, state.ActiveRangeStart); Assert.Equal(0.8f, state.ActiveRangeEnd);
        state.ActiveRangeStart = 0.36f; state.ActiveRangeEnd = 0.63f;
        Assert.Equal(0.25f, state.ActiveRangeStart); Assert.Equal(0.75f, state.ActiveRangeEnd);
        state.ActiveRangeStart = 5; Assert.Equal(state.ActiveRangeEnd, state.ActiveRangeStart);
        state.ActiveRangeEnd = -5; Assert.Equal(state.ActiveRangeStart, state.ActiveRangeEnd);
    }
    [AvaloniaTheory]
    [InlineData(false, 280, 48, 276, 16)]
    [InlineData(true, 48, 280, 16, 276)]
    public void Defaults_KeepInteractiveSpaceOutsideThumb(bool vertical, double width, double height, double trackWidth, double trackHeight)
    {
        MSlider slider = vertical ? new VerticalSlider { Height = 280 } : new MSlider { Width = 280 };
        var window = Show(slider);
        try
        {
            Assert.Equal(new Size(width, height), slider.Bounds.Size);
            var track = slider.GetVisualDescendants().OfType<SliderTrack>().Single();
            Assert.Equal(new Size(trackWidth, trackHeight), track.Bounds.Size);
            var thumb = slider.GetVisualDescendants().OfType<SliderThumb>().Single();
            Assert.Equal(new Size(vertical ? 44 : 4, vertical ? 4 : 44), thumb.Bounds.Size);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Tap_WritesOnlyOnReleaseAndPreservesValueBinding()
    {
        var source = new SliderState(0.25f);
        var slider = new MSlider { Width = 280 };
        slider.Bind(MSlider.ValueProperty, new Avalonia.Data.Binding(nameof(SliderState.Value)) { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        var finished = 0; slider.ValueChangeFinished += (_, _) => finished++;
        var window = Show(slider);
        try
        {
            window.MouseDown(new Point(210, 24), MouseButton.Left); Assert.Equal(0.25f, source.Value);
            window.MouseUp(new Point(210, 24), MouseButton.Left);
            Assert.InRange(source.Value, 0.74f, 0.76f); Assert.Equal(source.Value, slider.Value); Assert.Equal(1, finished);
            source.Value = 0.1f; window.UpdateLayout(); Assert.Equal(0.1f, slider.ActualState.Value);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, true, Key.Right, 0.26f)]
    [InlineData(false, false, Key.Right, 0.24f)]
    [InlineData(true, true, Key.Down, 0.26f)]
    [InlineData(true, false, Key.Down, 0.24f)]
    public void Keyboard_UsesOnePercentAndDirection(bool vertical, bool forward, Key key, float expected)
    {
        MSlider slider = vertical ? new VerticalSlider { Height = 280, TopToBottom = forward, Value = 0.25f }
            : new MSlider { Width = 280, Value = 0.25f, FlowDirection = forward ? FlowDirection.LeftToRight : FlowDirection.RightToLeft };
        var window = Show(slider); var finished = 0; slider.ValueChangeFinished += (_, _) => finished++;
        try
        {
            slider.Focus(); window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(expected, slider.Value); Assert.Equal(0, finished);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, finished);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void HoistedCallback_OwnsValueUntilItWritesBack()
    {
        var state = new SliderState(0.25f); var requested = 0f;
        state.OnValueChange = value => requested = value;
        var slider = new MSlider { Width = 280, State = state, Value = 0.9f };
        var window = Show(slider);
        try
        {
            slider.Focus(); window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(0.26f, requested); Assert.Equal(0.25f, state.Value);
            state.Value = requested; Assert.Equal(0.26f, state.Value);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Automation_SnapsProgressAndFinishesOnlyForAChange()
    {
        var slider = new MSlider { Width = 280, Steps = 3, Value = 0.25f };
        var window = Show(slider); var finished = 0; slider.ValueChangeFinished += (_, _) => finished++;
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(slider)!;
            Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
            Assert.Equal(300, peer.GetBoundingRectangle().Width); // visual width 280 plus semantics padding
            var provider = Assert.IsAssignableFrom<IRangeValueProvider>(peer);
            provider.SetValue(0.375); Assert.Equal(0.5, provider.Value); Assert.Equal(1, finished); // last-tick accessibility tie
            provider.SetValue(0.5); Assert.Equal(1, finished);
            slider.IsEnabled = false; Assert.True(provider.IsReadOnly); Assert.Throws<InvalidOperationException>(() => provider.SetValue(0.75));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void RangeAutomation_ExposesIndependentFocusableNonCrossingThumbs()
    {
        var slider = new RangeSlider { Width = 280, RangeStart = 0.25f, RangeEnd = 0.75f, Steps = 3 };
        var window = Show(slider);
        try
        {
            var children = ControlAutomationPeer.CreatePeerForElement(slider)!.GetChildren()!;
            Assert.Equal(2, children.Count);
            Assert.All(children, peer => Assert.Equal(24, peer.GetBoundingRectangle().Width));
            var start = Assert.IsAssignableFrom<IRangeValueProvider>(children[0]); var end = Assert.IsAssignableFrom<IRangeValueProvider>(children[1]);
            Assert.Equal(0, start.Minimum); Assert.Equal(0.75, start.Maximum); Assert.Equal(0.25, end.Minimum); Assert.Equal(1, end.Maximum);
            start.SetValue(1); Assert.Equal(0.75f, slider.RangeStart);
            var thumb = Part(slider, "PART_EndThumb"); thumb.Focus();
            window.KeyPress(Key.Home, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(slider.RangeStart, slider.RangeEnd);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DisableAndDetach_CancelAnActiveDragAndRetainState()
    {
        var state = new SliderState(0.25f); var slider = new MSlider { Width = 280, State = state }; var finished = 0;
        state.OnValueChangeFinished = () => finished++;
        var window = Show(slider);
        try
        {
            window.MouseDown(new Point(80, 24), MouseButton.Left); window.MouseMove(new Point(210, 24)); Assert.True(state.IsDragging);
            slider.IsEnabled = false; Assert.False(state.IsDragging); Assert.Equal(1, finished);
            var saved = state.Value; window.Content = null; slider.IsEnabled = true;
            window.Content = slider; window.UpdateLayout(); Assert.Same(state, slider.ActualState); Assert.Equal(saved, state.Value);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void RangeCancellation_ResizeInFinishedCallbackKeepsNextThumbSelectionCorrect()
    {
        var state = new RangeSliderState(0.25f, 0.75f);
        var slider = new RangeSlider { Width = 140, State = state }; var window = Show(slider);
        state.OnValueChangeFinished = () => { slider.Width = 280; window.UpdateLayout(); };
        try
        {
            window.MouseDown(new Point(36, 24), MouseButton.Left); window.MouseMove(new Point(120, 24));
            Assert.Equal(state.ActiveRangeEnd, state.ActiveRangeStart);
            slider.IsEnabled = false; slider.IsEnabled = true; window.UpdateLayout();
            window.MouseDown(new Point(60, 24), MouseButton.Left); window.MouseUp(new Point(60, 24), MouseButton.Left);
            Assert.InRange(state.ActiveRangeStart, 0.20f, 0.22f); Assert.Equal(0.75f, state.ActiveRangeEnd);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void CustomTemplates_ReceiveLiveStateAndControlMeasuredSize()
    {
        var state = new SliderState(0.5f); object? received = null;
        var slider = new MSlider { Width = 280, State = state,
            ThumbTemplate = new FuncDataTemplate<SliderState>((data, _) => { received = data; return new Border { Width = 12, Height = 32, Background = Brushes.Red }; }) };
        var window = Show(slider);
        try
        {
            Assert.Same(state, received); Assert.Equal(new Size(12, 32), Part(slider, "PART_Thumb").Child!.Bounds.Size);
            var old = Part(slider, "PART_Thumb").Bounds.X; state.Value = 0.75f; window.UpdateLayout(); Assert.True(Part(slider, "PART_Thumb").Bounds.X > old);
            slider.ThumbTemplate = null; window.UpdateLayout(); Assert.IsType<SliderThumb>(Part(slider, "PART_Thumb").Child);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("horizontal")]
    [InlineData("vertical")]
    [InlineData("range")]
    public void ConstrainedThumb_ObeysParentMaximum(string kind)
    {
        Control slider = kind switch
        {
            "vertical" => new VerticalSlider { Height = 280, Width = 20 },
            "range" => new RangeSlider { Width = 280, Height = 20 },
            _ => new MSlider { Width = 280, Height = 20 },
        };
        var window = Show(slider);
        try
        {
            foreach (var thumb in slider.GetVisualDescendants().OfType<SliderThumb>())
                Assert.Equal(20, kind == "vertical" ? thumb.Bounds.Width : thumb.Bounds.Height);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void PointerGesture_PreservesOtherFocusAndKeyboardFocusRemainsAvailable()
    {
        var other = new Avalonia.Controls.Button(); var slider = new MSlider { Width = 280 };
        var panel = new StackPanel { Children = { slider, other } }; var window = Show(panel);
        try
        {
            other.Focus(); Assert.True(other.IsFocused);
            window.MouseDown(new Point(140, 24), MouseButton.Left); window.MouseUp(new Point(140, 24), MouseButton.Left);
            Assert.True(other.IsFocused); Assert.False(slider.IsFocused);
            slider.Focus(NavigationMethod.Tab); Assert.True(slider.IsFocused);
            Assert.True(slider.Interaction.HasFlag(Material3Expressive.Foundation.Interaction.InteractionState.Focused));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ReplacingHoistedState_UpdatesDefaultTrackAndTemplate()
    {
        var previous = new SliderState(0.25f); var next = new SliderState(0.75f);
        var slider = new MSlider { Width = 280, State = previous }; var window = Show(slider);
        try
        {
            slider.State = next; window.UpdateLayout();
            Assert.Same(next, slider.ActualState); Assert.Same(next, slider.GetVisualDescendants().OfType<SliderTrack>().Single().State);
            var x = Part(slider, "PART_Thumb").Bounds.X;
            previous.Value = 0.9f; window.UpdateLayout(); Assert.Equal(x, Part(slider, "PART_Thumb").Bounds.X);
            next.Value = 0.1f; window.UpdateLayout(); Assert.True(Part(slider, "PART_Thumb").Bounds.X < x);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ChangingTrackCorner_UpdatesDiscreteThumbAlignment()
    {
        var state = new SliderState(0.25f, 3);
        var track = new SliderTrack { State = state, TrackCornerSize = 8, EnableCornerShrinking = true };
        var slider = new MSlider { Width = 280, State = state, Track = track }; var window = Show(slider);
        try
        {
            var previous = Part(slider, "PART_Thumb").Bounds.X;
            track.TrackCornerSize = 4; window.UpdateLayout();
            Assert.True(Part(slider, "PART_Thumb").Bounds.X < previous);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void RangeAutomation_UsesCustomThumbNamesAndReplacementState()
    {
        var start = new SliderThumb { Width = 4, Height = 44 };
        var end = new SliderThumb { Width = 4, Height = 44 };
        Avalonia.Automation.AutomationProperties.SetName(start, "下限");
        Avalonia.Automation.AutomationProperties.SetName(end, "上限");
        var slider = new RangeSlider { Width = 280, StartThumb = start, EndThumb = end }; var window = Show(slider);
        try
        {
            var children = ControlAutomationPeer.CreatePeerForElement(slider)!.GetChildren()!;
            Assert.Equal("下限", children[0].GetName()); Assert.Equal("上限", children[1].GetName());
            slider.State = new RangeSliderState(20, 80, minimum: 10, maximum: 90); window.UpdateLayout();
            var lower = Assert.IsAssignableFrom<IRangeValueProvider>(children[0]);
            var upper = Assert.IsAssignableFrom<IRangeValueProvider>(children[1]);
            Assert.Equal(20, lower.Value); Assert.Equal(80, lower.Maximum); Assert.Equal(10, lower.Minimum);
            Assert.Equal(80, upper.Value); Assert.Equal(20, upper.Minimum); Assert.Equal(90, upper.Maximum);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void TokenAndThemeUpdates_ReachDefaultParts()
    {
        var slider = new MSlider { Width = 280, IsEnabled = false }; var window = Show(slider);
        try
        {
            var thumb = slider.GetVisualDescendants().OfType<SliderThumb>().Single();
            var track = slider.GetVisualDescendants().OfType<SliderTrack>().Single();
            window.Resources[MaterialTokens.Sys.Color.Surface] = Colors.Green;
            window.Resources[MaterialTokens.Sys.Color.Primary] = Colors.Red;
            window.UpdateLayout();
            Assert.Equal(Colors.Green, thumb.SurfaceColor); Assert.Equal(Colors.Red, track.ActiveTrackColor);
            window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark; window.UpdateLayout();
            Assert.Equal(Colors.Green, thumb.SurfaceColor); Assert.Equal(Colors.Red, track.ActiveTrackColor);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void RangeBinding_PreservesBothSourcesAcrossInputAndProgrammaticUpdates()
    {
        var source = new RangeSliderState(0.25f, 0.75f);
        var slider = new RangeSlider { Width = 280 };
        slider.Bind(RangeSlider.RangeStartProperty, new Avalonia.Data.Binding(nameof(RangeSliderState.ActiveRangeStart)) { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        slider.Bind(RangeSlider.RangeEndProperty, new Avalonia.Data.Binding(nameof(RangeSliderState.ActiveRangeEnd)) { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        var window = Show(slider);
        try
        {
            window.MouseDown(new Point(252, 24), MouseButton.Left); window.MouseUp(new Point(252, 24), MouseButton.Left);
            Assert.InRange(source.ActiveRangeEnd, 0.9f, 0.92f); Assert.Equal(0.25f, source.ActiveRangeStart);
            source.ActiveRangeEnd = 0.95f; source.ActiveRangeStart = 0.85f; window.UpdateLayout();
            Assert.Equal(0.85f, slider.ActualState.ActiveRangeStart); Assert.Equal(0.95f, slider.ActualState.ActiveRangeEnd);
            source.ActiveRangeStart = 0.1f; source.ActiveRangeEnd = 0.2f; window.UpdateLayout();
            Assert.Equal(0.1f, slider.ActualState.ActiveRangeStart); Assert.Equal(0.2f, slider.ActualState.ActiveRangeEnd);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, true, 0)]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 3)]
    [InlineData(false, false, 3)]
    [InlineData(true, true, 3)]
    [InlineData(true, false, 3)]
    public void OverlappingRange_DragChoosesDirectionAndKeepsThumb(bool rtl, bool start, int steps)
    {
        var state = new RangeSliderState(0.5f, 0.5f, steps);
        var slider = new RangeSlider { Width = 280, State = state, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(slider); var finished = 0; state.OnValueChangeFinished = () => finished++;
        try
        {
            var center = ThumbCenter(slider, window); var direction = (start ? -1 : 1) * (rtl ? -1 : 1);
            var opened = center + new Vector(direction * 80, 0);
            window.MouseDown(center, MouseButton.Left); window.UpdateLayout(); window.MouseMove(opened);
            Assert.Equal(0.5f, start ? state.ActiveRangeEnd : state.ActiveRangeStart);
            Assert.InRange(start ? state.ActiveRangeStart : state.ActiveRangeEnd, start ? 0.18f : 0.75f, start ? 0.25f : 0.82f);
            Assert.Equal(InteractionState.Dragged, start ? slider.StartInteraction : slider.EndInteraction);
            Assert.Equal(InteractionState.None, start ? slider.EndInteraction : slider.StartInteraction);
            // Crossing the initial position closes the interval, but must not switch thumbs.
            window.MouseMove(center - new Vector(direction * 60, 0));
            Assert.Equal(0.5f, state.ActiveRangeStart); Assert.Equal(0.5f, state.ActiveRangeEnd);
            Assert.Equal(InteractionState.Dragged, start ? slider.StartInteraction : slider.EndInteraction);
            window.MouseMove(opened); window.MouseUp(opened, MouseButton.Left);
            Assert.Equal(0.5f, start ? state.ActiveRangeEnd : state.ActiveRangeStart);
            Assert.True(state.ActiveRangeStart < state.ActiveRangeEnd);
            Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction); Assert.Equal(1, finished);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void OverlappingRange_ReleaseDisplacementChoosesDirection(bool rtl, bool start)
    {
        var state = new RangeSliderState(0.5f, 0.5f, 3);
        var slider = new RangeSlider { Width = 280, State = state, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(slider); var finished = 0; state.OnValueChangeFinished = () => finished++;
        try
        {
            var center = ThumbCenter(slider, window); var direction = (start ? -1 : 1) * (rtl ? -1 : 1);
            window.MouseDown(center, MouseButton.Left); window.MouseUp(center + new Vector(direction * 80, 0), MouseButton.Left);
            Assert.Equal(start ? 0.25f : 0.5f, state.ActiveRangeStart); Assert.Equal(start ? 0.5f : 0.75f, state.ActiveRangeEnd);
            Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction); Assert.Equal(1, finished);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void OverlappingRange_TapKeepsPositionBasedSelection(bool rtl, bool start)
    {
        var state = new RangeSliderState(0.5f, 0.5f, 3);
        var slider = new RangeSlider { Width = 280, State = state, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(slider);
        try
        {
            var point = ThumbCenter(slider, window) + new Vector((start ? -1 : 1) * (rtl ? -1 : 1) * 80, 0);
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            Assert.Equal(start ? 0.25f : 0.5f, state.ActiveRangeStart); Assert.Equal(start ? 0.5f : 0.75f, state.ActiveRangeEnd);
            Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("vertical")]
    [InlineData("disable")]
    [InlineData("detach")]
    public void OverlappingRange_CancelBeforeDirectionDoesNotLeakIntoNextGesture(string cancellation)
    {
        var state = new RangeSliderState(0.5f, 0.5f);
        var slider = new RangeSlider { Width = 280, State = state }; var window = Show(slider);
        var finished = 0; state.OnValueChangeFinished = () => finished++;
        try
        {
            var center = ThumbCenter(slider, window); window.MouseDown(center, MouseButton.Left);
            if (cancellation == "vertical") window.MouseMove(center + new Vector(0, 40));
            else if (cancellation == "disable") { slider.IsEnabled = false; slider.IsEnabled = true; }
            else { window.Content = null; window.Content = slider; }
            window.UpdateLayout(); window.MouseUp(center, MouseButton.Left);
            Assert.Equal(0.5f, state.ActiveRangeStart); Assert.Equal(0.5f, state.ActiveRangeEnd);
            Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction); Assert.Equal(0, finished);
            center = ThumbCenter(slider, window); var target = center - new Vector(80, 0);
            window.MouseDown(center, MouseButton.Left); window.MouseMove(target); window.MouseUp(target, MouseButton.Left);
            Assert.True(state.ActiveRangeStart < 0.5f); Assert.Equal(0.5f, state.ActiveRangeEnd); Assert.Equal(1, finished);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonOverlappingRange_DragRetainsNearestThumbDespiteMovementDirection(bool rtl)
    {
        var state = new RangeSliderState(0.25f, 0.75f);
        var slider = new RangeSlider { Width = 280, State = state, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(slider);
        try
        {
            var center = ThumbCenter(slider, window); var target = center + new Vector((rtl ? -1 : 1) * 60, 0);
            window.MouseDown(center, MouseButton.Left); window.MouseMove(target);
            Assert.InRange(state.ActiveRangeStart, 0.45f, 0.49f); Assert.Equal(0.75f, state.ActiveRangeEnd);
            Assert.Equal(InteractionState.Dragged, slider.StartInteraction); Assert.Equal(InteractionState.None, slider.EndInteraction);
            window.MouseUp(target, MouseButton.Left);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverlappingRange_JitterBelowSlopDoesNotChooseDragThumb(bool rtl)
    {
        var state = new RangeSliderState(0.5f, 0.5f);
        var slider = new RangeSlider { Width = 280, State = state, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(slider);
        try
        {
            var center = ThumbCenter(slider, window); window.MouseDown(center, MouseButton.Left);
            window.MouseMove(center + new Vector((rtl ? -1 : 1) * 0.02, 0.08));
            Assert.Equal(0.5f, state.ActiveRangeStart); Assert.Equal(0.5f, state.ActiveRangeEnd);
            Assert.False((slider.StartInteraction | slider.EndInteraction).HasFlag(InteractionState.Dragged));
            var target = center + new Vector((rtl ? 1 : -1) * 80, 0);
            window.MouseMove(target); window.MouseUp(target, MouseButton.Left);
            Assert.True(state.ActiveRangeStart < 0.5f); Assert.Equal(0.5f, state.ActiveRangeEnd);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, 0f)]
    [InlineData(true, 0f)]
    [InlineData(false, 1f)]
    [InlineData(true, 1f)]
    public void OverlappingRange_AtBoundaryOpensInward(bool rtl, float value)
    {
        var state = new RangeSliderState(value, value, 3);
        var slider = new RangeSlider { Width = 280, State = state, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(slider);
        try
        {
            var center = ThumbCenter(slider, window); var target = center + new Vector((value == 0 ? 1 : -1) * (rtl ? -1 : 1) * 80, 0);
            window.MouseDown(center, MouseButton.Left); window.MouseMove(target); window.MouseUp(target, MouseButton.Left);
            Assert.Equal(value == 0 ? 0f : 0.75f, state.ActiveRangeStart); Assert.Equal(value == 0 ? 0.25f : 1f, state.ActiveRangeEnd);
            Assert.Equal(InteractionState.None, slider.StartInteraction | slider.EndInteraction);
        }
        finally { window.Close(); }
    }
    private static Point ThumbCenter(RangeSlider slider, Window window)
    {
        var thumb = Part(slider, "PART_StartThumb").Child!;
        return thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
    }
    private static ContentPresenter Part(Control control, string name) => control.GetVisualDescendants().OfType<ContentPresenter>().Single(x => x.Name == name);
    private static Window Show(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 320, Height = 320 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content; window.Show(); window.UpdateLayout(); return window;
    }
}
