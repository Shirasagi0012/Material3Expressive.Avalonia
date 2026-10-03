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
using NativeSlider = Avalonia.Controls.Slider;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Material3Expressive.Tests;

public sealed class SliderTests
{
    [AvaloniaFact]
    public void Tokens_AreCompleteAndKeepRoleReferences() => TokenScan.AssertComplete(new SliderTokenSource(), typeof(MaterialTokens.Comp.Slider));

    [AvaloniaFact]
    public void MaterialTheme_KeepsNativeNumericDefaultsAndAutomation()
    {
        var slider = new NativeSlider { Width = 280 }; var window = Show(slider);
        try
        {
            Assert.Equal(0, slider.Minimum); Assert.Equal(100, slider.Maximum);
            Assert.Equal(1, slider.SmallChange); Assert.Equal(10, slider.LargeChange);
            Assert.IsType<Avalonia.Controls.Automation.Peers.SliderAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(slider));
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, 280, 48, 276, 16)]
    [InlineData(true, 48, 280, 16, 276)]
    public void Defaults_KeepInteractiveSpaceOutsideThumb(bool vertical, double width, double height, double trackWidth, double trackHeight)
    {
        var slider = new NativeSlider { Maximum = 1, Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal };
        if (vertical) slider.Height = 280; else slider.Width = 280;
        var window = Show(slider);
        try
        {
            Assert.Equal(new Size(width, height), slider.Bounds.Size);
            Assert.Equal(new Size(trackWidth, trackHeight), slider.GetVisualDescendants().OfType<SliderTrack>().Single().Bounds.Size);
            Assert.Equal(new Size(vertical ? 44 : 4, vertical ? 4 : 44), NativeThumb(slider).Bounds.Size);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void NativeTap_UpdatesOnPressAndPreservesTwoWayBinding()
    {
        var source = new NativeSlider { Value = 25 };
        var slider = new NativeSlider { Width = 280 };
        slider.Bind(NativeSlider.ValueProperty, new Avalonia.Data.Binding(nameof(NativeSlider.Value)) { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        var changes = 0; slider.ValueChanged += (_, _) => changes++;
        var window = Show(slider);
        try
        {
            window.MouseDown(new Point(210, 24), MouseButton.Left);
            Assert.InRange(source.Value, 74, 76); Assert.Equal(source.Value, slider.Value); Assert.Equal(1, changes);
            window.MouseUp(new Point(210, 24), MouseButton.Left); Assert.Equal(1, changes);
            source.Value = 10; window.UpdateLayout(); Assert.Equal(10, slider.Value);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void MaterialTemplate_PreservesNativeInputAndEventOrder(bool vertical, bool reversed, bool rtl)
    {
        var material = Capture(false); var baseline = Capture(true);
        Assert.Equal(baseline.Values, material.Values);
        Assert.Equal(baseline.Events, material.Events);
        (double[] Values, double[] Events) Capture(bool plain)
        {
            var slider = new NativeSlider { Width = vertical ? 48 : 280, Height = vertical ? 280 : 48,
                Maximum = 1, Value = 0.25, SmallChange = 0.25, LargeChange = 0.5,
                TickFrequency = 0.25, IsSnapToTickEnabled = true,
                Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal, IsDirectionReversed = reversed,
                FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            if (plain) slider.Template = NativeTemplate(vertical);
            var events = new List<double>(); slider.ValueChanged += (_, e) => events.Add(e.NewValue);
            var window = Show(slider); var values = new List<double>();
            try
            {
                var a = vertical ? new Point(24, 180) : new Point(180, 24);
                var b = vertical ? new Point(24, 230) : new Point(230, 24);
                window.MouseDown(a, MouseButton.Left); values.Add(slider.Value);
                window.MouseMove(b); values.Add(slider.Value);
                window.MouseUp(b, MouseButton.Left); values.Add(slider.Value);
                window.UpdateLayout();
                var thumb = NativeThumb(slider);
                var thumbCenter = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
                window.MouseDown(thumbCenter, MouseButton.Left); values.Add(slider.Value);
                window.MouseMove(a); values.Add(slider.Value);
                window.MouseUp(a, MouseButton.Left); values.Add(slider.Value);
                slider.Focus();
                foreach (var key in new[] { Key.Right, Key.Down, Key.PageUp, Key.Home, Key.End })
                {
                    window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); values.Add(slider.Value);
                    window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); values.Add(slider.Value);
                }
                return (values.ToArray(), events.ToArray());
            }
            finally { window.Close(); }
        }
    }
    [AvaloniaFact]
    public void ProgrammaticAndAutomationValues_KeepNativeCoercionWithoutTickSnapping()
    {
        var slider = new NativeSlider { Width = 280, Maximum = 1, TickFrequency = 0.25, IsSnapToTickEnabled = true, Value = 0.36 };
        var window = Show(slider);
        try
        {
            Assert.Equal(0.36, slider.Value);
            var peer = ControlAutomationPeer.CreatePeerForElement(slider)!;
            Assert.Equal(280, peer.GetBoundingRectangle().Width);
            var provider = Assert.IsAssignableFrom<IRangeValueProvider>(peer);
            provider.SetValue(0.375); Assert.Equal(0.375, slider.Value);
            slider.Value = 1.2; Assert.Equal(1, slider.Value);
            window.Content = null; window.Content = slider; window.UpdateLayout(); Assert.Equal(1, slider.Value);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void PointerFocusAndFeedback_FollowNativeSlider()
    {
        var slider = new NativeSlider { Width = 280 }; var other = new Avalonia.Controls.Button();
        var window = Show(new StackPanel { Children = { slider, other } });
        try
        {
            other.Focus();
            window.MouseDown(new Point(140, 24), MouseButton.Left); window.MouseUp(new Point(140, 24), MouseButton.Left);
            Assert.True(slider.IsFocused); Assert.False(other.IsFocused);
            var visual = slider.GetVisualDescendants().OfType<SliderThumb>().Single();
            Assert.True(visual.Interaction.HasFlag(InteractionState.Focused));
            slider.IsEnabled = false; Assert.Equal(InteractionState.None, visual.Interaction);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DerivedTheme_CustomizesNativeThumbAndKeepsLiveValueLayout()
    {
        var slider = new NativeSlider { Width = 280, Maximum = 1, Value = 0.5 };
        var window = Show(slider);
        try
        {
            var theme = new ControlTheme(typeof(NativeSlider)) { BasedOn = (ControlTheme)window.FindResource("MaterialSliderTheme")! };
            var thumbStyle = new Style(s => s.Nesting().Template().OfType<Thumb>().Name("PART_Thumb"));
            thumbStyle.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
                new FuncControlTemplate<Thumb>((_, _) => new Border { Width = 12, Height = 32, Background = Brushes.Red })));
            theme.Children.Add(thumbStyle); slider.Theme = theme; window.UpdateLayout();
            var thumb = NativeThumb(slider); Assert.Equal(new Size(12, 32), thumb.Bounds.Size);
            var old = thumb.Bounds.X; slider.Value = 0.75; window.UpdateLayout(); Assert.True(thumb.Bounds.X > old);
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
            "vertical" => new NativeSlider { Height = 280, Width = 20, Orientation = Orientation.Vertical },
            "range" => new RangeSlider { Width = 280, Height = 20 },
            _ => new NativeSlider { Width = 280, Height = 20 },
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
    public void ChangingTrackCorner_UpdatesDiscreteThumbAlignment()
    {
        var slider = new NativeSlider { Width = 280, Maximum = 1, Value = 0.25, TickFrequency = 0.25, IsSnapToTickEnabled = true };
        SliderAppearance.SetTrackCornerSize(slider, 8);
        var window = Show(slider);
        try
        {
            var previous = NativeThumb(slider).Bounds.X;
            SliderAppearance.SetTrackCornerSize(slider, 4); window.UpdateLayout();
            Assert.True(NativeThumb(slider).Bounds.X < previous);
            slider.Ticks = new Avalonia.Collections.AvaloniaList<double> { 0, 0.25, 1 }; window.UpdateLayout();
            slider.TickFrequency = 0; slider.Ticks.Clear(); window.UpdateLayout();
            Assert.Equal(69, NativeThumb(slider).Bounds.X);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void IrregularTicks_KeepNativeInputAndAlignInteriorThumbs()
    {
        var slider = new NativeSlider { Width = 280, Maximum = 1, Value = 0.25,
            Ticks = new Avalonia.Collections.AvaloniaList<double> { 0.75, 0.25 }, IsSnapToTickEnabled = true };
        var window = Show(slider);
        try
        {
            Assert.Equal(73, NativeThumb(slider).Bounds.X);
            window.MouseDown(new Point(200, 24), MouseButton.Left);
            window.MouseUp(new Point(200, 24), MouseButton.Left);
            Assert.Equal(0.75, slider.Value);
            slider.Value = 0; window.UpdateLayout(); Assert.Equal(0, NativeThumb(slider).Bounds.X);
            slider.Value = 1; window.UpdateLayout(); Assert.Equal(276, NativeThumb(slider).Bounds.X);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void TokenAndThemeUpdates_ReachDefaultParts()
    {
        var slider = new NativeSlider { Width = 280, IsEnabled = false }; var window = Show(slider);
        try
        {
            var thumb = slider.GetVisualDescendants().OfType<SliderThumb>().Single();
            var track = slider.GetVisualDescendants().OfType<SliderTrack>().Single();
            window.Resources[MaterialTokens.Sys.Color.Surface] = Colors.Green;
            window.Resources[MaterialTokens.Sys.Color.Primary] = Colors.Red;
            window.Resources[MaterialTokens.Comp.Slider.InactiveTrackHeight] = 20d;
            window.UpdateLayout();
            Assert.Equal(Colors.Green, thumb.SurfaceColor); Assert.Equal(Colors.Red, track.ActiveTrackColor);
            Assert.Equal(20, track.Bounds.Height);
            window.RequestedThemeVariant = ThemeVariant.Dark; window.UpdateLayout();
            Assert.Equal(Colors.Green, thumb.SurfaceColor); Assert.Equal(Colors.Red, track.ActiveTrackColor);
        }
        finally { window.Close(); }
    }
    private static Thumb NativeThumb(NativeSlider control) => control.GetVisualDescendants().OfType<Thumb>().Single(t => t.Name == "PART_Thumb");
    private static IControlTemplate NativeTemplate(bool vertical) => new FuncControlTemplate<NativeSlider>((owner, scope) =>
    {
        var track = new Track { Name = "PART_Track", Orientation = owner.Orientation, IsDirectionReversed = owner.IsDirectionReversed,
            Minimum = owner.Minimum, Maximum = owner.Maximum,
            Thumb = new Thumb { Name = "PART_Thumb", Width = vertical ? 44 : 4, Height = vertical ? 4 : 44,
                Template = new FuncControlTemplate<Thumb>((_, _) => new Border { Background = Brushes.Red }) },
            IncreaseButton = TrackButton("PART_IncreaseButton"),
            DecreaseButton = TrackButton("PART_DecreaseButton") };
        scope.Register("PART_Track", track); scope.Register("PART_IncreaseButton", track.IncreaseButton); scope.Register("PART_DecreaseButton", track.DecreaseButton);
        track.Bind(Track.ValueProperty, new Avalonia.Data.Binding(nameof(NativeSlider.Value)) { Source = owner, Mode = Avalonia.Data.BindingMode.TwoWay });
        return track;
        static Avalonia.Controls.Button TrackButton(string name) => new()
        {
            Name = name, Focusable = false,
            Theme = new ControlTheme(typeof(Avalonia.Controls.Button))
            {
                Setters = { new Setter(TemplatedControl.TemplateProperty,
                    new FuncControlTemplate<Avalonia.Controls.Button>((_, _) => new Border { Background = Brushes.Transparent })) }
            }
        };
    });

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
