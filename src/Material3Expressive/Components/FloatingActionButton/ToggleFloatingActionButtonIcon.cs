// FloatingActionButtonMenu.kt / ToggleFloatingActionButtonDefaults.animateIcon;
// fixed AndroidX f3ed195f0c9e and Core a9e47e8aa04c, Apache-2.0.
using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>An image icon whose fixed size and whole-image tint follow its containing Toggle FAB.</summary>
/// <remarks>Use Source for bitmap, vector or custom IImage content. This does not tint an arbitrary control subtree.</remarks>
public sealed class ToggleFloatingActionButtonIcon : MaterialIcon
{
    public static readonly StyledProperty<Func<float, double>?> SizeProperty =
        AvaloniaProperty.Register<ToggleFloatingActionButtonIcon, Func<float, double>?>(nameof(Size));
    public static readonly StyledProperty<Func<float, Color>?> ColorProperty =
        AvaloniaProperty.Register<ToggleFloatingActionButtonIcon, Func<float, Color>?>(nameof(Color));

    /// <summary>Size in DIP for the current checked progress. Null consumes the containing Toggle FAB's themed icon size.</summary>
    public Func<float, double>? Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    /// <summary>Whole-image SrcIn tint for the current checked progress. Null consumes the containing Toggle FAB's themed icon color.</summary>
    public Func<float, Color>? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    private ToggleFloatingActionButton? _owner;
    private IDisposable? _width, _height, _foreground, _tint;
    private double _appliedSize = double.NaN;
    private Color? _appliedColor;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<ToggleFloatingActionButton>();
        if (_owner is null) return;
        _owner.PropertyChanged += OwnerChanged;
        LayoutUpdated += UpdateAfterLayout;
        _tint = SetValue(IsTintEnabledProperty, true, BindingPriority.Animation);
        UpdateAppearance();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is { } owner) owner.PropertyChanged -= OwnerChanged;
        _owner = null;
        LayoutUpdated -= UpdateAfterLayout;
        _width?.Dispose(); _height?.Dispose(); _foreground?.Dispose(); _tint?.Dispose();
        _width = _height = _foreground = _tint = null;
        _appliedSize = double.NaN; _appliedColor = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeProperty || change.Property == ColorProperty) UpdateAppearance();
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ToggleFloatingActionButton.CheckedProgressProperty ||
            change.Property == ToggleFloatingActionButton.IconSizeProperty ||
            change.Property == ToggleFloatingActionButton.IconColorProperty ||
            change.Property == ToggleFloatingActionButton.IconSizeFunctionProperty ||
            change.Property == ToggleFloatingActionButton.IconColorFunctionProperty)
            UpdateAppearance();
    }

    // Reevaluate physical rounding when a live window changes density; no frame polling.
    private void UpdateAfterLayout(object? sender, EventArgs e) => UpdateAppearance();

    private void UpdateAppearance()
    {
        if (_owner is not { } owner) return;
        var sizeFunction = Size ?? owner.IconSizeFunction;
        var colorFunction = Color ?? owner.IconColorFunction;
        if (sizeFunction is null || colorFunction is null) return;
        var progress = owner.CheckedProgress;
        var size = sizeFunction(progress);
        if (!double.IsFinite(size) || size < 0) throw new ArgumentOutOfRangeException(nameof(Size), "Icon size must be finite and nonnegative.");
        var density = LayoutHelper.GetLayoutScale(this);
        size = MathUtils.PxToDip(MathUtils.DipToPx((float)size, (float)density), density);
        var color = colorFunction(progress);
        if (_appliedSize != size)
        {
            _width?.Dispose(); _height?.Dispose();
            _width = SetValue(WidthProperty, size, BindingPriority.Animation);
            _height = SetValue(HeightProperty, size, BindingPriority.Animation);
            _appliedSize = size;
        }
        if (_appliedColor != color)
        {
            _foreground?.Dispose();
            _foreground = SetValue(ForegroundProperty, new SolidColorBrush(color), BindingPriority.Animation);
            _appliedColor = color;
        }
    }
}
