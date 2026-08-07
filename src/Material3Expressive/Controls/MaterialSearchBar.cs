using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 search bar that opens into a results surface.
/// </summary>
/// <remarks>
/// The bar and the results are one control because they are one shape: a pill that squares its
/// corners and grows downwards as it opens. Two controls would have to hand that transition
/// between them.
/// <para>
/// The shape morph is the same <see cref="AnimatedShape" /> the buttons use, so opening and
/// closing quickly reverses rather than restarting.
/// </para>
/// </remarks>
[TemplatePart(TextBoxPartName, typeof(TextBox))]
public class MaterialSearchBar : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.IsDark ?? false;
    private MaterialMotionScope Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    private void OnMotionTick() => InvalidateVisual();

    private const string TextBoxPartName = "PART_TextBox";

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MaterialSearchBar, string?>(
            nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="Placeholder" />
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MaterialSearchBar, string?>(nameof(Placeholder), "Search");

    /// <inheritdoc cref="LeadingIcon" />
    public static readonly StyledProperty<object?> LeadingIconProperty =
        AvaloniaProperty.Register<MaterialSearchBar, object?>(nameof(LeadingIcon));

    /// <inheritdoc cref="TrailingIcon" />
    public static readonly StyledProperty<object?> TrailingIconProperty =
        AvaloniaProperty.Register<MaterialSearchBar, object?>(nameof(TrailingIcon));

    /// <inheritdoc cref="Results" />
    public static readonly StyledProperty<object?> ResultsProperty =
        AvaloniaProperty.Register<MaterialSearchBar, object?>(nameof(Results));

    /// <inheritdoc cref="IsOpen" />
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MaterialSearchBar, bool>(
            nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Raised when the user commits a query.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> SearchEvent =
        RoutedEvent.Register<MaterialSearchBar, RoutedEventArgs>(nameof(Search), RoutingStrategies.Bubble);

    private readonly MaterialInteractionController _interaction;

    private AnimatedShape? _shape;
    private TextBox? _textBox;
    private SearchBarDefaultsTokens _tokens = SearchBarDefaultsTokens.Default;
    private SearchBarColors _colors = SearchBarDefaultsTokens.Default.Resolve(null, false);

    public MaterialSearchBar()
    {
        _material = MaterialElementContext.GetOrCreate(this);
        _interaction = MaterialElementContext.GetOrCreate(this).Interaction;
        _interaction.IsRippleEnabled = false;

        ApplyTokens();
    }

    static MaterialSearchBar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSearchBar>(false);
        AffectsRender<MaterialSearchBar>(IsOpenProperty);
    }

    /// <summary>Raised when the user presses Enter.</summary>
    public event EventHandler<RoutedEventArgs>? Search
    {
        add => AddHandler(SearchEvent, value);
        remove => RemoveHandler(SearchEvent, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>What the bar says while empty.</summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>The leading slot — a search glyph, or a back arrow once open.</summary>
    public object? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    /// <summary>The trailing slot — a clear button, or an avatar.</summary>
    public object? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    /// <summary>What to show under the bar once it is open.</summary>
    public object? Results
    {
        get => GetValue(ResultsProperty);
        set => SetValue(ResultsProperty, value);
    }

    /// <summary>Whether the results surface is showing.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>The bar's corners, morph included.</summary>
    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    /// <summary>The size icons in the two slots should be drawn at.</summary>
    public double IconSize => _tokens.IconSize;

    /// <summary>The gap between an icon and the input.</summary>
    public double IconLabelSpace => _tokens.IconLabelSpace;

    public static readonly DirectProperty<MaterialSearchBar, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialSearchBar, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, double>(
            nameof(IconLabelSpace), o => o.IconLabelSpace);

    private AnimatedShape Shape =>
        _shape ??= _interaction.CreateAnimatedShape(RestingShape(), Spring(MotionRole.DefaultSpatial));

    public override void Render(DrawingContext context)
    {
        var corners = ContainerCornerRadius;
        var shadow = ResolvedTheme.ColorScheme?.GetColor(SysColorToken.Shadow, IsDarkVariant) ?? Colors.Black;

        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            IsOpen ? _colors.ResultsContainerColor : _colors.ContainerColor,
            null,
            0,
            _tokens.ContainerElevation,
            shadow);

        if (IsOpen)
        {
            // The rule separates the query from what it found.
            var y = _tokens.ContainerHeight;
            context.DrawLine(
                new Avalonia.Media.Immutable.ImmutablePen(_colors.DividerColor, _tokens.DividerHeight),
                new Point(0, y),
                new Point(Bounds.Width, y));
        }

        base.Render(context);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_textBox is { })
            _textBox.KeyDown -= OnEditorKeyDown;

        _textBox = e.NameScope.Find<TextBox>(TextBoxPartName);

        if (_textBox is { })
            _textBox.KeyDown += OnEditorKeyDown;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
            UpdateShape();
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RaiseEvent(new RoutedEventArgs(SearchEvent, this));
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && IsOpen)
        {
            IsOpen = false;
            e.Handled = true;
        }
    }

    /// <summary>
    /// A pill when closed, a rounded rectangle when open.
    /// </summary>
    /// <remarks>
    /// The open shape has to come off the shape scheme rather than being fixed, so a theme that
    /// retunes its corner sizes retunes the search surface with everything else.
    /// </remarks>
    private RoundedCornerShape RestingShape()
    {
        return ResolvedTheme.Shapes.Get(
            IsOpen ? _tokens.ResultsContainerShape : _tokens.ContainerShape);
    }

    private void UpdateShape(bool snap = false)
    {
        var target = RestingShape();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            InvalidateVisual();
            return;
        }

        Shape.AnimateTo(target, Spring(MotionRole.DefaultSpatial));
        _interaction.Motion.Start();
    }

    private void ApplyTokens()
    {
        var previousIconSize = IconSize;
        var previousSpace = IconLabelSpace;

        _tokens = ResolvedTheme.SearchBarDefaultsTokens;
        _colors = ResolvedTheme.GetSearchBarColors(IsDarkVariant);

        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previousSpace, IconLabelSpace);

        var typography = ResolvedTheme.Typography;

        Foreground = _colors.InputTextColor;
        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinHeight = _tokens.ContainerHeight;
        FontSize = typography.GetTypeScale(_tokens.InputTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.InputTextStyle);

        UpdateShape(snap: true);
        InvalidateMeasure();
        InvalidateVisual();
    }
}
