using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Components;

internal static class SliderSemanticsBounds
{
    internal static Rect Get(Control control, bool vertical)
    {
        if (TopLevel.GetTopLevel(control) is not { } root || control.TransformToVisual(root) is not { } transform) return default;
        var padding = vertical ? new Thickness(0, 10) : new Thickness(10, 0);
        return new Rect(control.Bounds.Size).Inflate(padding).TransformToAABB(transform);
    }
}
internal sealed class RangeSliderAutomationPeer(RangeSlider owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    protected override string GetClassNameCore() => nameof(RangeSlider);
    protected override List<AutomationPeer>? GetChildrenCore() => new[] { owner.StartPresenter, owner.EndPresenter }
        .Where(p => p is not null).Select(p => ControlAutomationPeer.CreatePeerForElement(p!)!).ToList();
}
internal sealed class SliderThumbAutomationPeer(SliderThumbPresenter thumb) : ControlAutomationPeer(thumb), IRangeValueProvider
{
    protected override Rect GetBoundingRectangleCore() => SliderSemanticsBounds.Get(thumb, false);
    internal void NotifyState(RangeSliderState oldState, RangeSliderState newState)
    {
        RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
            (double)(thumb.IsStart ? oldState.ActiveRangeStart : oldState.ActiveRangeEnd),
            (double)(thumb.IsStart ? newState.ActiveRangeStart : newState.ActiveRangeEnd));
        RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty,
            (double)(thumb.IsStart ? oldState.Minimum : oldState.ActiveRangeStart),
            (double)(thumb.IsStart ? newState.Minimum : newState.ActiveRangeStart));
        RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty,
            (double)(thumb.IsStart ? oldState.ActiveRangeEnd : oldState.Maximum),
            (double)(thumb.IsStart ? newState.ActiveRangeEnd : newState.Maximum));
    }
    internal void NotifyValueOrBound(bool startChanged, float oldValue, float newValue) => RaisePropertyChangedEvent(
        startChanged == thumb.IsStart ? RangeValuePatternIdentifiers.ValueProperty
            : thumb.IsStart ? RangeValuePatternIdentifiers.MaximumProperty : RangeValuePatternIdentifiers.MinimumProperty,
        (double)oldValue, (double)newValue);
    private RangeSlider OwnerSlider => thumb.Owner!;
    private RangeSliderState State => OwnerSlider.ActualState;
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
    protected override string GetClassNameCore() => nameof(RangeSlider);
    protected override string GetNameCore()
    {
        if (base.GetNameCore() is { Length: > 0 } name) return name;
        if (thumb.Child is { } content && AutomationProperties.GetName(content) is { Length: > 0 } contentName) return contentName;
        return thumb.IsStart ? "Range start" : "Range end";
    }
    public bool IsReadOnly => !OwnerSlider.IsEffectivelyEnabled;
    public double Minimum => thumb.IsStart ? State.Minimum : State.ActiveRangeStart;
    public double Maximum => thumb.IsStart ? State.ActiveRangeEnd : State.Maximum;
    public double Value => thumb.IsStart ? State.ActiveRangeStart : State.ActiveRangeEnd;
    public double SmallChange => (State.Maximum - State.Minimum) / (State.Steps > 0 ? State.Steps + 1 : 100);
    public double LargeChange => SmallChange * Math.Clamp((State.Steps > 0 ? State.Steps + 1 : 100) / 10, 1, 10);
    public void SetValue(double value)
    {
        if (IsReadOnly) throw new InvalidOperationException("The slider is disabled.");
        OwnerSlider.SetProgress(thumb.IsStart, (float)value);
    }
}
