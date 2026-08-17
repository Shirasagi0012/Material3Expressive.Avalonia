using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

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
public class MaterialSearchBar : TemplatedControl, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default;
    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

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

    internal static readonly StyledProperty<SearchBarGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialSearchBar, SearchBarGeometryProfile>(
            nameof(GeometryProfile),
            new SearchBarGeometryProfile(
                ShapeRole.CornerFull,
                ShapeRole.CornerExtraLarge,
                3,
                56,
                new Thickness(16, 0),
                24,
                16,
                1));

    internal static readonly StyledProperty<SearchBarTypographyProfile> TypographyProfileProperty =
        AvaloniaProperty.Register<MaterialSearchBar, SearchBarTypographyProfile>(
            nameof(TypographyProfile),
            new SearchBarTypographyProfile(TypographyRole.BodyLarge));

    internal static readonly StyledProperty<SearchBarVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialSearchBar, SearchBarVisualColors>(
            nameof(VisualColors),
            TransparentVisualColors());

    /// <summary>Raised when the user commits a query.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> SearchEvent =
        RoutedEvent.Register<MaterialSearchBar, RoutedEventArgs>(nameof(Search), RoutingStrategies.Bubble);

    private readonly MaterialInteractionController _interaction;

    private AnimatedShape? _shape;
    private TextBox? _textBox;
    private SearchBarGeometryProfile _geometry = new(
        ShapeRole.CornerFull,
        ShapeRole.CornerExtraLarge,
        3,
        56,
        new Thickness(16, 0),
        24,
        16,
        1);
    private SearchBarTypographyProfile _typography = new(TypographyRole.BodyLarge);
    private SearchBarVisualColors _colors = TransparentVisualColors();
    private IImmutableBrush _inputBrush = TransparentBrush();
    private IImmutableBrush _supportingTextBrush = TransparentBrush();
    private IImmutableBrush _leadingIconBrush = TransparentBrush();
    private IImmutableBrush _trailingIconBrush = TransparentBrush();

    public MaterialSearchBar()
    {
        _material = MaterialElementContext.Ensure(this);
        _interaction = MaterialElementContext.Ensure(this).Interaction;
        _interaction.IsRippleEnabled = false;

        ApplyProfiles();
    }

    static MaterialSearchBar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSearchBar>(false);
        AffectsRender<MaterialSearchBar>(
            IsOpenProperty,
            GeometryProfileProperty,
            TypographyProfileProperty,
            VisualColorsProperty);
        AffectsMeasure<MaterialSearchBar>(GeometryProfileProperty, TypographyProfileProperty);
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

    internal SearchBarGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    internal SearchBarTypographyProfile TypographyProfile
    {
        get => GetValue(TypographyProfileProperty);
        set => SetValue(TypographyProfileProperty, value);
    }

    internal SearchBarVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    /// <summary>The bar's corners, morph included.</summary>
    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    /// <summary>The size icons in the two slots should be drawn at.</summary>
    public double IconSize => _geometry.IconSize;

    /// <summary>The gap between an icon and the input.</summary>
    public double IconLabelSpace => _geometry.IconLabelSpace;

    public static readonly DirectProperty<MaterialSearchBar, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialSearchBar, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, double>(
            nameof(IconLabelSpace), o => o.IconLabelSpace);

    internal static readonly DirectProperty<MaterialSearchBar, IImmutableBrush> InputBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, IImmutableBrush>(
            nameof(InputBrush), o => o.InputBrush);

    internal static readonly DirectProperty<MaterialSearchBar, IImmutableBrush> SupportingTextBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, IImmutableBrush>(
            nameof(SupportingTextBrush), o => o.SupportingTextBrush);

    public static readonly DirectProperty<MaterialSearchBar, IImmutableBrush> LeadingIconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, IImmutableBrush>(
            nameof(LeadingIconBrush), o => o.LeadingIconBrush);

    public static readonly DirectProperty<MaterialSearchBar, IImmutableBrush> TrailingIconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSearchBar, IImmutableBrush>(
            nameof(TrailingIconBrush), o => o.TrailingIconBrush);

    internal IImmutableBrush InputBrush => _inputBrush;

    internal IImmutableBrush SupportingTextBrush => _supportingTextBrush;

    public IImmutableBrush LeadingIconBrush => _leadingIconBrush;

    public IImmutableBrush TrailingIconBrush => _trailingIconBrush;

    private AnimatedShape Shape => _shape ??= CreateShape();

    private AnimatedShape CreateShape()
    {
        var shape = new AnimatedShape(RestingShape(), Spring(MotionRole.DefaultSpatial));
        _interaction.Motion.Add(shape.Progress);
        return shape;
    }

    public override void Render(DrawingContext context)
    {
        var corners = ContainerCornerRadius;
        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            IsOpen ? _colors.ResultsContainerColor : _colors.ContainerColor,
            null,
            0,
            _geometry.ContainerElevation,
            _colors.ShadowColor);

        if (IsOpen)
        {
            // The rule separates the query from what it found.
            var y = _geometry.ContainerHeight;
            context.DrawLine(
                new Avalonia.Media.Immutable.ImmutablePen(_colors.DividerColor, _geometry.DividerHeight),
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
        else if (change.Property == GeometryProfileProperty
                 || change.Property == TypographyProfileProperty
                 || change.Property == VisualColorsProperty)
            ApplyProfiles();
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
            IsOpen ? _geometry.ResultsContainerShape : _geometry.ContainerShape);
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
        _interaction.Motion.RequestFrame();
    }

    private void ApplyProfiles()
    {
        var previousIconSize = IconSize;
        var previousSpace = IconLabelSpace;

        _geometry = GeometryProfile;
        _typography = TypographyProfile;
        _colors = VisualColors;

        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previousSpace, IconLabelSpace);
        SetAndRaise(InputBrushProperty, ref _inputBrush, _colors.InputTextColor);
        SetAndRaise(
            SupportingTextBrushProperty,
            ref _supportingTextBrush,
            _colors.SupportingTextColor);
        SetAndRaise(
            LeadingIconBrushProperty,
            ref _leadingIconBrush,
            _colors.LeadingIconColor);
        SetAndRaise(
            TrailingIconBrushProperty,
            ref _trailingIconBrush,
            _colors.TrailingIconColor);

        UpdateShape(snap: true);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private static SearchBarVisualColors TransparentVisualColors()
    {
        var transparent = TransparentBrush();
        return new SearchBarVisualColors(
            transparent,
            transparent,
            transparent,
            transparent,
            transparent,
            transparent,
            transparent,
            Colors.Black);
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
