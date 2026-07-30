// Upstream behavior source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Slider.kt:
// RangeSliderImpl installs distinct focus, keyboard, and progress semantics on both thumbs.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialRangeSliderAutomationPeer : ControlAutomationPeer
{
    private readonly MaterialRangeSliderThumbAutomationPeer _startPeer;
    private readonly MaterialRangeSliderThumbAutomationPeer _endPeer;
    private readonly IReadOnlyList<AutomationPeer> _children;

    public MaterialRangeSliderAutomationPeer(MaterialRangeSlider owner)
        : base(owner)
    {
        _startPeer = new MaterialRangeSliderThumbAutomationPeer(owner, startEndpoint: true);
        _endPeer = new MaterialRangeSliderThumbAutomationPeer(owner, startEndpoint: false);
        _children = [_startPeer, _endPeer];
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialRangeSlider Owner => (MaterialRangeSlider)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(MaterialRangeSlider);

    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? "Range slider" : name;
    }

    protected override bool IsContentElementCore() => false;

    protected override bool IsControlElementCore() => true;

    protected override IReadOnlyList<AutomationPeer> GetChildrenCore() => _children;

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialRangeSlider.StartValueProperty)
        {
            _startPeer.RaiseRangePropertyChanged(
                RangeValuePatternIdentifiers.ValueProperty,
                e.OldValue,
                e.NewValue);
            _endPeer.RaiseRangePropertyChanged(
                RangeValuePatternIdentifiers.MinimumProperty,
                e.OldValue,
                e.NewValue);
            _startPeer.RaiseBoundsChanged();
        }
        else if (e.Property == MaterialRangeSlider.EndValueProperty)
        {
            _endPeer.RaiseRangePropertyChanged(
                RangeValuePatternIdentifiers.ValueProperty,
                e.OldValue,
                e.NewValue);
            _startPeer.RaiseRangePropertyChanged(
                RangeValuePatternIdentifiers.MaximumProperty,
                e.OldValue,
                e.NewValue);
            _endPeer.RaiseBoundsChanged();
        }
        else if (e.Property == MaterialRangeSlider.MinimumProperty)
        {
            _startPeer.RaiseRangePropertyChanged(
                RangeValuePatternIdentifiers.MinimumProperty,
                e.OldValue,
                e.NewValue);
            RaiseAllBoundsChanged();
        }
        else if (e.Property == MaterialRangeSlider.MaximumProperty)
        {
            _endPeer.RaiseRangePropertyChanged(
                RangeValuePatternIdentifiers.MaximumProperty,
                e.OldValue,
                e.NewValue);
            RaiseAllBoundsChanged();
        }
        else if (e.Property == Visual.BoundsProperty ||
                 e.Property == Visual.RenderTransformProperty ||
                 e.Property == Visual.RenderTransformOriginProperty ||
                 e.Property == Visual.FlowDirectionProperty)
        {
            RaiseAllBoundsChanged();
        }
    }

    private void RaiseAllBoundsChanged()
    {
        _startPeer.RaiseBoundsChanged();
        _endPeer.RaiseBoundsChanged();
    }
}

internal sealed class MaterialRangeSliderThumbAutomationPeer :
    ControlAutomationPeer,
    IRangeValueProvider
{
    private readonly bool _startEndpoint;

    public MaterialRangeSliderThumbAutomationPeer(
        MaterialRangeSlider owner,
        bool startEndpoint)
        : base(owner)
    {
        _startEndpoint = startEndpoint;
    }

    private new MaterialRangeSlider Owner => (MaterialRangeSlider)base.Owner;

    public bool IsReadOnly => false;

    public double Minimum => _startEndpoint ? Owner.Minimum : Owner.StartValue;

    public double Maximum => _startEndpoint ? Owner.EndValue : Owner.Maximum;

    public double Value => _startEndpoint ? Owner.StartValue : Owner.EndValue;

    public double SmallChange => Owner.SmallChange;

    public double LargeChange => Owner.LargeChange;

    public void SetValue(double value)
    {
        EnsureEnabled();
        if (double.IsNaN(value) || value < Minimum || value > Maximum)
            throw new ArgumentOutOfRangeException(nameof(value));

        if (_startEndpoint)
            Owner.StartValue = value;
        else
            Owner.EndValue = value;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Slider;

    protected override string GetClassNameCore() => "MaterialRangeSliderThumb";

    protected override string? GetAutomationIdCore() =>
        _startEndpoint ? "StartRange" : "EndRange";

    protected override string? GetNameCore() =>
        _startEndpoint ? "Start range" : "End range";

    protected override Rect GetBoundingRectangleCore()
    {
        var bounds = Owner.GetThumbBounds(_startEndpoint);
        var root = TopLevel.GetTopLevel(Owner);
        if (root is not Visual rootVisual)
            return default;

        var transform = Owner.TransformToVisual(rootVisual);
        return transform.HasValue ? bounds.TransformToAABB(transform.Value) : default;
    }

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

    protected override bool HasKeyboardFocusCore() =>
        Owner.IsFocused && Owner.IsStartEndpointActive == _startEndpoint;

    protected override bool IsKeyboardFocusableCore() => Owner.Focusable;

    protected override void SetFocusCore()
    {
        EnsureEnabled();
        Owner.FocusEndpoint(_startEndpoint);
    }

    internal void RaiseRangePropertyChanged(
        AutomationProperty property,
        object? oldValue,
        object? newValue) =>
        RaisePropertyChangedEvent(property, oldValue, newValue);

    internal void RaiseBoundsChanged() =>
        RaisePropertyChangedEvent(
            AutomationElementIdentifiers.BoundingRectangleProperty,
            null,
            GetBoundingRectangle());
}
