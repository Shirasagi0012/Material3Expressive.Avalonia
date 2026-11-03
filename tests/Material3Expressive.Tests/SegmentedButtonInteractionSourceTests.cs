using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class SegmentedButtonInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Stacking_UsesSignedPressAndFocusCount_AndSelectionAddsFive(bool selected)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var item = new SegmentedButton { Content = "Day", InteractionSource = source, IsSelected = selected };
        var row = new SingleChoiceSegmentedButtonRow(); row.Items.Add(item);
        var window = DialogTests.Show(row); var baseline = selected ? 5 : 0;
        var press = new MaterialInteractionEvent.Press(default); var second = new MaterialInteractionEvent.Press(default);
        var focus = new MaterialInteractionEvent.Focus(); var hover = new MaterialInteractionEvent.HoverEnter();
        source.Emit(press); source.Emit(second); source.Emit(focus); source.Emit(hover);
        Flush(window); Assert.Equal(baseline + 3, item.ZIndex);
        source.Emit(new MaterialInteractionEvent.Release(press)); Assert.Equal(baseline + 2, item.ZIndex);
        source.Emit(new MaterialInteractionEvent.CancelPress(second)); Assert.Equal(baseline + 1, item.ZIndex);
        source.Emit(new MaterialInteractionEvent.Unfocus(focus)); Assert.Equal(baseline, item.ZIndex);
        source.Emit(new MaterialInteractionEvent.HoverExit(hover)); Assert.Equal(baseline, item.ZIndex);
        source.Emit(new MaterialInteractionEvent.Release(press)); Assert.Equal(baseline - 1, item.ZIndex);
        item.IsSelected = !selected; Assert.Equal((selected ? 0 : 5) - 1, item.ZIndex);
    }

    [AvaloniaFact]
    public void SourceSwap_RetainsCounter_WhileIndicationStartsFresh_AndDetachResetsLifetime()
    {
        using var clock = new MotionTestClockScope();
        var old = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var item = new SegmentedButton { InteractionSource = old, Content = "Day" };
        var row = new SingleChoiceSegmentedButtonRow(); row.Items.Add(item); var window = DialogTests.Show(row);
        var press = new MaterialInteractionEvent.Press(new Point(24, 24));
        old.Emit(press); Assert.Equal(1, item.ZIndex);
        item.InteractionSource = next; Flush(window); Assert.Equal(1, item.ZIndex);
        Assert.Equal(0, MaterialInteraction.GetOrCreate(item).RippleCount);
        old.Emit(new MaterialInteractionEvent.Release(press)); Assert.Equal(1, item.ZIndex);
        next.Emit(new MaterialInteractionEvent.Release(press)); Assert.Equal(0, item.ZIndex);
        next.Emit(press); row.Items.Remove(item); Flush(window);
        next.Emit(press); row.Items.Add(item); Flush(window); Assert.Equal(0, item.ZIndex);
        next.Emit(press); Assert.Equal(1, item.ZIndex);
    }

    [AvaloniaFact]
    public void DataTemplateItems_ApplySourceCountToTheSiblingPresenter()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var row = new SingleChoiceSegmentedButtonRow { ItemsSource = new[] { "Day", "Week" },
            ItemTemplate = new FuncDataTemplate<string>((value, _) => new SegmentedButton { Content = value, InteractionSource = source }) };
        var window = DialogTests.Show(row);
        source.Emit(new MaterialInteractionEvent.Press(default));
        source.Emit(new MaterialInteractionEvent.Focus()); Flush(window);
        foreach (var item in row.GetVisualDescendants().OfType<SegmentedButton>())
            Assert.Equal(2, Assert.IsType<ContentPresenter>(item.GetVisualParent()).ZIndex);
    }

    [AvaloniaFact]
    public void NativePressAndDisable_EmitPairs_WithoutChangingSelection()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var item = new SegmentedButton { Content = "Day", InteractionSource = source };
        var row = new MultiChoiceSegmentedButtonRow(); row.Items.Add(item); var window = DialogTests.Show(row);
        var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center); window.MouseDown(center, MouseButton.Left); Flush(window);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        Assert.Equal(2, item.ZIndex); Assert.False(item.IsSelected);
        item.IsEnabled = false; Flush(window);
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        Assert.Equal(0, item.ZIndex); Assert.False(item.IsSelected);
        window.MouseUp(center, MouseButton.Left);
    }

    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
