// LegacyOneRowSnackbar / LegacyNewLineButtonSnackbar from the fixed Snackbar.kt (Apache-2.0).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class SnackbarLayout : Panel
{
    public static readonly StyledProperty<double> SingleLineContainerHeightProperty = AvaloniaProperty.Register<SnackbarLayout, double>(nameof(SingleLineContainerHeight));
    public double SingleLineContainerHeight { get => GetValue(SingleLineContainerHeightProperty); set => SetValue(SingleLineContainerHeightProperty, value); }
    public static readonly StyledProperty<double> TwoLinesContainerHeightProperty = AvaloniaProperty.Register<SnackbarLayout, double>(nameof(TwoLinesContainerHeight));
    public double TwoLinesContainerHeight { get => GetValue(TwoLinesContainerHeightProperty); set => SetValue(TwoLinesContainerHeightProperty, value); }

    public static readonly StyledProperty<bool> ActionOnNewLineProperty = Snackbar.ActionOnNewLineProperty.AddOwner<SnackbarLayout>();
    public bool ActionOnNewLine { get => GetValue(ActionOnNewLineProperty); set => SetValue(ActionOnNewLineProperty, value); }
    private Size _layoutSize;
    private Rect _messageBox, _actionBox, _dismissBox;
    protected override bool BypassFlowDirectionPolicies => true;
    private Control Message => Children[0];
    private Control Action => Children[1];
    private Control Dismiss => Children[2];
    static SnackbarLayout() => AffectsMeasure<SnackbarLayout>(ActionOnNewLineProperty, SingleLineContainerHeightProperty, TwoLinesContainerHeightProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return default;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        int Max(double value) => double.IsFinite(value) ? Px(value) : int.MaxValue;
        var maxWidth = Max(availableSize.Width); var maxHeight = Max(availableSize.Height);
        var owner = TemplatedParent as Snackbar;
        var inset = owner is null ? default : owner.Padding + new Thickness(owner.Data is null ? 0 : 12);
        var minWidth = owner is null ? 0 : double.IsNaN(owner.Width) ? Math.Min(maxWidth, Px(Math.Max(0, owner.MinWidth - inset.Left - inset.Right))) : maxWidth;
        var minHeight = owner is null ? 0 : double.IsNaN(owner.Height) ? Math.Min(maxHeight, Px(Math.Max(0, owner.MinHeight - inset.Top - inset.Bottom))) : maxHeight;
        var start = Px(16); var end = Dismiss.IsVisible ? 0 : Px(8);

        if (ActionOnNewLine && Action.IsVisible)
        {
            // widthIn(max=600).fillMaxWidth(): incoming minimums precede widthIn.
            var width = Math.Max(minWidth, Math.Min(maxWidth, Px(600)));
            var innerWidth = Math.Max(0, width - start); var innerHeight = Math.Max(0, maxHeight - Px(2));
            Message.Measure(new Size(Math.Max(0, innerWidth - Px(8)) / scale, innerHeight / scale));
            var mw = Px(Message.DesiredSize.Width); var mh = Px(Message.DesiredSize.Height);
            var (first, last) = Baselines(Message, scale);
            // paddingFromBaseline applies the inner LastBaseline modifier, then FirstBaseline.
            var bottom = Math.Clamp(Px(12) - mh + (last == int.MinValue ? 0 : last), 0, innerHeight - mh);
            var top = Math.Clamp(Px(30) - (first == int.MinValue ? 0 : first), 0, innerHeight - mh - bottom);
            var textHeight = top + mh + bottom;
            var rowWidth = Math.Max(0, innerWidth - end); var rowHeight = Math.Max(0, innerHeight - textHeight);
            Action.Measure(new Size(rowWidth / scale, rowHeight / scale));
            var aw = Px(Action.DesiredSize.Width); var ah = Px(Action.DesiredSize.Height);
            Dismiss.Measure(new Size(Math.Max(0, rowWidth - aw) / scale, rowHeight / scale));
            var dw = Px(Dismiss.DesiredSize.Width); var dh = Px(Dismiss.DesiredSize.Height);
            var x = width - end - aw - dw;
            _messageBox = new(start, top, mw, mh);
            _actionBox = new(x, textHeight, aw, ah);
            _dismissBox = new(x + aw, textHeight, dw, dh); // Legacy Row uses Top, not CenterVertically.
            _layoutSize = new(width, Math.Max(minHeight, textHeight + Math.Max(ah, dh) + Px(2)));
        }
        else
        {
            // The padding modifier is outside Layout, so its maximum is capped after padding.
            var innerMax = Math.Max(0, maxWidth - start - end);
            var innerMin = Math.Max(0, minWidth - start - end);
            var width = Math.Min(innerMax, Px(600));
            Action.Measure(new Size(innerMax / scale, maxHeight / scale));
            Dismiss.Measure(new Size(innerMax / scale, maxHeight / scale));
            var aw = Action.IsVisible ? Math.Max(innerMin, Px(Action.DesiredSize.Width)) : 0;
            var ah = Action.IsVisible ? Math.Max(minHeight, Px(Action.DesiredSize.Height)) : 0;
            var dw = Dismiss.IsVisible ? Math.Max(innerMin, Px(Dismiss.DesiredSize.Width)) : 0;
            var dh = Dismiss.IsVisible ? Math.Max(minHeight, Px(Dismiss.DesiredSize.Height)) : 0;
            var textMax = Math.Max(innerMin, width - aw - dw - (dw == 0 ? Px(8) : 0));
            var padding = Px(6);
            Message.Measure(new Size(textMax / scale, Math.Max(0, maxHeight - 2 * padding) / scale));
            var mw = Math.Max(innerMin, Px(Message.DesiredSize.Width));
            var mh = Px(Message.DesiredSize.Height);
            var textHeight = Math.Min(maxHeight, mh + 2 * padding);
            var (first, last) = Baselines(Message, scale);
            if (first != int.MinValue) first += padding;
            if (last != int.MinValue) last += padding;
            var oneLine = first == last || first == int.MinValue || last == int.MinValue;
            int height, textY, actionY;
            if (oneLine)
            {
                height = Math.Max(Px(SingleLineContainerHeight), Math.Max(ah, dh));
                textY = (height - textHeight) / 2;
                var (actionFirst, _) = Baselines(Action, scale);
                actionY = actionFirst == int.MinValue ? 0 : unchecked(textY + first - actionFirst);
            }
            else
            {
                textY = Px(30) - first;
                height = Math.Max(Px(TwoLinesContainerHeight), textY + textHeight);
                actionY = (height - ah) / 2;
            }
            _messageBox = new(start, textY + padding, mw, mh);
            _actionBox = new(start + width - dw - aw, actionY, aw, ah);
            _dismissBox = new(start + width - dw, (height - dh) / 2, dw, dh);
            _layoutSize = new(start + width + end, height);
        }
        return new(_layoutSize.Width / scale, _layoutSize.Height / scale);

    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var dx = (MathUtils.DipToPx(finalSize.Width, scale) - (int)_layoutSize.Width) / 2;
        var dy = (MathUtils.DipToPx(finalSize.Height, scale) - (int)_layoutSize.Height) / 2;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        Place(Message, _messageBox); Place(Action, _actionBox); Place(Dismiss, _dismissBox);
        return finalSize;

        void Place(Control control, Rect rect)
        {
            var x = rtl ? _layoutSize.Width - rect.X - rect.Width : rect.X;
            control.Arrange(new Rect((x + dx) / scale, (rect.Y + dy) / scale, rect.Width / scale, rect.Height / scale));
        }
    }

    // Native paragraphs and the existing standard Button template are the supported automatic
    // alignment-line sources. General custom-container propagation/explicit overrides are backlog.
    private static (int First, int Last) Baselines(Control control, double scale)
    {
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        if (control is TextBlock text && text.TextLayout.TextLines is { Count: > 0 } lines)
        {
            var y = text.Margin.Top + text.Padding.Top;
            return (Px(y + lines[0].Baseline), Px(y + lines.Take(lines.Count - 1).Sum(line => line.Height) + lines[^1].Baseline));
        }
        if (control is ContentPresenter { Child: { } child } presenter)
        {
            var pair = Baselines(child, scale);
            return pair.First == int.MinValue ? pair : (pair.First + Px(presenter.Padding.Top), pair.Last + Px(presenter.Padding.Top));
        }
        if (control is Grid grid && grid.Children.Where(c => c.IsVisible).ToArray() is [var only]) return Baselines(only, scale);
        if (control is Button button && button.GetVisualDescendants().OfType<ButtonLayout>().FirstOrDefault() is { } layout
            && button.GetVisualDescendants().OfType<ButtonContentPresenter>().FirstOrDefault() is { Child: TextBlock } label)
        {
            var pair = Baselines(label, scale);
            var height = Px(button.DesiredSize.Height - button.Margin.Top - button.Margin.Bottom);
            var containerHeight = Px(layout.ContainerSize.Height);
            var interactiveHeight = Math.Max(height, Px(layout.MinimumInteractiveSize));
            var containerY = MathUtils.RoundToInt((interactiveHeight - containerHeight) / 2f) + (height - interactiveHeight) / 2;
            var contentY = label.VerticalContentAlignment switch
            {
                VerticalAlignment.Center => MathUtils.RoundToInt((containerHeight - Px(label.DesiredSize.Height)) / 2f),
                VerticalAlignment.Bottom => containerHeight - Px(label.DesiredSize.Height),
                _ => 0
            };
            var y = Px(button.Margin.Top) + containerY + contentY;
            return (pair.First + y, pair.Last + y);
        }
        return (int.MinValue, int.MinValue);
    }
}
