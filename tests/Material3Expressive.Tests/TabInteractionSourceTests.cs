using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;
using Tab = Material3Expressive.Components.Tab;

namespace Material3Expressive.Tests;

public class TabInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExternalFeedback_DoesNotSelectAndIsIndependentOfNativeDisable(bool native)
    {
        using var clock = new MotionTestClockScope();
        var (root, item) = Create(native); var source = new MaterialInteractionSource();
        TabAppearance.SetInteractionSource(item, source); var window = DialogTests.Show(root);
        var controller = MaterialInteraction.GetOrCreate(item); var press = new MaterialInteractionEvent.Press(new(20, 20));
        source.Emit(press); Advance(clock, window); Assert.Equal(1, controller.RippleCount);
        Assert.False(Selected(item)); item.IsEnabled = false; Flush(window);
        Assert.True(controller.Interaction.HasFlag(InteractionState.Pressed));
        source.Emit(new MaterialInteractionEvent.Release(press)); Advance(clock, window);
        Assert.Equal(0, controller.RippleCount); Assert.False(Selected(item));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativePress_PublishesPairsAndReplacementCancelsOnlyItsStarts(bool native)
    {
        using var clock = new MotionTestClockScope();
        var (root, item) = Create(native); var source = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        TabAppearance.SetInteractionSource(item, source); var window = DialogTests.Show(root);
        var pointer = item.TranslatePoint(new Rect(item.Bounds.Size).Center, window)!.Value;
        window.MouseMove(pointer); window.MouseDown(pointer, MouseButton.Left); Flush(window);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        Assert.Equal(native, Selected(item)); // native TabItem selects on down; controlled Tab waits for its app.
        TabAppearance.SetInteractionSource(item, next); Flush(window);
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        Assert.Equal(0, MaterialInteraction.GetOrCreate(item).RippleCount);
        window.MouseUp(pointer, MouseButton.Left); Flush(window);
        Assert.Empty(events.OfType<MaterialInteractionEvent.Release>());
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacingTemplateAndReattaching_DoNotReplaySources(bool native)
    {
        using var clock = new MotionTestClockScope();
        var (root, item) = Create(native); var source = new MaterialInteractionSource();
        TabAppearance.SetInteractionSource(item, source); var host = new Grid { Children = { root } }; var window = DialogTests.Show(host);
        var controller = MaterialInteraction.GetOrCreate(item); source.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, window);
        Assert.True(controller.StateLayerOpacity > 0);
        var templated = (Avalonia.Controls.Primitives.TemplatedControl)item; var template = templated.Template;
        templated.Template = null; templated.ApplyTemplate(); Flush(window);
        source.Emit(new MaterialInteractionEvent.Press(new(20, 20)));
        templated.Template = template; templated.ApplyTemplate(); Flush(window);
        Assert.Equal(0, controller.RippleCount); Assert.Equal(0, controller.StateLayerOpacity);
        host.Children.Clear(); Flush(window); source.Emit(new MaterialInteractionEvent.HoverEnter());
        host.Children.Add(root); Flush(window); Assert.Equal(0, controller.StateLayerOpacity);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    static (Control Root, Control Item) Create(bool native)
    {
        if (native)
        {
            var item = new TabItem { Header = new Border { Width = 48, Height = 16 } };
            return (new TabControl { Width = 300, SelectedIndex = 0, Items = { new TabItem { Header = "First" }, item } }, item);
        }
        var tab = new Tab { Content = new Border { Width = 48, Height = 16 } };
        return (new TabRow { Width = 300, Items = { new Tab { Content = "First", IsSelected = true }, tab } }, tab);
    }
    static bool Selected(Control item) => item is Tab tab ? tab.IsSelected : ((TabItem)item).IsSelected;
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window)
    { for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
