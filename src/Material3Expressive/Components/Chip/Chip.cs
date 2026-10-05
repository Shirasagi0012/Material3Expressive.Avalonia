using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;
using AnimatedCornerRadius = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;

namespace Material3Expressive.Components;

/// <summary>Shared Chip.kt content and surface composition. Click/Command requests an action.</summary>
public abstract class Chip : Button
{
    public static readonly StyledProperty<object?> LeadingIconProperty = AvaloniaProperty.Register<Chip, object?>(nameof(LeadingIcon));
    public object? LeadingIcon { get => GetValue(LeadingIconProperty); set => SetValue(LeadingIconProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> LeadingIconTemplateProperty = AvaloniaProperty.Register<Chip, IDataTemplate?>(nameof(LeadingIconTemplate));
    public IDataTemplate? LeadingIconTemplate { get => GetValue(LeadingIconTemplateProperty); set => SetValue(LeadingIconTemplateProperty, value); }
    public static readonly StyledProperty<object?> TrailingIconProperty = AvaloniaProperty.Register<Chip, object?>(nameof(TrailingIcon));
    public object? TrailingIcon { get => GetValue(TrailingIconProperty); set => SetValue(TrailingIconProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> TrailingIconTemplateProperty = AvaloniaProperty.Register<Chip, IDataTemplate?>(nameof(TrailingIconTemplate));
    public IDataTemplate? TrailingIconTemplate { get => GetValue(TrailingIconTemplateProperty); set => SetValue(TrailingIconTemplateProperty, value); }
    public static readonly StyledProperty<ChipColors?> ColorsProperty = AvaloniaProperty.Register<Chip, ChipColors?>(nameof(Colors));
    public ChipColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<ChipElevation?> ElevationProperty = AvaloniaProperty.Register<Chip, ChipElevation?>(nameof(Elevation));
    /// <summary>Null uses the overload defaults; an all-zero configuration removes the shadow.</summary>
    public ChipElevation? Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public static readonly StyledProperty<ChipSpacing?> SpacingProperty = AvaloniaProperty.Register<Chip, ChipSpacing?>(nameof(Spacing));
    public ChipSpacing? Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    internal bool Selected => this is SelectableChip { IsSelected: true };
    internal bool Elevated => this switch { AssistChip c => c.IsElevated, SuggestionChip c => c.IsElevated, FilterChip c => c.IsElevated, _ => false };
    internal ChipShapes? MorphShapes => (this as SelectableChip)?.Shapes;
    private MaterialSurface? _surface;
    private ChipContentLayout? _content;
    private ChipIconSlot? _leading;
    private ChipIconSlot? _trailing;
    private VisualAnimationScope? _motion;
    private Animatable<double>? _elevation;
    private AnimatedCornerRadius? _shape;
    private (CornerRadius Shape, CornerRadius Selected, CornerRadius Pressed)? _shapeConfiguration;
    private InteractionState _lastElevationInteraction;
    private bool _pending;
    private CancellationTokenSource? _shapeEffect;
    private Task? _shapeTask;

    protected Chip()
    {
        MaterialInteraction.GetOrCreate(this).Changed += QueueTargets;
        ResourcesChanged += (_, _) => Refresh();
        ActualThemeVariantChanged += (_, _) => Refresh();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        DisposeMotion();
        base.OnApplyTemplate(e);
        _surface = e.NameScope.Find<MaterialSurface>("PART_Container");
        _content = e.NameScope.Find<ChipContentLayout>("PART_Layout");
        _leading = e.NameScope.Find<ChipIconSlot>("PART_LeadingIcon");
        _trailing = e.NameScope.Find<ChipIconSlot>("PART_TrailingIcon");
        InitializeMotion();
        Refresh();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InitializeMotion();
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DisposeMotion();
        base.OnDetachedFromVisualTree(e);
    }

    private void InitializeMotion()
    {
        if (_surface is null || !this.IsAttachedToVisualTree() || _motion is not null) return;
        _motion = VisualAnimationScope.Create(this);
        _elevation = _motion.CreateAnimatable(ElevationTarget());
        _motion.BindAnimatedValue(_surface, MaterialSurface.ShadowElevationProperty, _elevation);
        var surface = _surface;
        void OnSurfaceBoundsChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == BoundsProperty && _shape is { } shape)
            { shape.Size = surface.Bounds.Size; surface.CornerRadius = shape.Current; }
        }
        surface.PropertyChanged += OnSurfaceBoundsChanged;
        _motion.Lifetime.Register(() => surface.PropertyChanged -= OnSurfaceBoundsChanged);
        _leading?.Initialize(_motion);
        _trailing?.Initialize(_motion);
        RefreshShape(initial: true);
    }

    private void DisposeMotion()
    {
        _leading?.Stop(); _trailing?.Stop();
        _shapeEffect?.Cancel(); _shapeEffect = null; _shapeTask = null;
        _shape?.Dispose(); _shape = null; _shapeConfiguration = null;
        _motion?.Dispose(); _motion = null; _elevation = null; _pending = false;
        _lastElevationInteraction = InteractionState.None;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LeadingIconProperty || change.Property == TrailingIconProperty
            || change.Property == LeadingIconTemplateProperty || change.Property == TrailingIconTemplateProperty
            || change.Property == ColorsProperty || change.Property == ElevationProperty || change.Property == SpacingProperty
            || change.Property == IsEffectivelyEnabledProperty || change.Property == CornerRadiusProperty
            || change.Property == SelectableChip.IsSelectedProperty || change.Property == SelectableChip.ShapesProperty
            || change.Property == AssistChip.IsElevatedProperty || change.Property == FilterChip.IsElevatedProperty
            || change.Property == SuggestionChip.IsElevatedProperty || change.Property == InputChip.AvatarProperty
            || change.Property == InputChip.AvatarTemplateProperty)
            Refresh();
    }

