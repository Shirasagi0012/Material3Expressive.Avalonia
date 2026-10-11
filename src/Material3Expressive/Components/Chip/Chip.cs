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
    public static readonly StyledProperty<ChipElevation> ElevationProperty = AvaloniaProperty.Register<Chip, ChipElevation>(nameof(Elevation), new ChipElevation(), validate: value => value is not null);
    /// <summary>Complete interaction shadow configuration supplied by the theme; all-zero values remove the shadow.</summary>
    public ChipElevation Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public static readonly StyledProperty<ChipSpacing> SpacingProperty = AvaloniaProperty.Register<Chip, ChipSpacing>(nameof(Spacing), new ChipSpacing(), validate: value => value is not null);
    public ChipSpacing Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    public static readonly StyledProperty<IBrush?> LeadingIconColorProperty = AvaloniaProperty.Register<Chip, IBrush?>(nameof(LeadingIconColor));
    public IBrush? LeadingIconColor { get => GetValue(LeadingIconColorProperty); set => SetValue(LeadingIconColorProperty, value); }
    public static readonly StyledProperty<IBrush?> TrailingIconColorProperty = AvaloniaProperty.Register<Chip, IBrush?>(nameof(TrailingIconColor));
    public IBrush? TrailingIconColor { get => GetValue(TrailingIconColorProperty); set => SetValue(TrailingIconColorProperty, value); }
    internal static readonly StyledProperty<IBrush?> DefaultLeadingIconColorProperty = AvaloniaProperty.Register<Chip, IBrush?>(nameof(DefaultLeadingIconColor));
    internal IBrush? DefaultLeadingIconColor { get => GetValue(DefaultLeadingIconColorProperty); set => SetValue(DefaultLeadingIconColorProperty, value); }
    internal static readonly StyledProperty<IBrush?> DefaultTrailingIconColorProperty = AvaloniaProperty.Register<Chip, IBrush?>(nameof(DefaultTrailingIconColor));
    internal IBrush? DefaultTrailingIconColor { get => GetValue(DefaultTrailingIconColorProperty); set => SetValue(DefaultTrailingIconColorProperty, value); }
    internal static readonly StyledProperty<double> DefaultLeadingSpacingProperty = AvaloniaProperty.Register<Chip, double>(nameof(DefaultLeadingSpacing));
    internal double DefaultLeadingSpacing { get => GetValue(DefaultLeadingSpacingProperty); set => SetValue(DefaultLeadingSpacingProperty, value); }
    internal static readonly StyledProperty<double> DefaultTrailingSpacingProperty = AvaloniaProperty.Register<Chip, double>(nameof(DefaultTrailingSpacing));
    internal double DefaultTrailingSpacing { get => GetValue(DefaultTrailingSpacingProperty); set => SetValue(DefaultTrailingSpacingProperty, value); }
    internal static readonly StyledProperty<ChipElevation> DefaultElevationProperty = AvaloniaProperty.Register<Chip, ChipElevation>(nameof(DefaultElevation), new ChipElevation());
    internal ChipElevation DefaultElevation { get => GetValue(DefaultElevationProperty); set => SetValue(DefaultElevationProperty, value); }
    internal static readonly StyledProperty<MaterialSpring?> ShapeAnimationProperty = AvaloniaProperty.Register<Chip, MaterialSpring?>(nameof(ShapeAnimation));
    internal MaterialSpring? ShapeAnimation { get => GetValue(ShapeAnimationProperty); set => SetValue(ShapeAnimationProperty, value); }
    internal static readonly StyledProperty<double> DisabledAvatarOpacityProperty = AvaloniaProperty.Register<Chip, double>(nameof(DisabledAvatarOpacity));
    internal double DisabledAvatarOpacity { get => GetValue(DisabledAvatarOpacityProperty); set => SetValue(DisabledAvatarOpacityProperty, value); }
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
        UpdateContentFacts();
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
            { shape.Size = surface.Bounds.Size; surface.SetAnimatedCorners(shape.Current); }
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
        _surface?.SetAnimatedCorners(null);
        _motion?.Dispose(); _motion = null; _elevation = null; _pending = false;
        _lastElevationInteraction = InteractionState.None;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LeadingIconProperty || change.Property == TrailingIconProperty
            || change.Property == LeadingIconTemplateProperty || change.Property == TrailingIconTemplateProperty
            || change.Property == LeadingIconColorProperty || change.Property == TrailingIconColorProperty
            || change.Property == ForegroundProperty || change.Property == DisabledAvatarOpacityProperty
            || change.Property == ShapeAnimationProperty || change.Property == ElevationProperty || change.Property == SpacingProperty
            || change.Property == IsEffectivelyEnabledProperty || change.Property == CornerRadiusProperty
            || change.Property == SelectableChip.IsSelectedProperty || change.Property == SelectableChip.ShapesProperty
            || change.Property == AssistChip.IsElevatedProperty || change.Property == FilterChip.IsElevatedProperty
            || change.Property == SuggestionChip.IsElevatedProperty || change.Property == InputChip.AvatarProperty
            || change.Property == InputChip.AvatarTemplateProperty)
        {
            UpdateContentFacts();
            Refresh();
        }
    }

    private void UpdateContentFacts()
    {
        var input = this as InputChip;
        var avatar = input is not null && (input.Avatar is not null || input.AvatarTemplate is not null);
        PseudoClasses.Set(":assist", this is AssistChip);
        PseudoClasses.Set(":input", input is not null);
        PseudoClasses.Set(":filter", this is FilterChip);
        PseudoClasses.Set(":selected", Selected);
        PseudoClasses.Set(":elevated", Elevated);
        PseudoClasses.Set(":avatar", avatar);
        PseudoClasses.Set(":leading-icon", avatar || LeadingIcon is not null || LeadingIconTemplate is not null);
        PseudoClasses.Set(":trailing-icon", this is not SuggestionChip && (TrailingIcon is not null || TrailingIconTemplate is not null));
    }

    internal void Refresh()
    {
        if (_surface is null || _content is null || !((ILogical)this).IsAttachedToLogicalTree) return;
        var enabled = IsEffectivelyEnabled;
        var input = this as InputChip;
        var hasAvatar = input is not null && (input.Avatar is not null || input.AvatarTemplate is not null);
        var leadingContent = hasAvatar ? input!.Avatar : LeadingIcon;
        var leadingTemplate = hasAvatar ? input!.AvatarTemplate : LeadingIconTemplate;
        _leading?.Update(leadingContent, leadingTemplate,
            this is SelectableChip, hasAvatar, enabled ? 1 : DisabledAvatarOpacity,
            hasAvatar ? Foreground : LeadingIconColor, this);
        _trailing?.Update(this is SuggestionChip ? null : TrailingIcon,
            this is SuggestionChip ? null : TrailingIconTemplate,
            this is SelectableChip, false, 1, TrailingIconColor, this);
        _content.Spacing = Spacing;
        if (MorphShapes is null) _surface.CornerRadius = CornerRadius;
        QueueTargets();
    }

    private double ElevationTarget()
    {
        var values = Elevation;
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
            _surface.SetAnimatedCorners(null);
            _surface.CornerRadius = CornerRadius;
            return;
        }
        var pressed = MaterialInteraction.GetOrCreate(this).Interaction.HasFlag(InteractionState.Pressed);
        // key(shapes) uses ChipShapes' structural equality, including resolved theme defaults.
        var configuration = (Shape: shapes.Shape, Selected: shapes.SelectedShape, Pressed: shapes.PressedShape);
        var target = pressed ? configuration.Pressed : Selected ? configuration.Selected : configuration.Shape;
        if (ShapeAnimation is not { } spec) return; // A resource-scope move may temporarily withdraw the theme input.
        if (initial || _shapeConfiguration != configuration || _shape is null)
        {
            _shapeEffect?.Cancel(); _shapeEffect = null;
            _shape?.Dispose(); _shapeConfiguration = configuration;
            _shape = new AnimatedCornerRadius(target, spec, motion, () =>
            {
                if (_shape is { } shape && _surface is { } surface)
                { shape.Size = surface.Bounds.Size; surface.SetAnimatedCorners(shape.Current); }
            }) { Size = _surface.Bounds.Size };
            _surface.SetAnimatedCorners(_shape.Current);
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
