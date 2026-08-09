using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A button with a separate trailing segment: the leading half performs the default action, the
/// trailing half opens the alternatives.
/// </summary>
/// <remarks>
/// Two real buttons rather than one control drawing two halves, so each half keeps its own state
/// layer, ripple and press morph — which is the point, since the two halves are pressed
/// independently and have to say so separately.
/// <para>
/// What the container contributes is the shape: the outer corners stay fully round and the two
/// facing corners tighten, so the pair reads as one control with a seam rather than as two
/// buttons that happen to be adjacent.
/// </para>
/// </remarks>
[TemplatePart(LeadingPartName, typeof(Button))]
[TemplatePart(TrailingPartName, typeof(ToggleButton))]
public class MaterialSplitButton : TemplatedControl, IMaterialThemeConsumer
{
    private const string LeadingPartName = "PART_Leading";
    private const string TrailingPartName = "PART_Trailing";

    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialSplitButton, ButtonVariant>(nameof(Variant));

    /// <inheritdoc cref="Content" />
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<MaterialSplitButton, object?>(nameof(Content));

    /// <inheritdoc cref="TrailingIcon" />
    public static readonly StyledProperty<object?> TrailingIconProperty =
        AvaloniaProperty.Register<MaterialSplitButton, object?>(nameof(TrailingIcon));

    /// <inheritdoc cref="IsExpanded" />
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MaterialSplitButton, bool>(nameof(IsExpanded), defaultBindingMode:
            Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Raised when the leading half is clicked.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<MaterialSplitButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private Button? _leading;
    private ToggleButton? _trailing;
    private readonly MaterialElementContext _theme;
    private SplitButtonMetrics _tokens = SplitButtonDefaults.Metrics();

    static MaterialSplitButton()
    {
        AffectsMeasure<MaterialSplitButton>(ContentProperty, TrailingIconProperty);
    }

    public MaterialSplitButton()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    /// <summary>The default action.</summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Both halves take the same colour variant; a seam between two colours is not a seam.</summary>
    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>The leading half's label.</summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The trailing half's icon, usually a chevron.</summary>
    public object? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    /// <summary>
    /// Whether the trailing half is showing as open. Two-way, so a menu closing by any means can
    /// put the segment back.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>The gap between the halves, which is a token rather than a margin in the template.</summary>
    public double BetweenSpace => _tokens.BetweenSpace;

    /// <summary>Exposes <see cref="BetweenSpace" /> to the template, which needs an AvaloniaProperty.</summary>
    public static readonly DirectProperty<MaterialSplitButton, double> BetweenSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialSplitButton, double>(nameof(BetweenSpace), o => o.BetweenSpace);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_leading is { })
            _leading.Click -= OnLeadingClick;

        _leading = e.NameScope.Find<Button>(LeadingPartName);
        _trailing = e.NameScope.Find<ToggleButton>(TrailingPartName);

        if (_leading is { })
            _leading.Click += OnLeadingClick;

        ApplyTokens();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FlowDirectionProperty)
            ApplyShapes();
    }

    private void OnLeadingClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }

    /// <summary>
    /// Rounds the outside and tightens the seam.
    /// </summary>
    /// <remarks>
    /// Set once from the template rather than bound, because the two shapes are constants of the
    /// arrangement — which half is on the left is the only thing that varies, and that is flow
    /// direction.
    /// </remarks>
    private void ApplyShapes()
    {
        var outer = CornerSize.Full;
        var inner = CornerSize.Absolute(_tokens.InnerCornerSize);
        var flipped = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;

        var leadingShape = flipped
            ? new RoundedCornerShape(inner, outer, outer, inner)
            : new RoundedCornerShape(outer, inner, inner, outer);

        var trailingShape = flipped
            ? new RoundedCornerShape(outer, inner, inner, outer)
            : new RoundedCornerShape(inner, outer, outer, inner);

        if (_leading is { })
            M3Button.SetContainerShapeOverride(_leading, leadingShape);

        if (_trailing is { })
            M3Button.SetContainerShapeOverride(_trailing, trailingShape);
    }

    private void ApplyTokens()
    {
        _tokens = SplitButtonDefaults.Metrics();
        ApplyShapes();
        InvalidateMeasure();
        InvalidateVisual();
    }
}
