// SearchBar.kt / FullScreenSearchBarLayout, non-predictive desktop branch.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

internal sealed class FullScreenSearchBarLayout : ExpandedSearchBarLayout
{
    public static readonly StyledProperty<double> DefaultInputWidthProperty = AvaloniaProperty.Register<FullScreenSearchBarLayout, double>(nameof(DefaultInputWidth));
    public static readonly StyledProperty<double> DefaultInputHeightProperty = AvaloniaProperty.Register<FullScreenSearchBarLayout, double>(nameof(DefaultInputHeight));
    public static readonly StyledProperty<double> VerticalPaddingProperty = AvaloniaProperty.Register<FullScreenSearchBarLayout, double>(nameof(VerticalPadding));
    public static readonly StyledProperty<double> InputHorizontalPaddingProperty = AvaloniaProperty.Register<FullScreenSearchBarLayout, double>(nameof(InputHorizontalPadding));
    public static readonly StyledProperty<double> InputTopPaddingProperty = AvaloniaProperty.Register<FullScreenSearchBarLayout, double>(nameof(InputTopPadding));
    public double DefaultInputWidth { get => GetValue(DefaultInputWidthProperty); set => SetValue(DefaultInputWidthProperty, value); }
    public double DefaultInputHeight { get => GetValue(DefaultInputHeightProperty); set => SetValue(DefaultInputHeightProperty, value); }
    public double VerticalPadding { get => GetValue(VerticalPaddingProperty); set => SetValue(VerticalPaddingProperty, value); }
    public double InputHorizontalPadding { get => GetValue(InputHorizontalPaddingProperty); set => SetValue(InputHorizontalPaddingProperty, value); }
    public double InputTopPadding { get => GetValue(InputTopPaddingProperty); set => SetValue(InputTopPaddingProperty, value); }
    private Rect _surfaceBounds, _inputBounds, _contentBounds;
    private IDisposable? _shape, _alpha, _surfaceAlpha;
    protected override bool BypassFlowDirectionPolicies => true;
    static FullScreenSearchBarLayout() => AffectsMeasure<FullScreenSearchBarLayout>(DefaultInputWidthProperty, DefaultInputHeightProperty, VerticalPaddingProperty, InputHorizontalPaddingProperty, InputTopPaddingProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = (ExpandedFullScreenSearchBar)TemplatedParent!;
        var contained = owner is ExpandedFullScreenContainedSearchBar;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, scale);
        var progress = Math.Clamp(Inputs.Progress, 0, 1);
        var windowWidth = Px(availableSize.Width);
        var windowHeight = Px(availableSize.Height);
        var collapsed = Inputs.Collapsed ?? default;
        var collapsedWidth = Px(collapsed.Width == 0 ? DefaultInputWidth : collapsed.Width);
        var collapsedHeight = Px(collapsed.Height == 0 ? DefaultInputHeight : collapsed.Height);
        var width = contained ? windowWidth : Math.Clamp(MathUtils.LerpRoundToInt(collapsedWidth, windowWidth, progress), 0, windowWidth);
        var height = contained ? windowHeight : Math.Clamp(MathUtils.LerpRoundToInt(collapsedHeight, windowHeight, progress), 0, windowHeight);
        // Raw progress intentionally permits input-width overshoot while the surface uses clamped progress.
        var inputWidth = MathUtils.LerpRoundToInt(collapsedWidth, width - 2 * Px(InputHorizontalPadding), Inputs.Progress);
        var insets = owner.WindowInsets;
        var consumed = owner.GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
        var left = Math.Max(0, Px(insets.Left) - Px(consumed.Left));
        var top = Math.Max(0, Px(insets.Top) - Px(consumed.Top));
        var right = Math.Max(0, Px(insets.Right) - Px(consumed.Right));
        var bottom = Math.Max(0, Px(insets.Bottom) - Px(consumed.Bottom));
        var topPadding = MathUtils.LerpRoundToInt(0, top + Px(InputTopPadding), progress);
        var bottomPadding = contained ? Px(VerticalPadding) : MathUtils.LerpRoundToInt(0, Px(VerticalPadding), progress);
        var contentHeight = Math.Max(0, height - collapsedHeight - topPadding - bottomPadding);
        var surface = (MaterialSurface)Children[0];
        var input = (Border)Children[1];
        var content = (Border)Children[2];
        // Padding values are physical, but these descendants still use normal logical RTL layout.
        var rtl = owner.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
        input.Padding = new Thickness((rtl ? right : left) / scale, 0, (rtl ? left : right) / scale, 0);
        content.Padding = new Thickness((rtl ? right : left) / scale, 0, (rtl ? left : right) / scale, bottom / scale);
        MaterialWindowInsets.SetConsumePadding(this, new Thickness((rtl ? right : left) / scale, top / scale, (rtl ? left : right) / scale, bottom / scale));
        surface.Measure(new Size(width / scale, height / scale));
        input.Measure(new Size(inputWidth / scale, collapsedHeight / scale));
        content.Measure(new Size(width / scale, contentHeight / scale));
        var offsetX = contained ? 0 : MathUtils.LerpRoundToInt(Px(collapsed.X), 0, progress);
        var offsetY = MathUtils.LerpRoundToInt(Px(collapsed.Y), 0, progress);
        // IntRect.center truncates its integer coordinate before conversion to Float.
        var collapsedCenter = (Px(collapsed.X) + Px(collapsed.Right)) / 2;
        var currentCenter = MathUtils.Lerp((float)collapsedCenter, offsetX + width / 2f, Inputs.Progress);
        _surfaceBounds = new Rect(offsetX / scale, contained ? 0 : offsetY / scale, width / scale, height / scale);
        _inputBounds = new Rect(MathUtils.RoundToInt(currentCenter - inputWidth / 2f) / scale,
            (offsetY + topPadding) / scale, inputWidth / scale, collapsedHeight / scale);
        _contentBounds = new Rect(offsetX / scale, (offsetY + topPadding + collapsedHeight + bottomPadding) / scale,
            width / scale, content.DesiredSize.Height);
        // The default full shape is the repository's percentage-radius sentinel. Custom corners
        // switch at .5, as upstream only interpolates the identity CircleShape -> RectangleShape pair.
        var corners = owner.CornerRadius == new CornerRadius(1000)
            ? new CornerRadius(DefaultInputHeight / 2 * (1 - progress))
            : progress < .5f ? owner.CornerRadius : default;
        _shape?.Dispose(); _shape = surface.SetValue(MaterialSurface.CornerRadiusProperty, corners, BindingPriority.Animation);
        _alpha?.Dispose(); _alpha = content.SetValue(OpacityProperty, (double)(contained ? owner.State?.ContentProgress ?? 0 : progress), BindingPriority.Animation);
        _surfaceAlpha?.Dispose(); _surfaceAlpha = surface.SetValue(OpacityProperty, contained ? (double)progress : 1d, BindingPriority.Animation);
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Children[0].Arrange(_surfaceBounds);
        Children[1].Arrange(_inputBounds);
        Children[2].Arrange(_contentBounds);
        return finalSize;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _shape?.Dispose(); _shape = null; _alpha?.Dispose(); _alpha = null;
        _surfaceAlpha?.Dispose(); _surfaceAlpha = null;
        base.OnDetachedFromVisualTree(e);
    }
}
