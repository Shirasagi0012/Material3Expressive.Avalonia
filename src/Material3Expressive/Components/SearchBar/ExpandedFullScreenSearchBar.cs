using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

/// <summary>Expands the paired SearchBar into a search surface covering the window's client area.</summary>
/// <remarks>Uses the same application-owned State and content slots as the docked presentation.</remarks>
public class ExpandedFullScreenSearchBar : ExpandedDockedSearchBar
{
    public static readonly StyledProperty<bool> DismissOnEscapeProperty = Dialog.DismissOnEscapeProperty.AddOwner<ExpandedFullScreenSearchBar>();
    public static readonly StyledProperty<bool> DismissOnBackProperty = Dialog.DismissOnBackProperty.AddOwner<ExpandedFullScreenSearchBar>();
    /// <summary>Whether an unhandled Escape requests collapse. Defaults to true.</summary>
    public bool DismissOnEscape { get => GetValue(DismissOnEscapeProperty); set => SetValue(DismissOnEscapeProperty, value); }
    /// <summary>Whether a platform Back request collapses the search dialog. Defaults to true.</summary>
    public bool DismissOnBack { get => GetValue(DismissOnBackProperty); set => SetValue(DismissOnBackProperty, value); }
    private protected override bool AllowsDismissal(DismissReason reason) => reason switch
    {
        DismissReason.Escape => DismissOnEscape,
        DismissReason.Back => DismissOnBack,
        _ => base.AllowsDismissal(reason),
    };
    /// <summary>Physical left/top/right/bottom insets, excluding any already consumed by ancestors.</summary>
    public static readonly StyledProperty<Thickness> WindowInsetsProperty =
        AvaloniaProperty.Register<ExpandedFullScreenSearchBar, Thickness>(nameof(WindowInsets));
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    protected override Type StyleKeyOverride => typeof(ExpandedFullScreenSearchBar);
    private FullScreenSearchBarLayout? _layout;
    internal override void UpdateVariant(SearchBarPopupPresenter presenter, SearchBarState state) => _layout?.InvalidateMeasure();
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _layout = e.NameScope.Find<FullScreenSearchBarLayout>("PART_Layout");
        base.OnApplyTemplate(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowInsetsProperty || change.Property == CornerRadiusProperty ||
            change.Property == MaterialWindowInsets.ConsumedPaddingProperty || change.Property == FlowDirectionProperty)
            _layout?.InvalidateMeasure();
    }
}