    private void Refresh()
    {
        if (_surface is null || _content is null || !((ILogical)this).IsAttachedToLogicalTree
            || !TokenResolver.TryResolve(this, Comp.Chip.ContainerHeight, ActualThemeVariant, out _)) return;
        var colors = Colors;
        var enabled = IsEffectivelyEnabled;
        var selected = Selected;
        var normalContainer = Elevated ? Resolve(Comp.Chip.ElevatedContainerColor) : Avalonia.Media.Colors.Transparent;
        var container = enabled ? selected ? colors?.SelectedContainerColor ?? Resolve(Comp.Chip.SelectedContainerColor)
                : colors?.ContainerColor ?? normalContainer
            : selected ? colors?.DisabledSelectedContainerColor ?? Resolve(Comp.Chip.DisabledContainerColor)
                : colors?.DisabledContainerColor ?? (Elevated ? Resolve(Comp.Chip.DisabledContainerColor) : Avalonia.Media.Colors.Transparent);
        var label = !enabled ? colors?.DisabledLabelColor ?? Resolve(Comp.Chip.DisabledContentColor)
            : selected ? colors?.SelectedLabelColor ?? Resolve(Comp.Chip.SelectedContentColor)
            : colors?.LabelColor ?? Resolve(this is AssistChip ? Comp.Chip.AssistLabelColor : Comp.Chip.LabelColor);
        var leading = !enabled ? colors?.DisabledLeadingIconColor ?? Resolve(Comp.Chip.DisabledContentColor)
            : selected ? colors?.SelectedLeadingIconColor ?? Resolve(this is InputChip ? Comp.Chip.PrimaryIconColor : Comp.Chip.SelectedContentColor)
            : colors?.LeadingIconColor ?? Resolve(this is InputChip || (this is FilterChip && MorphShapes is not null)
                ? Comp.Chip.TonalIconColor : Comp.Chip.PrimaryIconColor);
        var trailing = !enabled ? colors?.DisabledTrailingIconColor ?? Resolve(Comp.Chip.DisabledContentColor)
            : selected ? colors?.SelectedTrailingIconColor ?? Resolve(Comp.Chip.SelectedContentColor)
            : colors?.TrailingIconColor ?? Resolve(this is AssistChip ? Comp.Chip.PrimaryIconColor : Comp.Chip.TonalIconColor);
        // Style priority preserves explicit native paint and bindings on the public control.
        SetValue(BackgroundProperty, new SolidColorBrush(container), BindingPriority.Style);
        SetValue(ForegroundProperty, new SolidColorBrush(label), BindingPriority.Style);
        SetValue(BorderBrushProperty, new SolidColorBrush(Resolve(enabled ? Comp.Chip.OutlineColor : Comp.Chip.DisabledOutlineColor)), BindingPriority.Style);
        SetValue(BorderThicknessProperty, new Thickness(Elevated || selected ? 0 : Resolve(Comp.Chip.OutlineWidth)), BindingPriority.Style);
        var input = this as InputChip;
        var hasAvatar = input is not null && (input.Avatar is not null || input.AvatarTemplate is not null);
        var leadingContent = hasAvatar ? input!.Avatar : LeadingIcon;
        var leadingTemplate = hasAvatar ? input!.AvatarTemplate : LeadingIconTemplate;
        var hasLeadingIcon = hasAvatar || leadingContent is not null || leadingTemplate is not null;
        var hasTrailingIcon = this is not SuggestionChip && (TrailingIcon is not null || TrailingIconTemplate is not null);
        _leading?.Update(leadingContent, leadingTemplate,
            this is SelectableChip, hasAvatar, enabled ? 1 : Resolve(Comp.Chip.AvatarOpacity),
            hasAvatar ? Foreground : new SolidColorBrush(leading), this);
        _trailing?.Update(this is SuggestionChip ? null : TrailingIcon,
            this is SuggestionChip ? null : TrailingIconTemplate,
            this is SelectableChip, false, 1, new SolidColorBrush(trailing), this);
        _content.Spacing = Spacing ?? (MorphShapes is not null ? new ChipSpacing(
            hasLeadingIcon ? 4 : 8, hasTrailingIcon ? 4 : 8) : new ChipSpacing());
        var start = input is null ? 8 : hasAvatar || !hasLeadingIcon ? 4 : 8;
        var end = input is null ? 8 : hasTrailingIcon ? 8 : 4;
        SetValue(PaddingProperty, new Thickness(start, 0, end, 0), BindingPriority.Style);
        if (MorphShapes is null) _surface.CornerRadius = CornerRadius;
        PseudoClasses.Set(":selected", selected);
        QueueTargets();
    }

