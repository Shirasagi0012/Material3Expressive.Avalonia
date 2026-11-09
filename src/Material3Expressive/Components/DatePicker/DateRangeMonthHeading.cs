// DateRangePicker.kt / customScrollActions at the pinned AndroidX baseline, Apache-2.0.
using System.Globalization;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Material3Expressive.Components;

internal sealed class DateRangeMonthHeading(Func<int, bool> scroll, Func<int, bool> canScroll,
    Func<CultureInfo> culture) : TextBlock
{
    protected override Type StyleKeyOverride => typeof(TextBlock);
    protected override AutomationPeer OnCreateAutomationPeer() => new HeadingPeer(this);

    private sealed class HeadingPeer : TextBlockAutomationPeer
    {
        private readonly ActionPeer[] _actions;
        public HeadingPeer(DateRangeMonthHeading owner) : base(owner) =>
            _actions = [new(this, owner, -1), new(this, owner, 1)];
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => _actions;
    }

    // UIA has no named custom-action list. Preserve both named operations as virtual
    // Invoke children, without adding focus stops or controls to the calendar layout.
    private sealed class ActionPeer(AutomationPeer parent, DateRangeMonthHeading owner, int direction)
        : ControlAutomationPeer(owner), IInvokeProvider
    {
        public void Invoke() => owner.Scroll(direction);
        protected override AutomationPeer? GetParentCore() => parent;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [];
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
        protected override string GetClassNameCore() => "DateRangeMonthAction";
        protected override string? GetAutomationIdCore() => direction < 0 ? "DateRangePicker.PreviousMonth" : "DateRangePicker.NextMonth";
        protected override string? GetNameCore() => owner.ActionName(direction);
        protected override bool IsEnabledCore() => owner.CanScroll(direction);
        protected override bool IsKeyboardFocusableCore() => false;
        protected override bool HasKeyboardFocusCore() => false;
        protected override void SetFocusCore() { }
    }

    private void Scroll(int direction) => scroll(direction);
    private bool CanScroll(int direction) => canScroll(direction);
    private string ActionName(int direction) => DateTimeStrings.Get(direction < 0
        ? "DateRangePickerScrollToShowPreviousMonth" : "DateRangePickerScrollToShowNextMonth", culture());
}
