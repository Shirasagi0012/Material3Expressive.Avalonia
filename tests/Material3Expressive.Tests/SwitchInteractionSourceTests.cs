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

public class SwitchInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Thumb_CountsAllPressAndEndEvents_WithoutChangingSelection(bool selected)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var box = new ToggleSwitch { IsChecked = selected };
        SwitchAppearance.SetInteractionSource(box, source);
        var window = DialogTests.Show(new Grid { Children = { box } });
        var thumb = box.GetVisualDescendants().OfType<SwitchThumbLayout>().Single();
        void Settle() { for (var i=0;i<40;i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
        Settle(); int clicks=0; box.Click += (_,_) => clicks++;
        var press = new MaterialInteractionEvent.Press(new(26,24));
        source.Emit(press); source.Emit(press); Settle(); Assert.Equal(28, thumb.PaintedSize);
        // An unmatched release still decrements ThumbNode's counter; indication has identity semantics.
        source.Emit(new MaterialInteractionEvent.Release(new(default))); Settle(); Assert.Equal(28, thumb.PaintedSize);
        source.Emit(new MaterialInteractionEvent.CancelPress(press)); Settle(); Assert.Equal(selected ? 24 : 16, thumb.PaintedSize);
        source.Emit(new MaterialInteractionEvent.Release(press));
        source.Emit(press); Settle(); Assert.Equal(selected ? 24 : 16, thumb.PaintedSize);
        source.Emit(press); Settle(); Assert.Equal(28, thumb.PaintedSize);
        box.IsEnabled=false; Settle(); Assert.Equal(28, thumb.PaintedSize);
        Assert.Equal(selected, box.IsChecked); Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void ReplacingSource_MovesIndication_ButFixedThumbNodeKeepsItsOriginalCollector()
    {
        using var clock = new MotionTestClockScope();
        var first=new MaterialInteractionSource(); var second=new MaterialInteractionSource();
        var box=new ToggleSwitch(); SwitchAppearance.SetInteractionSource(box,first);
        var root=new Grid { Children={box} }; var window=DialogTests.Show(root);
        var thumb=box.GetVisualDescendants().OfType<SwitchThumbLayout>().Single();
        void Settle() { for(var i=0;i<40;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush(window);} }
        Settle(); var press=new MaterialInteractionEvent.Press(new(26,24));
        first.Emit(press); Settle(); Assert.Equal(28,thumb.PaintedSize);
        SwitchAppearance.SetInteractionSource(box,second); Settle(); Assert.Equal(28,thumb.PaintedSize);
        Assert.Equal(0,MaterialInteraction.GetOrCreate(box).RippleCount);
        first.Emit(new MaterialInteractionEvent.Release(press)); Flush(window);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.InRange(thumb.PaintedSize,16.01,27.99);
        Settle(); Assert.Equal(16,thumb.PaintedSize);
        second.Emit(press); Settle(); Assert.Equal(16,thumb.PaintedSize);
        Assert.Equal(1,MaterialInteraction.GetOrCreate(box).RippleCount);
        root.Children.Remove(box); Flush(window); root.Children.Add(box); Flush(window); Settle();
        first.Emit(press); Settle(); Assert.Equal(16,thumb.PaintedSize);
        second.Emit(press); Settle(); Assert.Equal(28,thumb.PaintedSize);
        root.Children.Clear(); Flush(window); Assert.Equal(0,clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NativeInput_PublishesPairedPressAndRelease_WhileKeepingNativeActivation()
    {
        using var clock=new MotionTestClockScope();
        var source=new MaterialInteractionSource(); var events=new List<MaterialInteractionEvent>(); source.Interaction+=events.Add;
        var box=new ToggleSwitch(); SwitchAppearance.SetInteractionSource(box,source);
        var window=DialogTests.Show(new Grid { Children={box} });
        var center=box.TranslatePoint(new Point(26,24),window)!.Value;
        window.MouseMove(center); window.MouseDown(center,MouseButton.Left); Flush(window);
        window.MouseUp(center,MouseButton.Left); Flush(window);
        Assert.True(box.IsChecked);
        Assert.Same(Assert.Single(events.OfType<MaterialInteractionEvent.Press>()),Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
    }
    static void Flush(Window window){Dispatcher.UIThread.RunJobs();window.UpdateLayout();Dispatcher.UIThread.RunJobs();}
}