    internal T Resolve<T>(MaterialTokenKey<T> key) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is T result
            ? result : throw new InvalidOperationException($"Chip requires token {key}.");

    internal MaterialSpring Spring(MaterialTokenKey<MaterialSpring> key) => Resolve(key);

    private ChipElevation ElevationValues() => Elevation ?? (Elevated
        ? new ChipElevation(Resolve(Comp.Chip.ElevatedElevation), Resolve(Comp.Chip.ElevatedElevation),
            Resolve(Comp.Chip.ElevatedElevation), Resolve(Comp.Chip.ElevatedHoverElevation),
            Resolve(Comp.Chip.DraggedElevation), Resolve(Comp.Chip.DisabledElevation))
        : new ChipElevation(Resolve(Comp.Chip.FlatElevation), Resolve(Comp.Chip.FlatElevation),
            Resolve(Comp.Chip.FlatElevation), Resolve(this is FilterChip ? Comp.Chip.FlatFilterHoverElevation : Comp.Chip.FlatElevation),
            Resolve(Comp.Chip.DraggedElevation), Resolve(Comp.Chip.FlatElevation)));

    private double ElevationTarget()
    {
        var values = ElevationValues();
        return !IsEffectivelyEnabled ? values.Disabled : MaterialInteraction.GetOrCreate(this).LastInteraction switch
        {
            InteractionState.Pressed => values.Pressed, InteractionState.Hovered => values.Hovered,
            InteractionState.Focused => values.Focused, InteractionState.Dragged => values.Dragged, _ => values.Default
        };
    }

