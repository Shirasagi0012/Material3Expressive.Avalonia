using Avalonia.Controls;

namespace Material3Expressive.Components;

// FloatingToolbarDefaults.VerticalNestedScrollExpansionNode, in DIP. The application
// owns Expanded. An external change is observed before the next consumed scroll.
internal sealed class FloatingToolbarExpansionPolicy
{
    private readonly Func<bool> _readExpanded;
    private readonly Action _onExpand, _onCollapse;
    private readonly bool _reverse;
    private readonly float _expandThreshold, _collapseThreshold;
    private bool _expanded;
    private float _contentOffset, _threshold;
    internal FloatingToolbarExpansionPolicy(Func<bool> expanded, Action onExpand, Action onCollapse,
        bool reverse, float expandThreshold, float collapseThreshold)
    {
        _readExpanded = expanded; _onExpand = onExpand; _onCollapse = onCollapse;
        _reverse = reverse; _expandThreshold = expandThreshold; _collapseThreshold = collapseThreshold;
        _expanded = expanded(); _threshold = _expanded ? -collapseThreshold : expandThreshold;
    }
    internal void OnPostScroll(float consumedY)
    {
        var expanded = _readExpanded();
        if (_expanded != expanded)
        {
            _expanded = expanded;
            _threshold = _expanded ? _contentOffset - _collapseThreshold : _contentOffset + _expandThreshold;
        }
        var delta = consumedY * (_reverse ? -1 : 1);
        _contentOffset += delta;
        if (delta < 0 && _contentOffset <= _threshold) { _threshold = _contentOffset + _expandThreshold; _onCollapse(); }
        else if (delta > 0 && _contentOffset >= _threshold) { _threshold = _contentOffset - _collapseThreshold; _onExpand(); }
        // A synchronous application update is the equivalent of the node's next
        // recomposition. Its threshold is already the one selected above.
        _expanded = _readExpanded();
    }
}

internal sealed class FloatingToolbarExpansionSubscription : IDisposable
{
    private ScrollViewer? _source;
    private readonly FloatingToolbarExpansionPolicy _policy;
    internal FloatingToolbarExpansionSubscription(ScrollViewer source, FloatingToolbarExpansionPolicy policy)
    { _source = source; _policy = policy; source.ScrollChanged += Changed; }
    private void Changed(object? sender, ScrollChangedEventArgs e) => _policy.OnPostScroll(-(float)e.OffsetDelta.Y);
    public void Dispose() { if (_source is null) return; _source.ScrollChanged -= Changed; _source = null; }
}
