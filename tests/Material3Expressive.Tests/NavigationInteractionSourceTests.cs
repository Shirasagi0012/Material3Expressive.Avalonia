using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData("bar")]
    [InlineData("rail")]
    [InlineData("short")]
    [InlineData("drawer")]
    public void ExternalSource_DrivesFeedbackWithoutChangingSelectionOrActivating(string kind)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var item = Create(kind, source);
        var window = DialogTests.Show(new Grid { Children = { item } });
        var controller = MaterialInteraction.GetOrCreate(item); var clicks = 0; item.Click += (_, _) => clicks++;
        var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover); Advance(clock, window);
        Assert.True(controller.StateLayerOpacity > 0);
        var press = new MaterialInteractionEvent.Press(new(32, 16)); source.Emit(press);
        Assert.Equal(1, controller.RippleCount); Assert.False(item.IsPressed);
        item.IsEnabled = false; Flush(window);
        Assert.True(controller.Interaction.HasFlag(InteractionState.Pressed));
        source.Emit(new MaterialInteractionEvent.Release(press)); source.Emit(new MaterialInteractionEvent.HoverExit(hover)); Advance(clock, window);
        Assert.Equal(0, controller.StateLayerOpacity); Assert.Equal(0, controller.RippleCount);
        Assert.Equal(0, clicks); Assert.False(Selected(item));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("bar")]
    [InlineData("rail")]
    [InlineData("short")]
    [InlineData("drawer")]
    public void ReplacementAndReattachment_ObserveOnlyFutureSourceEvents(string kind)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var next = new MaterialInteractionSource(); var item = Create(kind, source);
        var root = new Grid { Children = { item } }; var window = DialogTests.Show(root);
        var controller = MaterialInteraction.GetOrCreate(item);
        source.Emit(new MaterialInteractionEvent.Press(new(32, 16))); Advance(clock, window);
        SetSource(item, next); Flush(window); Assert.Equal(0, controller.RippleCount);
        source.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, window); Assert.Equal(0, controller.StateLayerOpacity);
        next.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, window); Assert.True(controller.StateLayerOpacity > 0);
        root.Children.Remove(item); Flush(window);
        next.Emit(new MaterialInteractionEvent.Press(new(32, 16)));
        root.Children.Add(item); Flush(window); Assert.Equal(0, controller.RippleCount); Assert.Equal(0, controller.StateLayerOpacity);
        root.Children.Clear(); Flush(window); Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData("bar")]
    [InlineData("rail")]
    [InlineData("short")]
    [InlineData("drawer")]
    public void NativeInput_PublishesOwnerCoordinatesAndPairsCancellation(string kind)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var item = Create(kind, source); var window = DialogTests.Show(new Grid { Children = { item } });
        var local = new Point(24, kind == "drawer" ? item.Bounds.Height / 2 : 18); var pointer = item.TranslatePoint(local, window)!.Value;
        window.MouseMove(pointer); window.MouseDown(pointer, MouseButton.Left); Flush(window);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        Assert.Equal(local, press.Position); // the indication maps coordinates, the public source does not
        item.IsEnabled = false; Flush(window);
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        window.MouseUp(pointer, MouseButton.Left); Assert.Empty(events.OfType<MaterialInteractionEvent.Release>());
        item.IsEnabled = true; item.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
        Assert.Same(events.OfType<MaterialInteractionEvent.Press>().Last(), Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ShortItemPositionChange_RebuildsIndicationWithoutCancellingTheNativePress()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var item = (ShortNavigationBarItem)Create("short", source);
        var window = DialogTests.Show(new Grid { Children = { item } });
        var pointer = item.TranslatePoint(new Point(24, 18), window)!.Value;
        window.MouseMove(pointer); window.MouseDown(pointer, MouseButton.Left); Flush(window);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        item.IconPosition = NavigationItemIconPosition.Start; Flush(window);
        Assert.True(item.IsPressed); Assert.Empty(events.OfType<MaterialInteractionEvent.CancelPress>());
        Assert.Equal(0, MaterialInteraction.GetOrCreate(item).RippleCount);
        window.MouseUp(pointer, MouseButton.Left); Flush(window);
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    static Button Create(string kind, MaterialInteractionSource source) => kind switch
    {
        "drawer" => new NavigationDrawerItem { InteractionSource = source, Icon = new Border { Width = 24, Height = 24 }, Content = new Border { Width = 32, Height = 16 } },
        "bar" => new NavigationBarItem { InteractionSource = source, Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 32, Height = 16 } },
        "rail" => new NavigationRailItem { InteractionSource = source, Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 32, Height = 16 } },
        _ => new ShortNavigationBarItem { InteractionSource = source, Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 32, Height = 16 } },
    };
    static bool Selected(Button item) => item switch { NavigationDrawerItem d => d.IsSelected, NavigationBarItem b => b.IsSelected, NavigationRailItem r => r.IsSelected, ShortNavigationBarItem s => s.IsSelected, _ => false };
    static void SetSource(Button item, MaterialInteractionSource source)
    {
        switch (item) { case NavigationDrawerItem d: d.InteractionSource = source; break; case NavigationBarItem b: b.InteractionSource = source; break; case NavigationRailItem r: r.InteractionSource = source; break; case ShortNavigationBarItem s: s.InteractionSource = source; break; }
    }
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window)
    { for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
