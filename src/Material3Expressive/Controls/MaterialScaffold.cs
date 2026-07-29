// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Scaffold.kt:87-380.
// Scaffold owns five layout slots, reports caller-applied content padding, positions the FAB and
// snackbar inside system-bar insets, and gives content/top/snackbar/bottom/FAB z-indices 0/1/2/3/4.
// The pinned Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack Material3 1.5.0-alpha22
// executable reference has the same public slots and geometry but relies on placement order rather
// than alpha25's explicit z-indices. Avalonia exposes WindowInsets as nullable Thickness: null
// follows the current TopLevel safe area, while any explicit Thickness (including zero) overrides it.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;

namespace Material3Expressive.Controls;

/// <summary>Positions the primary regions of a Material application screen.</summary>
/// <remarks>
/// <para>
/// Like Compose Scaffold, the body content fills the complete scaffold and is not padded
/// automatically. Bind or otherwise apply <see cref="ContentPadding" /> to the body content. For a
/// scrolling body, apply it to the scrolling child rather than to the <see cref="ScrollViewer" />.
/// </para>
/// <para>
/// When <see cref="ContentWindowInsets" /> is <see langword="null" />, the scaffold observes its
/// containing <see cref="TopLevel" /> safe area. Set an explicit value to replace that source; use
/// zero when an ancestor already consumed the safe area.
/// </para>
/// </remarks>
public sealed class MaterialScaffold : ContentControl
{
    /// <summary>Defines the content displayed at the top edge.</summary>
    public static readonly StyledProperty<object?> TopBarProperty =
        AvaloniaProperty.Register<MaterialScaffold, object?>(nameof(TopBar));

    /// <summary>Defines the content displayed at the bottom edge.</summary>
    public static readonly StyledProperty<object?> BottomBarProperty =
        AvaloniaProperty.Register<MaterialScaffold, object?>(nameof(BottomBar));

    /// <summary>Defines the snackbar host displayed above body content.</summary>
    public static readonly StyledProperty<object?> SnackbarHostProperty =
        AvaloniaProperty.Register<MaterialScaffold, object?>(nameof(SnackbarHost));

    /// <summary>Defines the floating action button displayed above body content.</summary>
    public static readonly StyledProperty<object?> FloatingActionButtonProperty =
        AvaloniaProperty.Register<MaterialScaffold, object?>(nameof(FloatingActionButton));

    /// <summary>Defines the logical horizontal position of the floating action button.</summary>
    public static readonly StyledProperty<ScaffoldFabPosition> FloatingActionButtonPositionProperty =
        AvaloniaProperty.Register<MaterialScaffold, ScaffoldFabPosition>(
            nameof(FloatingActionButtonPosition),
            ScaffoldFabPosition.End,
            validate: static value => Enum.IsDefined(value));

    /// <summary>
    /// Defines the physical window insets used by content, snackbar, and FAB layout. A null value
    /// observes the containing TopLevel safe area.
    /// </summary>
    public static readonly StyledProperty<Thickness?> ContentWindowInsetsProperty =
        AvaloniaProperty.Register<MaterialScaffold, Thickness?>(
            nameof(ContentWindowInsets),
            validate: static value => value is null || IsValidInsets(value.Value));

    /// <summary>Defines the caller-applied padding calculated from bars and window insets.</summary>
    public static readonly DirectProperty<MaterialScaffold, Thickness> ContentPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialScaffold, Thickness>(
            nameof(ContentPadding),
            scaffold => scaffold.ContentPadding);

    private IInsetsManager? _insetsManager;
    private Thickness _safeAreaPadding;
    private Thickness _contentPadding;

    static MaterialScaffold()
    {
        TopLevel.AutoSafeAreaPaddingProperty.OverrideDefaultValue<MaterialScaffold>(false);
        AffectsMeasure<MaterialScaffold>(
            TopBarProperty,
            BottomBarProperty,
            SnackbarHostProperty,
            FloatingActionButtonProperty,
            FloatingActionButtonPositionProperty,
            ContentWindowInsetsProperty,
            FlowDirectionProperty);
    }

    /// <summary>Gets or sets the content displayed at the top edge.</summary>
    public object? TopBar
    {
        get => GetValue(TopBarProperty);
        set => SetValue(TopBarProperty, value);
    }

