// InteractiveListItemMeasurePolicy, AndroidX f3ed195f0c9e; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

internal sealed class ListItemLayout : Panel
{
    private ListItem? _owner;
    private readonly Size[] _sizes = new Size[5];
    private ContentPresenter Slot(int index) => (ContentPresenter)Children[index];
    internal void SetOwner(ListItem? owner) { _owner = owner; Refresh(); }
    internal void Refresh()
    {
        if (_owner is not { } owner || Children.Count != 5) return;
        for (var i = 0; i < 5; i++)
        {
            var slot = Slot(i);
            slot.IsVisible = i == 4 || slot.Content is not null || slot.ContentTemplate is not null;
            if (i < 2)
            {
                slot.Padding = i == 0 ? new Thickness(0, 0, owner.InternalSpacing, 0) : new Thickness(owner.InternalSpacing, 0, 0, 0);
                MaterialInteractiveSize.SetMinimum(slot, Math.Max(0, MaterialInteractiveSize.Resolve(owner)
                    - (i == 0 ? owner.Padding.Left : owner.Padding.Right) - owner.InternalSpacing));
            }
            MaterialText.SetStyle(slot, i switch
            {
                0 => owner.LeadingTextStyle, 1 => owner.TrailingTextStyle, 2 => owner.OverlineTextStyle,
                3 => owner.SupportingTextStyle, _ => owner.ContentTextStyle,
            });
        }
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_owner is not { } owner || Children.Count != 5) return default;
        RefreshSlots();
        var scale = LayoutHelper.GetLayoutScale(this);
        double Px(double value) => double.IsFinite(value) ? MathUtils.DipToPx(value, scale) / scale : value;
        var width = Px(availableSize.Width);
        var height = Px(availableSize.Height);
        void Measure(int index, double w, double h)
        {
            var slot = Slot(index);
            slot.Measure(new Size(Math.Max(0, w), Math.Max(0, h)));
            _sizes[index] = slot.IsVisible ? new Size(Px(slot.DesiredSize.Width), Px(slot.DesiredSize.Height)) : default;
        }
        Measure(0, width, height);
        Measure(1, width - _sizes[0].Width, height);
        var remaining = Math.Max(0, width - _sizes[0].Width - _sizes[1].Width);
        Measure(2, remaining, height);
        Measure(4, remaining, height - _sizes[2].Height);
        Measure(3, remaining, height - _sizes[2].Height - _sizes[4].Height);
        var supporting = Slot(3);
        var (first, last) = Baselines(supporting);
        var three = Slot(2).IsVisible && supporting.IsVisible || supporting.IsVisible && first != last;
        var defaultHeight = three ? owner.ThreeLineHeight : Slot(2).IsVisible || supporting.IsVisible ? owner.TwoLineHeight : owner.OneLineHeight;
        var verticalPadding = owner.Padding.Top + owner.Padding.Bottom;
        var explicitMinimum = Math.Max(0, owner.MinHeight - verticalPadding);
        var minimum = explicitMinimum > 0 ? explicitMinimum : Math.Max(0, defaultHeight - verticalPadding);
        var desiredHeight = Math.Max(_sizes[0].Height, Math.Max(_sizes[1].Height, _sizes[2].Height + _sizes[3].Height + _sizes[4].Height));
        return new Size(double.IsFinite(width) ? width : _sizes[0].Width + _sizes[1].Width + Math.Max(_sizes[4].Width, Math.Max(_sizes[2].Width, _sizes[3].Width)),
            Math.Min(height, Math.Max(Px(minimum), desiredHeight)));
    }
    private void RefreshSlots()
    {
        // Presenter content bindings may update after the owner's property callback.
        for (var i = 0; i < 5; i++)
        {
            var slot = Slot(i);
            slot.IsVisible = i == 4 || slot.Content is not null || slot.ContentTemplate is not null;
            slot.UpdateChild();
        }
    }
    private static (double First, double Last) Baselines(ContentPresenter presenter)
    {
        if (presenter.Child is not { } child) return (double.NegativeInfinity, double.NegativeInfinity);
        var controls = new[] { child }.Concat(child.GetVisualDescendants().OfType<Control>());
        foreach (var control in controls)
        {
            if (control.IsSet(TextBlock.BaselineOffsetProperty))
                return (TextBlock.GetBaselineOffset(control), TextBlock.GetBaselineOffset(control));
            if (control is TextBlock { TextLayout: { } text } && text.TextLines.Count > 0)
                return (text.TextLines[0].Baseline, text.TextLines.Take(text.TextLines.Count - 1).Sum(line => line.Height) + text.TextLines[^1].Baseline);
        }
        return (double.NegativeInfinity, double.NegativeInfinity);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner is not { } owner || Children.Count != 5) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Y(double height)
        {
            var alignment = owner.ItemVerticalAlignment;
            if (alignment == ListItemVerticalAlignment.Auto)
                alignment = MathUtils.DipToPx(finalSize.Height, scale) < MathUtils.DipToPx(owner.VerticalAlignmentBreakpoint, scale)
                    ? ListItemVerticalAlignment.Center : ListItemVerticalAlignment.Top;
            return alignment == ListItemVerticalAlignment.Center ? MathUtils.RoundToInt((finalSize.Height - height) * scale / 2) / scale
                : alignment == ListItemVerticalAlignment.Bottom ? finalSize.Height - height : 0;
        }
        void Place(int i, double x, double y) => Slot(i).Arrange(new Rect(x, y, _sizes[i].Width, _sizes[i].Height));
        Place(0, 0, Y(_sizes[0].Height));
        Place(1, finalSize.Width - _sizes[1].Width, Y(_sizes[1].Height));
        var contentX = _sizes[0].Width;
        var contentY = Y(_sizes[2].Height + _sizes[4].Height + _sizes[3].Height);
        Place(2, contentX, contentY);
        Place(4, contentX, contentY + _sizes[2].Height);
        Place(3, contentX, contentY + _sizes[2].Height + _sizes[4].Height);
        owner.LayoutChanged();
        return finalSize;
    }
}
