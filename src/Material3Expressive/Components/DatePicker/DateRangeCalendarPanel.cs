using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

// DateRangePickerContent.onPreviewKeyEvent skips the whole month list on Tab.
// The native custom navigation contract searches outside this group when the
// result is (true, null); entry and arrow navigation keep their native traversal.
internal sealed class DateRangeCalendarPanel : Grid, ICustomKeyboardNavigation
{
    protected override Type StyleKeyOverride => typeof(Grid);

    public (bool handled, IInputElement? next) GetNext(IInputElement element, NavigationDirection direction) =>
        direction is NavigationDirection.Next or NavigationDirection.Previous &&
        element is Visual visual && this.IsVisualAncestorOf(visual)
            ? (true, null) : (false, null);
}
