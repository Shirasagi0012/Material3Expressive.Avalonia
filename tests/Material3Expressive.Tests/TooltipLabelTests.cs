using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipLabelTests
{
    [AvaloniaFact]
    public void OverlappingInteractions_RemoveByIdentity_AndOnlyHideAfterLastEnd()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var label = Create(source);
        var window = DialogTests.Show(new Grid { Children = { label } });
        var hover = new MaterialInteractionEvent.HoverEnter();
        var drag = new MaterialInteractionEvent.DragStart();
        var press = new MaterialInteractionEvent.Press(default);
        Assert.False(label.IsPresented);
        source.Emit(hover); Flush(window); Assert.True(label.IsPresented);
        source.Emit(drag); source.Emit(press);
        source.Emit(new MaterialInteractionEvent.HoverExit(hover));
        source.Emit(new MaterialInteractionEvent.CancelPress(new(default)));
        source.Emit(new MaterialInteractionEvent.DragStop(drag));
        Flush(window); Assert.True(label.IsPresented);
        source.Emit(new MaterialInteractionEvent.Release(press));
        Flush(window); Assert.False(label.IsPresented);
        source.Emit(new MaterialInteractionEvent.Focus());
        Flush(window); Assert.False(label.IsPresented);
        source.Emit(drag); Flush(window); Assert.True(label.IsPresented);
        source.Emit(new MaterialInteractionEvent.DragCancel(drag));
        Flush(window); Assert.False(label.IsPresented);
        source.Emit(press); source.Emit(new MaterialInteractionEvent.CancelPress(press));
        Flush(window); Assert.False(label.IsPresented);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutsideDismiss_PreservesAggregateUntilItBecomesInactive(bool persistent)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var label = Create(source); label.IsPersistent = persistent;
        var window = DialogTests.Show(new Grid { Background = Brushes.White, Children = { label } });
        var hover = new MaterialInteractionEvent.HoverEnter();
        var drag = new MaterialInteractionEvent.DragStart();
        source.Emit(hover); Flush(window); Assert.True(label.IsPresented);
        window.MouseDown(new Point(10, 10), MouseButton.Left);
        window.MouseUp(new Point(10, 10), MouseButton.Left);
        Flush(window); Assert.Equal(persistent, label.IsPresented);
        source.Emit(drag); Flush(window); Assert.Equal(persistent, label.IsPresented);
        source.Emit(new MaterialInteractionEvent.HoverExit(hover));
        source.Emit(new MaterialInteractionEvent.DragStop(drag)); Flush(window);
        Assert.Equal(persistent, label.IsPresented);
        source.Emit(hover); Flush(window); Assert.True(label.IsPresented);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        Flush(window); Assert.True(label.IsPresented);
    }

    [AvaloniaFact]
    public void SourceReplacementAndDetach_CancelCollectionWithoutReplayingOldEvents()
    {
        using var clock = new MotionTestClockScope();
        var old = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var label = Create(old); var root = new Grid { Children = { label } };
        var window = DialogTests.Show(root);
        old.Emit(new MaterialInteractionEvent.DragStart()); Flush(window); Assert.True(label.IsPresented);
        label.InteractionSource = next; Flush(window); Assert.False(label.IsPresented);
        old.Emit(new MaterialInteractionEvent.HoverEnter()); Flush(window); Assert.False(label.IsPresented);
        next.Emit(new MaterialInteractionEvent.HoverEnter()); Flush(window); Assert.True(label.IsPresented);
        root.Children.Remove(label); Flush(window); Assert.False(label.IsPresented);
        next.Emit(new MaterialInteractionEvent.DragStart());
        root.Children.Add(label); Flush(window); Assert.False(label.IsPresented);
        next.Emit(new MaterialInteractionEvent.Press(default)); Flush(window); Assert.True(label.IsPresented);
        label.IsPersistent = true; Flush(window); Assert.True(label.IsPresented);
        label.IsPersistent = false; Flush(window); Assert.False(label.IsPresented);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void PersistentLabel_MeasuresOnlyAnchor_AndTracksPositionAndContent(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var label = Create(null); label.IsPersistent = true;
        label.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = DialogTests.Show(new Grid { Children = { label } });
        var tip = (PlainTooltip)label.Label!;
        Assert.True(label.IsPresented);
        Assert.Equal(new Size(24, 24), label.Bounds.Size);
        Assert.Equal(new Size(80, 24), tip.Bounds.Size);
        var anchor = PhysicalOrigin(label, window);
        Assert.Equal(new Point(anchor.X - 28, anchor.Y - 28), PhysicalOrigin(tip, window));
        label.Margin = new Thickness(40, 20, 0, 0); Flush(window);
        anchor = PhysicalOrigin(label, window);
        Assert.Equal(new Point(anchor.X - 28, anchor.Y - 28), PhysicalOrigin(tip, window));
        ((Border)tip.Content!).Width = 100; Flush(window);
        Assert.Equal(new Size(116, 24), tip.Bounds.Size);
        Assert.Equal(new Point(anchor.X - 46, anchor.Y - 28), PhysicalOrigin(tip, window));
    }

    [AvaloniaFact]
    public void SharedChipInput_ControlsLabel_WithoutTakingFocusOrInterceptingClick()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var chip = new AssistChip { Content = "Save", InteractionSource = source };
        var label = Create(source); label.Content = chip;
        int clicks = 0; chip.Click += (_, _) => clicks++;
        var window = DialogTests.Show(new Grid { Children = { label } });
        var center = chip.TranslatePoint(new Point(chip.Bounds.Width / 2, chip.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center); Flush(window); Assert.True(label.IsPresented);
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left); Flush(window);
        Assert.Equal(1, clicks);
        Assert.Same(chip, window.FocusManager!.GetFocusedElement());
        window.MouseMove(new Point(10, 10)); Flush(window); Assert.False(label.IsPresented);
    }

    [AvaloniaFact]
    public void TemplateOnlyLabel_InheritsDataContext_AndReplacesLiveContent()
    {
        using var clock = new MotionTestClockScope();
        var label = Create(null); label.IsPersistent = true; label.Label = null;
        var data = new object(); label.DataContext = data;
        label.LabelTemplate = new FuncDataTemplate<object>((_, _) => new PlainTooltip { Content = "Template label" });
        var window = DialogTests.Show(new Grid { Children = { label } });
        var presenter = window.GetVisualDescendants().OfType<TooltipPresenter>().Single();
        Assert.IsType<PlainTooltip>(presenter.Child);
        var replacement = new PlainTooltip { Content = "New" };
        label.LabelTemplate = null; label.Label = replacement; Flush(window);
        Assert.Same(replacement, presenter.Child); Assert.Same(data, replacement.DataContext);
    }

    static TooltipLabel Create(MaterialInteractionSource? source) => new()
    {
        InteractionSource = source, Content = new Border { Width = 24, Height = 24 },
        Label = new PlainTooltip { Content = new Border { Width = 64, Height = 8 } },
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
    };
    static Point PhysicalOrigin(Control control, Window window)
    {
        var first = control.TranslatePoint(default, window)!.Value;
        var last = control.TranslatePoint(new Point(control.Bounds.Width, 0), window)!.Value;
        return new Point(Math.Min(first.X, last.X), first.Y);
    }
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