    private void QueueTargets()
    {
        if (_motion is not { IsEnding: false } motion || _pending) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            _pending = false;
            var target = ElevationTarget();
            if (_elevation is { } elevation && elevation.TargetValue != target)
            {
                var interaction = MaterialInteraction.GetOrCreate(this).LastInteraction;
                var from = _lastElevationInteraction;
                _lastElevationInteraction = interaction;
                _ = motion.Launch(token => !IsEffectivelyEnabled || (interaction == InteractionState.None && from == InteractionState.None)
                    ? elevation.SnapToAsync(target, token)
                    : elevation.AnimateToAsync(target, new TweenSpec<double>(TimeSpan.FromMilliseconds(
                        interaction != InteractionState.None || from == InteractionState.Hovered ? 120 : 150),
                        easing: interaction != InteractionState.None ? ComposeEasings.FastOutSlowIn : new CubicBezierEasing(0.4, 0, 0.6, 1)),
                        cancellationToken: token), "Chip elevation");
            }
            RefreshShape(initial: false);
        }, "Chip composition");
    }

    private void RefreshShape(bool initial)
    {
        if (_surface is null || _motion is not { } motion) return;
        var shapes = MorphShapes;
        if (shapes is null)
        {
            _shapeEffect?.Cancel(); _shapeEffect = null;
            _shape?.Dispose(); _shape = null; _shapeConfiguration = null;
            _surface.CornerRadius = CornerRadius;
            return;
        }
        var pressed = MaterialInteraction.GetOrCreate(this).Interaction.HasFlag(InteractionState.Pressed);
        // key(shapes) uses ChipShapes' structural equality, including resolved theme defaults.
        var configuration = (Shape: shapes.Shape ?? Resolve(Comp.Chip.UnselectedShape),
            Selected: shapes.SelectedShape ?? Resolve(Comp.Chip.SelectedShape),
            Pressed: shapes.PressedShape ?? Resolve(Comp.Chip.PressedShape));
        var target = pressed ? configuration.Pressed : Selected ? configuration.Selected : configuration.Shape;
        var spec = Spring(Sys.Motion.Spring.SpatialFast);
        if (initial || _shapeConfiguration != configuration || _shape is null)
        {
            _shapeEffect?.Cancel(); _shapeEffect = null;
            _shape?.Dispose(); _shapeConfiguration = configuration;
            _shape = new AnimatedCornerRadius(target, spec, motion, () =>
            {
                if (_shape is { } shape && _surface is { } surface)
                { shape.Size = surface.Bounds.Size; surface.CornerRadius = shape.Current; }
            }) { Size = _surface.Bounds.Size };
            _surface.CornerRadius = _shape.Current;
        }
        if (_shape.Target == target) return;
        _shapeEffect?.Cancel();
        var previous = _shapeTask;
        _shapeTask = motion.Launch(async ct =>
        {
            if (previous is not null) { try { await previous; } catch (OperationCanceledException) { } }
            using var effect = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _shapeEffect = effect;
            try
            {
                if (_shape is { } shape)
                { shape.Size = _surface.Bounds.Size; await shape.AnimateToAsync(target, spec, effect.Token); }
            }
            finally { if (ReferenceEquals(_shapeEffect, effect)) _shapeEffect = null; }
        }, "Chip shape");
    }
}

public sealed class AssistChip : Chip
{
    public static readonly StyledProperty<bool> IsElevatedProperty = AvaloniaProperty.Register<AssistChip, bool>(nameof(IsElevated));
    public bool IsElevated { get => GetValue(IsElevatedProperty); set => SetValue(IsElevatedProperty, value); }
}

public sealed class SuggestionChip : Chip
{
    public static readonly StyledProperty<bool> IsElevatedProperty = AvaloniaProperty.Register<SuggestionChip, bool>(nameof(IsElevated));
    public bool IsElevated { get => GetValue(IsElevatedProperty); set => SetValue(IsElevatedProperty, value); }
}

/// <summary>Compose selectable Surface: the application owns selection; activating does not toggle it.</summary>
public abstract class SelectableChip : Chip
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SelectableChip, bool>(nameof(IsSelected));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public static readonly StyledProperty<ChipShapes?> ShapesProperty = AvaloniaProperty.Register<SelectableChip, ChipShapes?>(nameof(Shapes));
    public ChipShapes? Shapes { get => GetValue(ShapesProperty); set => SetValue(ShapesProperty, value); }
    protected override AutomationPeer OnCreateAutomationPeer() => new ChipAutomationPeer(this);

    private sealed class ChipAutomationPeer : ButtonAutomationPeer, IToggleProvider
    {
        public ChipAutomationPeer(SelectableChip owner) : base(owner)
        {
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsSelectedProperty)
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                        (bool)change.OldValue! ? ToggleState.On : ToggleState.Off,
                        (bool)change.NewValue! ? ToggleState.On : ToggleState.Off);
            };
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;
        protected override string GetClassNameCore() => Owner.GetType().Name;
        public ToggleState ToggleState => ((SelectableChip)Owner).IsSelected ? ToggleState.On : ToggleState.Off;
        public void Toggle() => ((IInvokeProvider)this).Invoke();
    }
}

public sealed class FilterChip : SelectableChip
{
    public static readonly StyledProperty<bool> IsElevatedProperty = AvaloniaProperty.Register<FilterChip, bool>(nameof(IsElevated));
    public bool IsElevated { get => GetValue(IsElevatedProperty); set => SetValue(IsElevatedProperty, value); }
}

public sealed class InputChip : SelectableChip
{
    public static readonly StyledProperty<object?> AvatarProperty = AvaloniaProperty.Register<InputChip, object?>(nameof(Avatar));
    public object? Avatar { get => GetValue(AvatarProperty); set => SetValue(AvatarProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> AvatarTemplateProperty = AvaloniaProperty.Register<InputChip, IDataTemplate?>(nameof(AvatarTemplate));
    public IDataTemplate? AvatarTemplate { get => GetValue(AvatarTemplateProperty); set => SetValue(AvatarTemplateProperty, value); }
}
