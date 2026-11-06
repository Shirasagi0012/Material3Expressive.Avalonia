using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using ComposeAnimation;

namespace Material3Expressive.Components;

public partial class HorizontalCarousel
{
    protected override AutomationPeer OnCreateAutomationPeer() => new CarouselAutomationPeer(this);

    private sealed class CarouselAutomationPeer : ItemsControlAutomationPeer, IScrollProvider
    {
        private readonly HorizontalCarousel _owner;
        private bool _canScroll;
        private double _percent, _viewSize;
        public CarouselAutomationPeer(HorizontalCarousel owner) : base(owner)
        {
            _owner = owner;
            _canScroll = HorizontallyScrollable; _percent = HorizontalScrollPercent; _viewSize = HorizontalViewSize;
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property != ScrollOffsetProperty && change.Property != MaximumScrollOffsetProperty &&
                    change.Property != BoundsProperty && change.Property != UserScrollEnabledProperty && change.Property != IsEffectivelyEnabledProperty) return;
                var canScroll = HorizontallyScrollable; var percent = HorizontalScrollPercent; var view = HorizontalViewSize;
                if (_canScroll != canScroll) RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontallyScrollableProperty, _canScroll, canScroll);
                if (_percent != percent) RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontalScrollPercentProperty, _percent, percent);
                if (_viewSize != view) RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontalViewSizeProperty, _viewSize, view);
                _canScroll = canScroll; _percent = percent; _viewSize = view;
            };
        }
        protected override string GetClassNameCore() => nameof(HorizontalCarousel);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
        protected override object? GetProviderCore(Type type) => type == typeof(IScrollProvider)
            ? _owner.UserScrollEnabled ? this : null : base.GetProviderCore(type);
        // The base peer delegates scrolling to a ScrollViewer; this ItemsControl owns a keyline panel.
        public new bool HorizontallyScrollable => _owner.UserScrollEnabled && _owner.IsEffectivelyEnabled && _owner.MaximumScrollOffset > 0;
        public new bool VerticallyScrollable => false;
        public new double HorizontalScrollPercent => HorizontallyScrollable
            ? Math.Clamp(_owner.ScrollOffset / _owner.MaximumScrollOffset * 100, 0, 100) : ScrollPatternIdentifiers.NoScroll;
        public new double VerticalScrollPercent => ScrollPatternIdentifiers.NoScroll;
        public new double HorizontalViewSize => _owner.MaximumScrollOffset <= 0 ? 100
            : _owner.Bounds.Width / (_owner.Bounds.Width + _owner.MaximumScrollOffset) * 100;
        public new double VerticalViewSize => 100;

        public new void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            EnsureScrollingEnabled();
            if (verticalAmount != ScrollAmount.NoAmount) throw new InvalidOperationException("The carousel has no vertical scroll axis.");
            var direction = horizontalAmount switch
            {
                ScrollAmount.NoAmount => 0,
                ScrollAmount.SmallIncrement or ScrollAmount.LargeIncrement => 1,
                ScrollAmount.SmallDecrement or ScrollAmount.LargeDecrement => -1,
                _ => throw new ArgumentOutOfRangeException(nameof(horizontalAmount))
            };
            var index = _owner.CurrentItem + direction;
            if (direction == 0 || index < 0 || index >= _owner.ItemCount || !HorizontallyScrollable) return;
            // Both UIA increments map to the fixed Pager's discrete pageLeft/pageRight actions.
            _ = AnimationScope.Get(_owner).Launch(token => _owner.AnimateScroll(index, null, token, usePagerDistance: true), "Carousel accessibility page");
        }
        public new void SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            EnsureScrollingEnabled();
            if (verticalPercent != ScrollPatternIdentifiers.NoScroll) throw new InvalidOperationException("The carousel has no vertical scroll axis.");
            if (horizontalPercent == ScrollPatternIdentifiers.NoScroll) return;
            if (!double.IsFinite(horizontalPercent) || horizontalPercent < 0 || horizontalPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(horizontalPercent));
            if (!HorizontallyScrollable) throw new InvalidOperationException("The carousel has no horizontal scroll range.");
            _owner.CancelPointer(false); _owner.CancelScroll();
            _owner.SetCurrentValue(ScrollOffsetProperty, _owner.MaximumScrollOffset * horizontalPercent / 100);
        }
        private void EnsureScrollingEnabled()
        {
            EnsureEnabled();
            if (!_owner.UserScrollEnabled) throw new InvalidOperationException("User scrolling is disabled.");
        }
    }
}