    /// <summary>Gets or sets the content displayed at the bottom edge.</summary>
    public object? BottomBar
    {
        get => GetValue(BottomBarProperty);
        set => SetValue(BottomBarProperty, value);
    }

    /// <summary>
    /// Gets or sets the snackbar host. The scaffold positions this slot but does not own snackbar
    /// queue or timeout state.
    /// </summary>
    public object? SnackbarHost
    {
        get => GetValue(SnackbarHostProperty);
        set => SetValue(SnackbarHostProperty, value);
    }

    /// <summary>Gets or sets the floating action button.</summary>
    public object? FloatingActionButton
    {
        get => GetValue(FloatingActionButtonProperty);
        set => SetValue(FloatingActionButtonProperty, value);
    }

    /// <summary>Gets or sets the logical horizontal position of the floating action button.</summary>
    public ScaffoldFabPosition FloatingActionButtonPosition
    {
        get => GetValue(FloatingActionButtonPositionProperty);
        set => SetValue(FloatingActionButtonPositionProperty, value);
    }

    /// <summary>
    /// Gets or sets the physical window insets used by this scaffold. Null follows the current
    /// TopLevel safe area; an explicit value, including zero, overrides that source.
    /// </summary>
    public Thickness? ContentWindowInsets
    {
        get => GetValue(ContentWindowInsetsProperty);
        set => SetValue(ContentWindowInsetsProperty, value);
    }

    /// <summary>
    /// Gets the padding applications should apply to body content. Top and bottom bars replace
    /// their corresponding window inset. Horizontal values use Avalonia's logical layout
    /// coordinates so applying the Thickness inside the same FlowDirection preserves the resolved
    /// physical left and right insets.
    /// </summary>
    public Thickness ContentPadding => _contentPadding;

    internal Thickness ResolvedContentWindowInsets => ContentWindowInsets ?? _safeAreaPadding;

    internal void SetContentPadding(Thickness value)
    {
        if (_contentPadding == value)
            return;

        SetAndRaise(ContentPaddingProperty, ref _contentPadding, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ObserveSafeArea();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopObservingSafeArea();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentWindowInsetsProperty ||
            change.Property == FloatingActionButtonPositionProperty ||
            change.Property == FlowDirectionProperty)
        {
            InvalidateScaffoldLayout();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialScaffoldAutomationPeer(this);

    private void ObserveSafeArea()
    {
        StopObservingSafeArea();
        _insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insetsManager is null)
        {
            UpdateSafeArea(default);
            return;
        }

        _insetsManager.SafeAreaChanged += OnSafeAreaChanged;
        UpdateSafeArea(_insetsManager.SafeAreaPadding);
    }

    private void StopObservingSafeArea()
    {
        if (_insetsManager is not null)
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;

        _insetsManager = null;
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) =>
        UpdateSafeArea(e.SafeAreaPadding);

    private void UpdateSafeArea(Thickness value)
    {
        if (_safeAreaPadding == value)
            return;

        _safeAreaPadding = value;
        if (ContentWindowInsets is null)
            InvalidateScaffoldLayout();
    }

    private void InvalidateScaffoldLayout()
    {
        InvalidateMeasure();
        this.GetVisualDescendants()
            .OfType<MaterialScaffoldPanel>()
            .FirstOrDefault()
            ?.InvalidateMeasure();
    }

    private static bool IsValidInsets(Thickness value) =>
        IsValidInset(value.Left) &&
        IsValidInset(value.Top) &&
        IsValidInset(value.Right) &&
        IsValidInset(value.Bottom);

    private static bool IsValidInset(double value) => double.IsFinite(value) && value >= 0;
}

/// <summary>Logical placement options for a Scaffold floating action button.</summary>
public enum ScaffoldFabPosition
{
    /// <summary>Place the FAB at the logical start above a bottom bar when present.</summary>
    Start,

    /// <summary>Place the FAB in the horizontal center above a bottom bar when present.</summary>
    Center,

    /// <summary>Place the FAB at the logical end above a bottom bar when present.</summary>
    End,

    /// <summary>Place the FAB at the logical end over the bottom bar region.</summary>
    EndOverlay
}
